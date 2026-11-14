using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RageRefinement;

namespace RageBench
{
    public static class ProgressionChecks
    {
        private static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
        public static int Failures { get; private set; }
        private static void Expect(bool value, string name)
        {
            if (!value) { Failures++; Debug.LogError("ECONOMY FAIL " + name); }
            else Debug.Log("ECONOMY PASS " + name);
        }
        private static Health Player() { return UnityEngine.Object.FindAnyObjectByType<PlayerController>().GetComponent<Health>(); }
        private static Health Enemy() { return UnityEngine.Object.FindObjectsByType<Enemy>().Select(e => e.GetComponent<Health>()).First(h => h.CurrentHealth > 0); }
        private static void Hurt(Health health, int amount)
        {
            typeof(Health).GetProperty("IsInvincible").GetSetMethod(true).Invoke(health, new object[] {false});
            health.TakeDamage(new DamageData(amount, Vector2.zero, 0, 0));
        }
        private static void StartRun(GameMenu menu)
        {
            typeof(GameMenu).GetMethod("StartRun", Hidden).Invoke(menu, new object[] {false});
        }
        public static IEnumerator Run()
        {
            Failures = 0;
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-rage-qa-profile");
            if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Economy checks require an isolated QA profile.");
            string key = "RageRefinement.QA.Progression." + args[index + 1];
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(key + ".backup");
            Progression.LeaveRun();
            typeof(Progression).GetField("save", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
            var menu = UnityEngine.Object.FindAnyObjectByType<GameMenu>();
            StartRun(menu);
            yield return null; yield return null;
            Time.timeScale = 0;
            Expect(Player().MaxHealth == 3 && Progression.Rank(0) == 0 && Progression.Clay == 0, "fresh profile starts weak and empty");
            Expect(!Progression.TryBuy(0) && !Progression.TryBuy(-1) && Progression.Clay == 0, "unaffordable and invalid purchases rejected");
            int stage = Course.Stage;
            Course.TryAdvance(Player().GetComponent<Collider2D>());
            Expect(Course.Stage == stage && Progression.EnemiesRemaining > 0, "exit locked until every enemy is defeated");
            var enemy = Enemy();
            int initial = enemy.CurrentHealth;
            Hurt(enemy, 1);
            Expect(enemy.CurrentHealth == initial - 1 && Progression.Clay == 1, "partial damage earns saved clay");
            Hurt(enemy, 1000);
            Expect(Progression.Clay == initial + 8 && Progression.RunKills == 1, "overkill rewards actual health only plus one defeat bonus");
            int wallet = Progression.Clay;
            Hurt(enemy, 1000);
            Expect(Progression.Clay == wallet, "dead enemies cannot reward twice");
            Hurt(Enemy(),1000);
            wallet = Progression.Clay;
            int price = Progression.Cost(0);
            Expect(Progression.TryBuy(0) && Progression.Clay == wallet - price && Progression.Rank(0) == 1, "purchase charges the exact price");
            enemy = Enemy();
            initial = enemy.CurrentHealth;
            Hurt(enemy, 1);
            Expect(enemy.CurrentHealth == initial - 1, "mid-run purchases wait until the next run");
            Hurt(Player(), 1000);
            Expect(Progression.RunEnded && GameMenu.GameplayPaused && SceneManager.GetActiveScene().buildIndex == Course.LevelScene,
                "defeat opens a paused result screen without automatic reload");
            wallet = Progression.Clay;
            enemy = Enemy(); initial = enemy.CurrentHealth;
            Hurt(enemy, 1);
            Expect(enemy.CurrentHealth == initial && Progression.Clay == wallet, "paused damage and currency farming blocked");
            typeof(Progression).GetField("save", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
            Expect(Progression.Rank(0) == 1 && Progression.Clay == wallet, "wallet and purchases reload from persistent save");

            StartRun(menu);
            yield return null; yield return null;
            Time.timeScale = 0;
            enemy = Enemy(); initial = enemy.CurrentHealth;
            Hurt(enemy, 1);
            Expect(initial - enemy.CurrentHealth == 2, "bought damage applies on the next run");
            var fallen = Enemy();
            int remaining = Progression.EnemiesRemaining;
            fallen.transform.position = new Vector3(fallen.transform.position.x, -30, 0);
            typeof(Health).GetProperty("IsInvincible").GetSetMethod(true).Invoke(fallen, new object[] {false});
            Progression.UpdateWorld();
            Expect(fallen.CurrentHealth == 0 && Progression.EnemiesRemaining == remaining - 1, "fallen enemies cannot permanently lock an exit");
            for (int wave = 0; wave < Course.StageCount; wave++)
            {
                for (int sweep = 0; sweep < 4 && Progression.EnemiesRemaining > 0; sweep++)
                    foreach (var target in UnityEngine.Object.FindObjectsByType<Enemy>())
                        if (target.GetComponent<Health>().CurrentHealth > 0) Hurt(target.GetComponent<Health>(), 10000);
                Expect(Progression.EnemiesRemaining == 0, "all enemies counted in stage " + (wave + 1));
                var gate=UnityEngine.Object.FindAnyObjectByType<WinTrigger>();
                typeof(WinTrigger).GetMethod("OnTriggerStay2D",Hidden).Invoke(gate,new object[] {Player().GetComponent<Collider2D>()});
                if (wave < Course.StageCount - 1)
                {
                    yield return null; yield return null;
                    Time.timeScale = 0;
                    Expect(Course.Stage == wave + 1 && Enemy().MaxHealth >= DemoContent.EnemyHealth[Course.Stage], "next district gets tougher");
                }
            }
            Expect(Course.Finished && Progression.Won && GameMenu.GameplayPaused, "clearing all six districts opens victory results");
            Expect(Progression.TryBuy(1) && Progression.TryBuy(2) && Progression.TryBuy(3), "all four body buff types can be purchased");
            StartRun(menu);
            yield return null; yield return null;
            Time.timeScale = 0;
            var player = Player();
            Expect(player.MaxHealth == 5 && Progression.Shields == 1, "health and shell buffs activate at run start");
            Hurt(player, 1);
            Expect(player.CurrentHealth == 5 && Progression.Shields == 0, "shell blocks one hit and is consumed");
            Hurt(player, 1);
            Expect(player.CurrentHealth == 4, "damage resumes after the shell is used");
            Hurt(Enemy(), 10000);
            Expect(player.CurrentHealth == 4, "first healing rank does not trivialize every fight");
            Hurt(Enemy(), 10000); Hurt(Enemy(), 10000);
            Expect(player.CurrentHealth == 5, "second wind heals after three defeats at rank one");
            Hurt(player, 10000);
            Expect(Progression.RunEnded, "subsequent defeat returns to the same loop");
            while (Progression.Rank(1) < Progression.Limits[1] && Progression.TryBuy(1)) { }
            Expect(Progression.Rank(1) == Progression.Limits[1] && !Progression.TryBuy(1), "upgrade cap prevents extra purchases");
            StartRun(menu);
            yield return null; yield return null;
            Time.timeScale = 0;
            Expect(Player().MaxHealth == 15, "maximum health buff applies without losing hearts");
            var hud = UnityEngine.Object.FindAnyObjectByType<PlayerHUDController>();
            var hearts = (VisualElement)typeof(PlayerHUDController).GetField("_heartContainer", Hidden).GetValue(hud);
            var viewport = hearts.panel.visualTree.worldBound;
            Expect(hearts.childCount == 15 && hearts.Children().All(h => h.worldBound.xMax <= viewport.xMax && h.worldBound.yMax <= viewport.yMax),
                "all fifteen clay hearts fit the HUD");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "max-hearts.png"));
            yield return new WaitForSecondsRealtime(.15f);
            Hurt(Player(), 10000); Hurt(Player(), 10000);
            string expected = Progression.Clay + "|" + string.Join(",", Enumerable.Range(0, 8).Select(i => Progression.Rank(i).ToString()).ToArray());
            PlayerPrefs.SetString(key + ".expected", expected);
            PlayerPrefs.Save();
            Debug.Log("ECONOMY complete; failures=" + Failures + " persisted=" + expected);
        }
    }
}
