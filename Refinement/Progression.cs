using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RageRefinement
{
    public static class Progression
    {
        [Serializable]
        private sealed class SaveData
        {
            public int version = 1;
            public int clay;
            public int[] ranks = new int[8];
            public int attempts;
            public int victories;
            public bool storySeen;
            public int unlockedDistrict;
        }

        public static readonly string[] Names = { "Stronger kicks", "More hearts", "Clay shell", "Second wind", "Clay dash", "Ricochet", "Forked shots", "Rhythm punch" };
        public static readonly int[] Limits = { 8, 6, 4, 3, 3, 2, 2, 3 };
        private static readonly int[] BaseCosts = { 20, 24, 32, 40, 45, 60, 80, 70 };
        private static readonly List<Health> enemies = new List<Health>();
        private static readonly HashSet<Health> rewarded = new HashSet<Health>();
        private static SaveData save;
        private static int[] activeRanks = new int[8];
        private static float started;
        private static bool fought;
        private static int shields;
        private static Health playerHealth;
        public static bool RunActive { get; private set; }
        public static bool RunEnded { get; private set; }
        public static bool Won { get; private set; }
        public static int RunClay { get; private set; }
        public static int RunKills { get; private set; }
        public static int Clay { get { EnsureLoaded(); return save.clay; } }
        public static int Attempts { get { EnsureLoaded(); return save.attempts; } }
        public static bool StorySeen { get { EnsureLoaded(); return save.storySeen; } }
        public static int UnlockedDistrict { get { EnsureLoaded(); return save.unlockedDistrict; } }
        public static int Shields { get { return shields; } }
        public static event Action Changed;

        // Integration runs use a separate save key and cannot spend the player's real clay.
        private static string SaveKey
        {
            get
            {
                var args = Environment.GetCommandLineArgs();
                for (int index = 0; index + 1 < args.Length; index++)
                    if (args[index] == "-rage-qa-profile")
                        return "RageRefinement.QA.Progression." + args[index + 1];
                return "RageRefinement.Progression.v1";
            }
        }

        private static SaveData Read(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                var value = JsonUtility.FromJson<SaveData>(text);
                if (value == null || value.version != 1 || value.ranks == null || value.ranks.Length < 4 || value.ranks.Length > 8) return null;
                if (value.ranks.Length != 8) Array.Resize(ref value.ranks,8);
                value.clay = Mathf.Clamp(value.clay, 0, 1000000000);
                value.attempts = Mathf.Max(0, value.attempts);
                value.victories = Mathf.Max(0, value.victories);
                value.unlockedDistrict = Mathf.Clamp(value.unlockedDistrict,0,Course.StageCount - 1);
                for (int index = 0; index < 8; index++) value.ranks[index] = Mathf.Clamp(value.ranks[index], 0, Limits[index]);
                return value;
            }
            catch (ArgumentException) { return null; }
        }

        private static void EnsureLoaded()
        {
            if (save != null) return;
            save = Read(PlayerPrefs.GetString(SaveKey, ""))
                ?? Read(PlayerPrefs.GetString(SaveKey + ".backup", "")) ?? new SaveData();
        }

        private static void Persist()
        {
            var old = PlayerPrefs.GetString(SaveKey, "");
            if (Read(old) != null) PlayerPrefs.SetString(SaveKey + ".backup", old);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        private static void Notify() { if (Changed != null) Changed(); }
        public static int Rank(int index) { EnsureLoaded(); return index >= 0 && index < 8 ? save.ranks[index] : 0; }
        public static int ActiveRank(int index) { return index >= 0 && index < 8 ? Active(index) : 0; }
        private static int Active(int index) { return RunActive ? activeRanks[index] : Rank(index); }
        public static int Cost(int index)
        {
            if (index < 0 || index >= 8 || Rank(index) >= Limits[index]) return 0;
            int rank = Rank(index);
            return BaseCosts[index] + rank * 8 + rank * rank * 4;
        }
        public static bool TryBuy(int index)
        {
            if (index < 0 || index >= 8 || Rank(index) >= Limits[index]) return false;
            int cost = Cost(index);
            if (save.clay < cost) return false;
            save.clay -= cost;
            save.ranks[index]++;
            Persist();
            Notify();
            return true;
        }

        public static void BeginRun()
        {
            EnsureLoaded();
            activeRanks = (int[])save.ranks.Clone();
            RunActive = true;
            RunEnded = Won = fought = false;
            RunClay = RunKills = 0;
            shields = activeRanks[2];
            PlayerTricks.BeginRun();
            started = Time.realtimeSinceStartup;
            save.attempts++;
            Persist();
            Notify();
        }

        public static void LeaveRun()
        {
            RunActive = RunEnded = Won = false;
            playerHealth = null;
            Notify();
        }

        public static void SceneChanged(Scene previous, Scene current)
        {
            enemies.Clear();
            rewarded.Clear();
            playerHealth = null;
            if (current.buildIndex != Course.LevelScene) return;
            if (!RunActive) BeginRun();
            if (previous.buildIndex == 1 && !save.storySeen)
            {
                save.storySeen = true;
                Persist();
            }
        }

        public static void ConfigureHealth(Component health, ref int maximum, ref float invincibility)
        {
            EnsureLoaded();
            if (health.CompareTag("Player"))
            {
                maximum = 3 + Active(1) * 2;
                invincibility = Mathf.Max(invincibility, .8f);
            }
            else if (health.GetComponent<Enemy>() != null)
            {
                maximum = DemoContent.EnemyHealth[Course.Stage];
                invincibility = .18f;
            }
        }

        public static DamageData AdjustDamage(Health target, DamageData damage)
        {
            if (!RunActive || SceneManager.GetActiveScene().buildIndex != Course.LevelScene) return damage;
            if (target.CompareTag("Player"))
            {
                if (PlayerTricks.Dashing) { damage.Amount = 0; damage.KnockbackForce = 0; return damage; }
                if (damage.Amount > 0 && !target.IsInvincible && target.CurrentHealth > 0 && shields > 0)
                {
                    shields--;
                    damage.Amount = 0;
                    damage.KnockbackForce = 0;
                    Notify();
                }
            }
            else if (target.GetComponent<Enemy>() != null && damage.Amount > 0)
            {
                damage.Amount += activeRanks[0];
                if (!target.IsInvincible && target.CurrentHealth > 0) damage.Amount = PlayerTricks.RhythmDamage(damage.Amount);
                var kind = target.GetComponent<ClayEnemy>();
                if (kind != null && kind.Kind == ClayEnemyKind.Shieldguard &&
                    Mathf.Sign(damage.DamageSourcePosition.x - target.transform.position.x) == Mathf.Sign(target.transform.localScale.x))
                    damage.Amount = Mathf.Max(1,damage.Amount / 2);
                if (target.GetComponent<KilnKing>() != null) damage.KnockbackForce = 0;
            }
            return damage;
        }

        public static void TrackHealth(Component component)
        {
            if (SceneManager.GetActiveScene().buildIndex != Course.LevelScene) return;
            var health = component as Health;
            if (health == null) return;
            if (health.CompareTag("Player"))
            {
                playerHealth = health;
                health.OnDamaged += damage => { if (damage.Amount > 0) fought = true; };
                return;
            }
            if (health.GetComponent<Enemy>() == null || enemies.Contains(health)) return;
            enemies.Add(health);
            int previous = health.CurrentHealth;
            int budget = health.MaxHealth;
            health.OnHealthChanged += (current, maximum) =>
            {
                int removed = Mathf.Min(budget, Mathf.Max(0, previous - current));
                previous = current;
                if (!RunActive || removed <= 0) return;
                budget -= removed;
                fought = true;
                Award(removed);
            };
            health.OnDeath += () =>
            {
                if (!RunActive || !rewarded.Add(health)) return;
                RunKills++;
                Award(8 + Course.Stage * 4);
                if (playerHealth != null && activeRanks[3] > 0 && RunKills % (4 - activeRanks[3]) == 0) playerHealth.Heal(1);
                Notify();
            };
            Notify();
        }

        private static void Award(int amount)
        {
            EnsureLoaded();
            amount = Mathf.Max(0, amount);
            save.clay = Mathf.Min(1000000000, save.clay + amount);
            RunClay += amount;
            Persist();
            Notify();
        }

        public static int EnemiesRemaining
        {
            get
            {
                int count = 0;
                foreach (var health in enemies)
                    if (health != null && health.gameObject.activeInHierarchy && health.CurrentHealth > 0) count++;
                return count;
            }
        }

        public static Health NearestEnemy(Vector3 position)
        {
            Health nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var health in enemies)
            {
                if (health == null || !health.gameObject.activeInHierarchy || health.CurrentHealth <= 0) continue;
                float candidate = (health.transform.position - position).sqrMagnitude;
                if (candidate < distance) { nearest = health; distance = candidate; }
            }
            return nearest;
        }

        public static void UpdateWorld()
        {
            if (!RunActive || GameMenu.GameplayPaused) return;
            // Knockback can throw an enemy off a platform. It must not lock the exit forever.
            foreach (var health in enemies)
                if (health != null && health.gameObject.activeInHierarchy && health.CurrentHealth > 0 && health.transform.position.y < -22f)
                    health.TakeDamage(new DamageData(health.CurrentHealth, Vector2.zero, 0, 0));
        }

        public static void StageCleared()
        {
            if (!RunActive) return;
            save.unlockedDistrict = Mathf.Max(save.unlockedDistrict,Mathf.Min(Course.Stage + 1,Course.StageCount - 1));
            Award(25 * (Course.Stage + 1));
        }

        public static void HandleDefeat(string ignored)
        {
            if (SceneManager.GetActiveScene().buildIndex != Course.LevelScene)
            {
                SceneManager.LoadScene(Course.LevelScene);
                return;
            }
            EndRun(false);
        }

        public static void EndRun(bool won)
        {
            if (!RunActive || RunEnded) return;
            if (!won && (fought || Time.realtimeSinceStartup - started >= 5f)) Award(4);
            if (won) { save.victories++; Persist(); }
            RunActive = false;
            RunEnded = true;
            Won = won;
            GameMenu.ShowRunResult();
            Notify();
        }
    }
}
