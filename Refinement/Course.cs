using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RageRefinement
{
    public static class Course
    {
        public const int LevelScene = 2;
        public const int StageCount = 6;
        public static int Stage { get; private set; }
        public static bool Finished { get; private set; }

        private static int seed;
        private static bool changingScene;
        private static Transform player;
        private static bool checkpointReached;
        private static Vector3 checkpoint;
        private static float shelfTop;
        private static bool shelfReady;

        public static bool HasCheckpoint { get { return checkpointReached; } }

        public static void RestartFromBeginning()
        {
            checkpointReached = false;
        }

        public static void ConfigurePlayerHealth(Component health, ref int maximum, ref float invincibility)
        {
            Progression.ConfigureHealth(health, ref maximum, ref invincibility);
        }

        public static void BeginRun(int district = 0)
        {
            Stage = Mathf.Clamp(district,0,StageCount - 1);
            checkpointReached = false;
            Finished = false;
            seed = Guid.NewGuid().GetHashCode();
        }

        public static float JumpVelocity(float original)
        {
            return SceneManager.GetActiveScene().buildIndex == LevelScene ? original * 1.06f : original;
        }

        public static void OnSceneChanged(Scene scene)
        {
            player = null;
            shelfReady = false;
            changingScene = false;
            if (scene.buildIndex == 0)
            {
                Stage = 0;
                checkpointReached = false;
                Finished = false;
                seed = Guid.NewGuid().GetHashCode();
                return;
            }
            if (scene.buildIndex != LevelScene) return;

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                if (root.CompareTag("Player")) player = root.transform;
            if (Stage > 0)
                Generate(roots);
            else
                EaseAuthoredClimb(roots);

            var environment = Array.Find(roots, root => root.name == "Enviro");
            var upperGround = environment == null ? null : environment.transform.Find("Ground (3)");
            var shelf = upperGround == null ? null : upperGround.GetComponentInChildren<BoxCollider2D>();
            if (shelf == null) return;

            shelfReady = true;
            shelfTop = shelf.bounds.max.y;
            checkpoint = new Vector3(shelf.bounds.min.x + 2f, shelfTop + 1.15f, 0f);
            if (checkpointReached && player != null)
            {
                player.position = checkpoint;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) body.position = checkpoint;
                var camera = Camera.main;
                if (camera != null)
                    camera.transform.position = new Vector3(checkpoint.x, checkpoint.y + 1.5f, camera.transform.position.z);
                Physics2D.SyncTransforms();
            }
        }

        public static void StartGeneratedRun()
        {
            Stage = 1;
            checkpointReached = false;
            seed = Guid.NewGuid().GetHashCode();
        }

        public static bool TryAdvance(Collider2D collider)
        {
            if (!collider.CompareTag("Player") || SceneManager.GetActiveScene().buildIndex != LevelScene)
                return false;
            if (changingScene || Finished) return true;
            if (!Progression.RunActive) return true;
            if (Progression.EnemiesRemaining > 0)
            {
                GameMenu.ShowLockedExit();
                return true;
            }
            Progression.StageCleared();

            if (Stage + 1 == StageCount)
            {
                Finished = true;
                Progression.EndRun(true);
            }
            else
            {
                Stage++;
                checkpointReached = false;
                ReloadStage();
            }
            return true;
        }

        public static bool UpdateProgress()
        {
            if (changingScene || Finished || player == null) return false;
            var position = player.position;
            if (position.y < -22f)
            {
                Progression.EndRun(false);
                return false;
            }
            if (!shelfReady || checkpointReached || position.x < checkpoint.x || position.y < shelfTop + 0.85f
                || position.y > shelfTop + 1.35f)
                return false;

            checkpointReached = true;
            player.gameObject.SendMessage("Heal", 2, SendMessageOptions.DontRequireReceiver);
            return true;
        }

        private static void EaseAuthoredClimb(GameObject[] roots)
        {
            var environment = Array.Find(roots, root => root.name == "Enviro");
            if (environment == null) return;
            var env = environment.transform;
            var upperGround = env.Find("Ground (3)");
            var shelf = upperGround == null ? null : upperGround.GetComponentInChildren<BoxCollider2D>();
            if (shelf == null) return;

            // Keep the four clay pieces and form a readable, forward climb.
            for (int index = 0; index < 4; index++)
            {
                var step = env.Find("bgV2_2 (" + (index + 6) + ")");
                if (step == null) continue;
                var collider = step.GetComponent<BoxCollider2D>();
                if (collider == null) continue;
                float top = Mathf.Lerp(-3.36f, shelf.bounds.max.y, (index + 1) / 5f);
                step.position = new Vector3(shelf.bounds.min.x - 2.8f - (3 - index) * 5.5f,
                    step.position.y + top - collider.bounds.max.y, step.position.z);
            }
            Physics2D.SyncTransforms();
        }

        private static void ReloadStage()
        {
            changingScene = true;
            SceneManager.LoadScene(LevelScene);
        }

        private static void Generate(GameObject[] roots)
        {
            var env = Array.Find(roots, root => root.name == "Enviro");
            var enemies = Array.Find(roots, root => root.name == "Enemies");
            var exit = Array.Find(roots, root => root.GetComponent("WinTrigger") != null);
            var entry = Array.Find(roots, root => root.GetComponent("AutoDialogueTrigger") != null);
            if (env == null || enemies == null || exit == null) return;

            var upperGround = env.transform.Find("Ground (3)");
            var upperBackdrop = env.transform.Find("Background (3)");
            var endWall = env.transform.Find("Wall (2)");
            var platform = env.transform.Find("bgV2_2 (6)");
            if (upperGround == null || upperBackdrop == null || endWall == null || platform == null) return;
            var shelf = upperGround.GetComponentInChildren<BoxCollider2D>();
            var sample = platform.GetComponent<BoxCollider2D>();
            if (shelf == null || sample == null) return;
            if (entry != null) entry.SetActive(false);

            var random = new System.Random(unchecked(seed * 397 ^ Stage));
            float offset = 5f + (Stage - 1) * 9f + Next(random, 0f, 5f);
            float shelfEdge = shelf.bounds.min.x + offset;
            float shelfHeight = shelf.bounds.max.y;
            float platformTopOffset = sample.bounds.max.y - platform.position.y;
            float platformWidth = sample.bounds.size.x;
            Shift(upperGround, offset);
            Shift(upperBackdrop, offset);
            var lowerWall = env.transform.Find("Wall (1)");
            if (lowerWall != null) Shift(lowerWall, offset);
            Shift(endWall, offset);
            Shift(exit.transform, offset);
            Physics2D.SyncTransforms();
            var exitCollider = exit.GetComponent<Collider2D>();
            var endCollider = endWall.GetComponentInChildren<Collider2D>();
            if (exitCollider != null && endCollider != null)
            {
                // Keep enough approach space for the player's body before the solid end wall.
                float destination = Mathf.Min(exitCollider.bounds.center.x, endCollider.bounds.min.x - 2f);
                destination = Mathf.Clamp(destination, shelf.bounds.min.x + 5f, shelf.bounds.max.x - 2f);
                exit.transform.position += Vector3.right * (destination - exitCollider.bounds.center.x);
            }

            // Replace the authored approach with steps of bounded height and span.
            // The flat lower road stays open, so a missed jump drops onto a retry route.
            for (int index = 6; index <= 12; index++)
            {
                var existing = env.transform.Find("bgV2_2 (" + index + ")");
                if (existing != null && existing.position.x > 40f) existing.gameObject.SetActive(false);
            }

            var stairs = new GameObject("Generated clay steps").transform;
            stairs.SetParent(env.transform, false);
            int stepCount = 5 + Stage;
            float nextX = shelfEdge - 2.8f;
            for (int index = stepCount; index > 0; index--)
            {
                float width = Next(random, 4.2f, 5f);
                float top = Mathf.Lerp(-3.36f, shelfHeight, (float)index / (stepCount + 1))
                    + Next(random, -0.08f, 0.08f);
                var step = Object.Instantiate(platform.gameObject, stairs).transform;
                step.name = "Clay step " + index;
                step.localScale = new Vector3(width / platformWidth, 1f, 1f);
                step.position = new Vector3(nextX, top - platformTopOffset, 0f);
                step.gameObject.SetActive(true);
                nextX -= Next(random, 5.4f, 5.8f);
            }

            int lowerCount = 2;
            int upperCount = Stage == 1 ? 3 : 4;
            var lower = new System.Collections.Generic.List<Transform>();
            var upper = new System.Collections.Generic.List<Transform>();
            foreach (Transform enemy in enemies.transform)
            {
                if (enemy.position.y > 8f)
                    upper.Add(enemy);
                else if (enemy.position.x >= 0f)
                    lower.Add(enemy);
                else
                    enemy.gameObject.SetActive(false);
            }
            PlaceEnemies(lower, lowerCount, 32f, Mathf.Max(38f, nextX - 10f), -3.36f, random);
            PlaceEnemies(upper, upperCount, shelfEdge + 13f, exit.transform.position.x - 12f, shelfHeight, random);
            Physics2D.SyncTransforms();
        }

        private static void PlaceEnemies(System.Collections.Generic.List<Transform> enemies, int count,
            float left, float right, float floor, System.Random random)
        {
            for (int index = enemies.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                var selected = enemies[other];
                enemies[other] = enemies[index];
                enemies[index] = selected;
            }

            for (int index = 0; index < enemies.Count; index++)
            {
                var enemy = enemies[index];
                if (index >= count)
                {
                    enemy.gameObject.SetActive(false);
                    continue;
                }
                var collider = enemy.GetComponent<Collider2D>();
                var position = enemy.position;
                position.y += floor + 0.08f - collider.bounds.min.y;
                position.x = Mathf.Lerp(left, right, count == 1 ? 0.5f : (float)index / (count - 1))
                    + Next(random, -1f, 1f);
                enemy.position = position;
            }
        }

        private static void Shift(Transform transform, float offset)
        {
            transform.position += Vector3.right * offset;
        }

        private static float Next(System.Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
