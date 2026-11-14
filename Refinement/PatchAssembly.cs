using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class PatchAssembly
{
    public static void Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Expected original DLL, menu DLL, and output DLL.");

        using (var resolver = new DefaultAssemblyResolver())
        {
            resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0])));
            resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
            var reader = new ReaderParameters { AssemblyResolver = resolver, InMemory = true };
            using (var original = AssemblyDefinition.ReadAssembly(args[0], reader))
            using (var menu = AssemblyDefinition.ReadAssembly(args[1], reader))
            {
                var game = original.MainModule;
                var helpers = menu.MainModule.GetType("RageRefinement.GameMenu");
                var blockInput = Import(game, helpers, "get_BlockGameplayInput");
                var bindings = menu.MainModule.GetType("RageRefinement.KeyboardBindings");
                var actions = game.GetType("InputSystem_Actions");
                var asset = Method(actions, "get_asset");
                AttachAtReturn(Method(actions, ".ctor"), asset, Import(game, bindings, "Register"));
                var dispose = Method(actions, "Dispose");
                var disposeIL = dispose.Body.GetILProcessor();
                var disposeEntry = dispose.Body.Instructions[0];
                disposeIL.InsertBefore(disposeEntry, disposeIL.Create(OpCodes.Ldarg_0));
                disposeIL.InsertBefore(disposeEntry, disposeIL.Create(OpCodes.Call, asset));
                disposeIL.InsertBefore(disposeEntry, disposeIL.Create(OpCodes.Call, Import(game, bindings, "Unregister")));

                var course = menu.MainModule.GetType("RageRefinement.Course");
                var health = game.GetType("Health");
                var progression = menu.MainModule.GetType("RageRefinement.Progression");
                var healthStart = Method(health, "Start");
                var startIL = healthStart.Body.GetILProcessor();
                var startEntry = healthStart.Body.Instructions[0];
                startIL.InsertBefore(startEntry, startIL.Create(OpCodes.Ldarg_0));
                startIL.InsertBefore(startEntry, startIL.Create(OpCodes.Call, Import(game, progression, "TrackHealth")));
                var damage = Method(health, "TakeDamage");
                var damageIL = damage.Body.GetILProcessor();
                var damageEntry = damage.Body.Instructions[0];
                damageIL.InsertBefore(damageEntry, damageIL.Create(OpCodes.Ldarg_0));
                damageIL.InsertBefore(damageEntry, damageIL.Create(OpCodes.Ldarg_1));
                damageIL.InsertBefore(damageEntry, damageIL.Create(OpCodes.Call, Import(game, progression, "AdjustDamage")));
                damageIL.InsertBefore(damageEntry, damageIL.Create(OpCodes.Starg, damage.Parameters[0]));
                ReplaceCalls(Method(game.GetType("PlayerHealthReceiver"), "HandleDeath"), "UnityEngine.SceneManagement.SceneManager",
                    "LoadScene", Import(game, progression, "HandleDefeat"), 1);
                var startJourney = Method(game.GetType("MainMenuController"), "OnStartClicked");
                startJourney.Body.Instructions.Clear();
                startJourney.Body.ExceptionHandlers.Clear();
                startJourney.Body.Variables.Clear();
                var journeyIL = startJourney.Body.GetILProcessor();
                journeyIL.Emit(OpCodes.Call, Import(game, helpers, "StartJourney"));
                journeyIL.Emit(OpCodes.Ret);
                var healthAwake = Method(health, "Awake");
                var healthIL = healthAwake.Body.GetILProcessor();
                var healthEntry = healthAwake.Body.Instructions[0];
                healthIL.InsertBefore(healthEntry, healthIL.Create(OpCodes.Ldarg_0));
                foreach (var name in new[] { "_maxHealth", "_invincibilityDuration" })
                {
                    healthIL.InsertBefore(healthEntry, healthIL.Create(OpCodes.Ldarg_0));
                    healthIL.InsertBefore(healthEntry, healthIL.Create(OpCodes.Ldflda, health.Fields.Single(field => field.Name == name)));
                }
                healthIL.InsertBefore(healthEntry, healthIL.Create(OpCodes.Call, Import(game, course, "ConfigurePlayerHealth")));
                var jump = Method(game.GetType("PlayerController"), "HandleJump");
                var jumpIL = jump.Body.GetILProcessor();
                var jumpForce = jump.Body.Instructions.Single(instruction => instruction.OpCode == OpCodes.Ldfld
                    && ((FieldReference)instruction.Operand).Name == "JumpForce");
                jumpIL.InsertAfter(jumpForce, jumpIL.Create(OpCodes.Call, Import(game, course, "JumpVelocity")));
                var trigger = Method(game.GetType("WinTrigger"), "OnTriggerEnter2D");
                var triggerIL = trigger.Body.GetILProcessor();
                var triggerEntry = trigger.Body.Instructions[0];
                triggerIL.InsertBefore(triggerEntry, triggerIL.Create(OpCodes.Ldarg_1));
                triggerIL.InsertBefore(triggerEntry, triggerIL.Create(OpCodes.Call, Import(game, course, "TryAdvance")));
                triggerIL.InsertBefore(triggerEntry, triggerIL.Create(OpCodes.Brfalse_S, triggerEntry));
                triggerIL.InsertBefore(triggerEntry, triggerIL.Create(OpCodes.Ret));

                CaptureEnemySpawnInStart(game.GetType("Enemy"));
                var dialogue = game.GetType("DialogueUIController");
                LocalizeDialogue(game, menu.MainModule, dialogue);
                var titleUI = Method(game.GetType("MainMenuController"), "OnUIReload");
                var titleCaptions = menu.MainModule.GetType("RageRefinement.TitleCaptions");
                var uiIL = titleUI.Body.GetILProcessor();
                var uiEntry = titleUI.Body.Instructions[0];
                uiIL.InsertBefore(uiEntry, uiIL.Create(OpCodes.Ldarg_2));
                uiIL.InsertBefore(uiEntry, uiIL.Create(OpCodes.Call, Import(game, titleCaptions, "Attach")));
                var nameLabel = dialogue.Fields.Single(field => field.Name == "_speakerNameLabel");
                AttachAtReturn(Method(dialogue, "ApplySpeaker"), nameLabel, Import(game, helpers, "StyleSpeakerName"));
                var title = Method(game.GetType("MainMenuController"), "Awake");
                var hud = game.GetType("PlayerHUDController");
                AttachAtReturn(Method(hud, "InitializeHearts"), hud.Fields.Single(field => field.Name == "_heartContainer"),
                    Import(game, helpers, "StyleHearts"));
                var progress = game.GetType("DialogueProgress");
                var resetProgress = Method(progress, "Restore");
                var mark = Method(progress, "MarkCompleted");
                var markIL = mark.Body.GetILProcessor();
                var markRet = mark.Body.Instructions.Last(instruction => instruction.OpCode == OpCodes.Ret);
                markRet.OpCode = OpCodes.Ldarg_0;
                var notify = markIL.Create(OpCodes.Call, Import(game, helpers, "OnConversationCompleted"));
                markIL.InsertAfter(markRet, notify);
                markIL.InsertAfter(notify, markIL.Create(OpCodes.Ret));
                var titleIL = title.Body.GetILProcessor();
                var titleEntry = title.Body.Instructions[0];
                titleIL.InsertBefore(titleEntry, titleIL.Create(OpCodes.Ldnull));
                titleIL.InsertBefore(titleEntry, titleIL.Create(OpCodes.Call, resetProgress));
                foreach (var name in new[] { "PlayerCombat", "PlayerInteractor", "PlayerController" })
                {
                    var type = game.GetType(name);
                    var update = Method(type, "Update");
                    if (name == "PlayerCombat")
                        GuardCombatInput(update, blockInput);
                    else
                        Guard(update, blockInput);
                    DisposeActions(game, type);
                }

                var paused = Import(game, helpers, "get_GameplayPaused");
                Guard(damage, paused);
                var projectile = game.GetType("Projectile");
                var outsideView = Import(game, menu.MainModule.GetType("RageRefinement.ProjectileBounds"), "IsOutsideView");
                CullOffscreenProjectile(Method(projectile, "FixedUpdate"), outsideView, Method(projectile, "Despawn"));
                // Physics may cross the edge and trigger a hit in the same step.
                CullOffscreenProjectile(Method(projectile, "OnTriggerEnter2D"), outsideView, Method(projectile, "Despawn"));
                Guard(Method(game.GetType("Enemy"), "Update"), paused);
                Guard(Method(game.GetType("Enemy"), "HitboxCheck"), paused);
                var combat = game.GetType("PlayerCombat");
                Guard(Method(combat, "HitboxCheck"), paused);

                var timeScale = Import(game, helpers, "SetGameplayTimeScale");
                ReplaceCalls(Method(combat, "OnDisable"), "UnityEngine.Time", "set_timeScale", timeScale, 1);
                ReplaceCalls(StateMachine(combat, "<HitStopRoutine>"), "UnityEngine.Time", "set_timeScale", timeScale, 2);
                ReplaceCalls(StateMachine(game.GetType("DialogueUIController"), "<TypeRoutine>"),
                    "UnityEngine.Time", "get_unscaledDeltaTime", Import(game, helpers, "DialogueDeltaTime"), 1);

                var scale = Import(game, helpers, "SetLocalScaleIfChanged");
                ReplaceCalls(Method(game.GetType("Enemy"), "Face"), "UnityEngine.Transform", "set_localScale", scale, 1);
                ReplaceCalls(Method(game.GetType("PlayerController"), "HandleMovement"),
                    "UnityEngine.Transform", "set_localScale", scale, 1);

                game.Write(args[2]);
            }
        }
    }

    private static MethodDefinition Method(TypeDefinition type, string name)
    {
        return type.Methods.Single(method => method.Name == name);
    }

    private static MethodDefinition StateMachine(TypeDefinition type, string namePrefix)
    {
        return Method(type.NestedTypes.Single(nested => nested.Name.StartsWith(namePrefix, StringComparison.Ordinal)), "MoveNext");
    }

    private static MethodReference Import(ModuleDefinition game, TypeDefinition helpers, string name)
    {
        return game.ImportReference(Method(helpers, name));
    }

    private static void Guard(MethodDefinition method, MethodReference predicate)
    {
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];
        il.InsertBefore(first, il.Create(OpCodes.Call, predicate));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse_S, first));
        il.InsertBefore(first, il.Create(OpCodes.Ret));
    }

    private static void CullOffscreenProjectile(MethodDefinition method, MethodReference outsideView, MethodReference despawn)
    {
        var il = method.Body.GetILProcessor();
        var entry = method.Body.Instructions[0];
        il.InsertBefore(entry, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(entry, il.Create(OpCodes.Call, outsideView));
        il.InsertBefore(entry, il.Create(OpCodes.Brfalse_S, entry));
        il.InsertBefore(entry, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(entry, il.Create(OpCodes.Call, despawn));
        il.InsertBefore(entry, il.Create(OpCodes.Ret));
    }

    private static void LocalizeDialogue(ModuleDefinition game, ModuleDefinition menu, TypeDefinition dialogue)
    {
        var translations = menu.GetType("RageRefinement.DialogueTranslations");
        var replay = translations.NestedTypes.Single(type => type.Name == "ReplayLine");
        var begin = Method(dialogue, "BeginLine");
        var il = begin.Body.GetILProcessor();
        var entry = begin.Body.Instructions[0];
        il.InsertBefore(entry, il.Create(OpCodes.Ldarg_1));
        il.InsertBefore(entry, il.Create(OpCodes.Ldarg_2));
        il.InsertBefore(entry, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(entry, il.Create(OpCodes.Ldftn, begin));
        il.InsertBefore(entry, il.Create(OpCodes.Newobj, Import(game, replay, ".ctor")));
        il.InsertBefore(entry, il.Create(OpCodes.Call, Import(game, translations, "Remember")));

        foreach (var parameter in begin.Parameters)
        {
            string name = parameter.Index == 0 ? "Translate" : "TranslateChoices";
            il.InsertBefore(entry, il.Create(OpCodes.Ldarg, parameter));
            il.InsertBefore(entry, il.Create(OpCodes.Call, Import(game, translations, name)));
            il.InsertBefore(entry, il.Create(OpCodes.Starg, parameter));
        }

        var hint = dialogue.Fields.Single(field => field.Name == "_continueHint");
        AttachAtReturn(Method(dialogue, "OnUIReload"), hint, Import(game, translations, "AttachContinueCaption"));
        var disable = Method(dialogue, "OnDisable");
        var disableIL = disable.Body.GetILProcessor();
        disableIL.InsertBefore(disable.Body.Instructions[0],
            disableIL.Create(OpCodes.Call, Import(game, translations, "DetachContinueCaption")));

        foreach (var name in new[] { "HideDialogueUI", "OnDisable" })
        {
            var cleanup = Method(dialogue, name);
            var cleanupIL = cleanup.Body.GetILProcessor();
            cleanupIL.InsertBefore(cleanup.Body.Instructions[0],
                cleanupIL.Create(OpCodes.Call, Import(game, translations, "Clear")));
        }
    }

    private static void GuardCombatInput(MethodDefinition method, MethodReference predicate)
    {
        var type = method.DeclaringType;
        var charged = type.Fields.Single(field => field.Name == "_attack1Down");
        var il = method.Body.GetILProcessor();
        var first = method.Body.Instructions[0];
        var done = il.Create(OpCodes.Ret);
        il.InsertBefore(first, il.Create(OpCodes.Call, predicate));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse_S, first));
        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(first, il.Create(OpCodes.Ldfld, charged));
        il.InsertBefore(first, il.Create(OpCodes.Brfalse_S, done));
        il.InsertBefore(first, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(first, il.Create(OpCodes.Call, Method(type, "CancelCharge")));
        il.InsertBefore(first, done);
    }

    private static void ReplaceCalls(MethodDefinition method, string owner, string name, MethodReference replacement, int expected)
    {
        var sites = method.Body.Instructions.Where(instruction =>
        {
            var call = instruction.Operand as MethodReference;
            return call != null && call.Name == name && call.DeclaringType.FullName == owner;
        }).ToArray();
        if (sites.Length != expected)
            throw new InvalidOperationException("Unexpected call sites in " + method.FullName + " for " + name);
        foreach (var site in sites)
        {
            site.OpCode = OpCodes.Call;
            site.Operand = replacement;
        }
    }

    private static void AttachAtReturn(MethodDefinition method, MemberReference value, MethodReference callback)
    {
        var il = method.Body.GetILProcessor();
        var field = value as FieldReference;
        foreach (var ret in method.Body.Instructions.Where(instruction => instruction.OpCode == OpCodes.Ret).ToArray())
        {
            ret.OpCode = OpCodes.Ldarg_0;
            var load = field != null ? il.Create(OpCodes.Ldfld, field) : il.Create(OpCodes.Call, (MethodReference)value);
            il.InsertAfter(ret, load);
            var call = il.Create(OpCodes.Call, callback);
            il.InsertAfter(load, call);
            il.InsertAfter(call, il.Create(OpCodes.Ret));
        }
    }

    private static void CaptureEnemySpawnInStart(TypeDefinition enemy)
    {
        var awake = Method(enemy, "Awake");
        var instructions = awake.Body.Instructions;
        var spawn = enemy.Fields.Single(field => field.Name == "_spawnX");
        int index = instructions.IndexOf(instructions.Single(instruction =>
            instruction.OpCode == OpCodes.Stfld && ReferenceEquals(instruction.Operand, spawn)));
        var start = Method(enemy, "Start");
        var il = start.Body.GetILProcessor();
        var entry = start.Body.Instructions[0];
        // Scene generation moves enemies after Awake and before Start.
        for (int offset = -5; offset <= 0; offset++)
        {
            var source = instructions[index + offset];
            var member = source.Operand as MethodReference;
            var field = source.Operand as FieldReference;
            var clone = member != null ? il.Create(source.OpCode, member) :
                field != null ? il.Create(source.OpCode, field) : il.Create(source.OpCode);
            il.InsertBefore(entry, clone);
        }
    }

    private static void DisposeActions(ModuleDefinition game, TypeDefinition type)
    {
        var actions = type.Fields.Single(field => field.Name == "_inputActions");
        var dispose = Method(actions.FieldType.Resolve(), "Dispose");
        if (type.Methods.Any(method => method.Name == "OnDestroy"))
            throw new InvalidOperationException("OnDestroy already exists in " + type.FullName);

        var cleanup = new MethodDefinition("OnDestroy", MethodAttributes.Private | MethodAttributes.HideBySig, game.TypeSystem.Void);
        type.Methods.Add(cleanup);
        var il = cleanup.Body.GetILProcessor();
        var ret = il.Create(OpCodes.Ret);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, actions);
        il.Emit(OpCodes.Brfalse_S, ret);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, actions);
        il.Emit(OpCodes.Callvirt, dispose);
        il.Append(ret);
    }
}
