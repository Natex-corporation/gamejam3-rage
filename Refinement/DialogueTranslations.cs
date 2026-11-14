using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RageRefinement
{
    public static class DialogueTranslations
    {
        // Source, Czech override, English, Slovak, German. Null keeps the original Czech.
        private static readonly string[,] Lines =
        {
            { "uGHrrr..", "Ugh...", "Ugh...", "Ugh...", "Uff..." },
            { "aAAahhh..", "Ááá...", "Aaah...", "Ááá...", "Aaah..." },
            { "A víte co mě to ani nezajíma!", "A víte co? Mě to ani nezajímá!", "You know what? I don't even care!", "A viete čo? Ani ma to nezaujíma!", "Wissen Sie was? Es ist mir völlig egal!" },
            { "Ahoj, já jsem Hnoop, jsem tu nový. Jak se jmenujete vy?", null, "Hi, I'm Hnoop. I'm new here. What's your name?", "Ahoj, ja som Hnoop, som tu nový. Ako sa voláte vy?", "Hallo, ich bin Hnoop und neu hier. Wie heißen Sie?" },
            { "Ale proč ne? Vždyť jsme sousedé a až se potkáme u popelnic, tak je blbé abych vás oslovoval: Hej ty hlíno! A kde tu bydlíte?", null, "Why not? We're neighbors. I can't just shout, ‘Hey, clayface!’ when we meet by the trash cans. Where do you live?", "Prečo nie? Veď sme susedia. Keď sa stretneme pri smetiakoch, nemôžem len tak zvolať: „Hej, ty hruda hliny!“ Kde bývate?", "Warum nicht? Wir sind doch Nachbarn. An der Mülltonne kann ich schlecht »Hey, du Tonklumpen!« rufen. Wo wohnen Sie denn?" },
            { "Asi si tady všichni velmi cení svého soukromí. Zkusím něco neutrálního. Nevíte kdy sváží odpad?", "Asi si tady všichni velmi cení svého soukromí. Zkusím něco neutrálního. Nevíte, kdy sváží odpad?", "People here sure like their privacy. Fine. Do you know when the trash gets picked up?", "Asi si tu všetci veľmi cenia súkromie. Skúsim niečo neutrálne. Neviete, kedy vyvážajú odpad?", "Hier legen wohl alle Wert auf ihre Privatsphäre. Na gut. Wann kommt denn die Müllabfuhr?" },
            { "Dík za nic!", null, "Thanks for nothing!", "Vďaka za nič!", "Danke für nichts!" },
            { "Divná to vesnice.", null, "What a weird village.", "To je ale čudná dedina.", "Was für ein seltsames Dorf." },
            { "Hej ty! Neznáme se od někud?", "Hej, ty! Neznáme se odněkud?", "Hey! Don't I know you from somewhere?", "Hej, ty! Nepoznáme sa odniekiaľ?", "Hey, du! Kennen wir uns nicht irgendwoher?" },
            { "Hmm, ten mi toho asi moc neřekne...", null, "Hmm, I don't think he'll tell me much...", "Hmm, ten mi toho asi veľa nepovie...", "Hmm, viel wird der mir wohl nicht erzählen ..." },
            { "Já vůbec nechápu kde jsem se ocitl! To je tady normální?!", "Já vůbec nechápu, kde jsem se ocitl! To je tady normální?!", "I have no idea where I've ended up! Is this normal around here?!", "Vôbec nechápem, kde som sa ocitol! To je tu normálne?!", "Ich habe keine Ahnung, wo ich hier gelandet bin! Ist das hier normal?!" },
            { "JESTLI JEŠTĚ JEDNOU USLYŠÍM...", null, "IF I HEAR THAT ONE MORE TIME...", "AK TO EŠTE RAZ ZAČUJEM...", "WENN ICH DAS NOCH EINMAL HÖRE ..." },
            { "Mě to nezajímá.", null, "I don't care.", "Mňa to nezaujíma.", "Das ist mir egal." },
            { "My jsem se ještě nepotkali. Já jsem váš nový soused Hnoop. Jak se vám daří?", "My jsme se ještě nepotkali. Já jsem váš nový soused Hnoop. Jak se vám daří?", "We haven't met yet. I'm Hnoop, your new neighbor. How are you doing?", "My sme sa ešte nestretli. Som váš nový sused Hnoop. Ako sa máte?", "Wir sind uns noch nicht begegnet. Ich bin Hnoop, Ihr neuer Nachbar. Wie geht es Ihnen?" },
            { "Nazdar, umíš mluvit? Mě tu nikdo neodpovídá.", "Nazdar, umíš mluvit? Mně tu nikdo neodpovídá.", "Hey, can you talk? Nobody here will answer me.", "Nazdar, vieš rozprávať? Nikto mi tu neodpovedá.", "Hi, kannst du reden? Hier antwortet mir keiner." },
            { "No dobře, tak vy mi toho asi moc neřeknete.", null, "Right. I guess I'm not getting much out of you.", "No dobre, tak vy mi toho asi veľa nepoviete.", "Na gut, von Ihnen erfahre ich wohl auch nicht viel." },
            { "No to se snad zplácnu, další co určitě nemluví. Řekneš mi co se tady sakra děje?!", "No to se snad zplácnu. Další, co určitě nemluví. Řekneš mi, co se tady sakra děje?!", "For clay's sake, another one who won't talk. What the hell is going on here?!", "No to ma snáď sploští. Ďalší, čo nepovie ani slovo. Čo sa tu, dočerta, deje?!", "Ach, drück mich doch platt. Noch einer, der den Mund nicht aufkriegt. Was ist hier zum Teufel los?!" },
            { "Tenhle asi taky nerad mluví...", null, "This one doesn't like to talk either...", "Ani tento asi nerád rozpráva...", "Der redet wohl auch nicht gern ..." },
            { "To mě vůbec nepřekvapuje! A řekneš mi vůbec něco?!", null, "Figures. Are you going to tell me anything at all?!", "To ma vôbec neprekvapuje! A povieš mi vôbec niečo?!", "Das überrascht mich überhaupt nicht! Sagst du mir überhaupt irgendwas?!" },
            { "To ti neřek...", null, "I won't tell—", "To ti nepo—", "Das sag ich dir n–" },
            { "To ti neřeknu!", null, "I won't tell you!", "To ti nepoviem!", "Das sag ich dir nicht!" },
            { "To ti neřeknu.", null, "I won't tell you.", "To ti nepoviem.", "Das sag ich dir nicht." },
            { "Tohle už není normální!", null, "Okay, now this is just weird!", "Toto už nie je normálne!", "Das ist doch nicht mehr normal!" },
            { "Víš co? Jdi se ZMÁČKNOUT!!", null, "You know what? Go FLATTEN yourself!", "Vieš čo? Choď sa SPLOŠTIŤ!", "Weißt du was? Geh dich PLATTDRÜCKEN!" },
            { "Zdravím i vás sousede můj. Vy jste místní?", "Zdravím i vás, sousede můj. Vy jste místní?", "Hey there, neighbor. Are you from around here?", "Zdravím aj vás, sused môj. Vy ste odtiaľto?", "Hallo, Nachbar. Sind Sie von hier?" },
            { "I should probably kill them all now!", "Asi bych je teď měl všechny zabít!", "I should probably kill them all now!", "Asi by som ich teraz mal všetkých zabiť!", "Ich sollte sie jetzt wohl alle erledigen!" },
            { "I caN pRobABly kiLL TheM by kiCKinG with a 'Left Click'!", "Krátkým stiskem tlačítka útoku kopnu.", "Tap Attack to kick.", "Krátkym stlačením tlačidla útoku kopnem.", "Tippe die Angriffstaste an, um zu treten." },
            { "Or I coULd shOOt theM by chaRGinG with holding 'Left Click' and then RELEASE!", "Podrž tlačítko útoku a pak ho pusť. Vystřelíš.", "Hold Attack to charge a shot, then release to fire.", "Podrž tlačidlo útoku a potom ho pusť. Vystrelíš.", "Halte die Angriffstaste zum Aufladen und lass sie zum Schießen los." },
        };

        private static readonly Dictionary<string, int> Rows = Locale.IndexRows(Lines);
        public delegate void ReplayLine(string text, IReadOnlyList<string> choices);
        private static ReplayLine replay;
        private static string source;
        private static IReadOnlyList<string> sourceChoices;
        private static Label continueCaption;

        static DialogueTranslations()
        {
            Locale.Changed += Refresh;
        }

        public static void Remember(string text, IReadOnlyList<string> choices, ReplayLine callback)
        {
            source = text;
            sourceChoices = choices;
            replay = callback;
        }

        public static void Clear()
        {
            source = null;
            sourceChoices = null;
            replay = null;
        }

        public static void AttachContinueCaption(VisualElement hint)
        {
            if (hint == null) return;
            continueCaption = hint.Q<Label>("LocalizedContinue");
            if (continueCaption == null)
            {
                continueCaption = new Label { name = "LocalizedContinue", pickingMode = PickingMode.Ignore };
                var style = continueCaption.style;
                style.position = Position.Absolute;
                style.right = Length.Percent(110);
                style.top = 0;
                style.whiteSpace = WhiteSpace.NoWrap;
                style.unityFontDefinition = GameMenu.HandwrittenFont;
                style.fontSize = 20;
                style.unityFontStyleAndWeight = FontStyle.Bold;
                style.color = (Color)new Color32(255, 239, 215, 255);
                style.backgroundColor = (Color)new Color32(35, 21, 24, 255);
                style.paddingLeft = 8;
                style.paddingRight = 8;
                GameMenu.StylePaperBadge(continueCaption);
                hint.Add(continueCaption);
            }
            RefreshCaption();
        }

        public static void RefreshCaption()
        {
            if (continueCaption != null)
                continueCaption.text = Locale.UI("Continue") + "  ·  " + KeyboardBindings.DisplayKey(4);
        }

        public static void DetachContinueCaption()
        {
            continueCaption = null;
        }

        private static void Refresh()
        {
            RefreshCaption();
            if (replay != null) replay(source, sourceChoices);
        }

        public static string Translate(string text)
        {
            int row;
            if (string.IsNullOrEmpty(text) || !Rows.TryGetValue(text.Trim(), out row)) return text;
            return Lines[row, Locale.Language + 1] ?? text;
        }

        public static IReadOnlyList<string> TranslateChoices(IReadOnlyList<string> choices)
        {
            if (choices == null) return null;
            var result = new string[choices.Count];
            for (int index = 0; index < result.Length; index++)
                result[index] = Translate(choices[index]);
            return result;
        }
    }
}
