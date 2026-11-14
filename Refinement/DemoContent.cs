using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RageRefinement
{
    public enum ClayEnemyKind { Regular, Lobber, Shieldguard, Mender, Splitter, Rushling }

    public static class DemoContent
    {
        public static readonly string[] Districts = { "Paper Lane", "Sunflower Steps", "Glaze Alley", "Mossy Rooftops", "The Clayworks", "The Royal Kiln" };
        public static readonly int[] EnemyHealth = { 10, 18, 26, 36, 48, 60 };
        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo Stats = typeof(Enemy).GetField("_stats", Hidden);
        private static readonly FieldInfo Maximum = typeof(Health).GetField("_maxHealth", Hidden);
        private static readonly MethodInfo SetCurrent = typeof(Health).GetProperty("CurrentHealth").GetSetMethod(true);
        private static readonly List<Enemy> prototypes = new List<Enemy>();
        private static Transform prototypeRoot;
        private static Sprite bossSprite;
        public static KilnKing Boss { get; private set; }

        public static void SetHealth(Health health, int value)
        {
            Maximum.SetValue(health, value);
            SetCurrent.Invoke(health, new object[] {value});
        }

        public static Sprite LoadSprite(string file, Vector2 pivot, float unitsHigh)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var path = Path.Combine(Application.dataPath, "Refinement", "Assets", file);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true)) throw new InvalidDataException(path);
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, texture.height / unitsHigh,
                0, SpriteMeshType.FullRect);
        }

        public static void Install(GameObject[] roots)
        {
            Boss = null;
            prototypes.Clear();
            var group = Array.Find(roots, root => root.name == "Enemies");
            if (group == null) return;
            prototypeRoot = new GameObject("Clay enemy prototypes").transform;
            prototypeRoot.gameObject.SetActive(false);
            var originals = group.GetComponentsInChildren<Enemy>(true);
            foreach (var original in originals)
            {
                bool duplicate = false;
                foreach (var prototype in prototypes)
                    if (((EnemyStats)Stats.GetValue(prototype)).name == ((EnemyStats)Stats.GetValue(original)).name) duplicate = true;
                if (duplicate) continue;
                var copy = Object.Instantiate(original, prototypeRoot);
                copy.gameObject.SetActive(false);
                prototypes.Add(copy);
            }
            if (prototypes.Count == 0) return;
            int index = 0;
            foreach (var enemy in originals)
            {
                if (!enemy.gameObject.activeInHierarchy) continue;
                ClayEnemyKind kind = ClayEnemyKind.Regular;
                if (Course.Stage >= 1 && index % 4 == 1) kind = ClayEnemyKind.Lobber;
                if (Course.Stage >= 2 && index % 4 == 2) kind = ClayEnemyKind.Shieldguard;
                if (Course.Stage >= 3 && index % 5 == 3) kind = ClayEnemyKind.Mender;
                if (Course.Stage >= 4 && index % 5 == 4) kind = ClayEnemyKind.Splitter;
                Configure(enemy, kind, false);
                index++;
            }
            var environment = Array.Find(roots, root => root.name == "Enviro");
            var upper = environment == null ? null : environment.transform.Find("Ground (3)");
            var shelf = upper == null ? null : upper.GetComponentInChildren<BoxCollider2D>();
            var exit = Array.Find(roots, root => root.GetComponent<WinTrigger>() != null);
            if (shelf != null && exit != null && Course.Stage > 0)
            {
                int extra = Mathf.Min(4, Course.Stage);
                for (int slot = 0; slot < extra; slot++)
                {
                    var kind = slot == 0 ? ClayEnemyKind.Lobber : slot == 1 ? ClayEnemyKind.Shieldguard : slot == 2 ? ClayEnemyKind.Mender : ClayEnemyKind.Splitter;
                    if ((int)kind > Course.Stage + 1) kind = ClayEnemyKind.Regular;
                    Spawn(kind, new Vector3(shelf.bounds.min.x + 9 + slot * 7, shelf.bounds.max.y, 0), false);
                }
            }
            if (environment != null)
            {
                Color tint = Course.Stage == 1 ? new Color(1,.98f,.91f) : Course.Stage == 2 ? new Color(.94f,.94f,1) :
                    Course.Stage == 3 ? new Color(.92f,1,.92f) : Course.Stage >= 4 ? new Color(1,.92f,.87f) : Color.white;
                foreach (var renderer in environment.GetComponentsInChildren<SpriteRenderer>()) renderer.color = tint;
            }
            if (Course.Stage == Course.StageCount - 1 && shelf != null && exit != null)
            {
                // The final shelf is an arena. Keep a short approach and give the boss room to telegraph.
                foreach (var enemy in Object.FindObjectsByType<Enemy>())
                    if (enemy.transform.position.y > 7f) enemy.gameObject.SetActive(false);
                var boss = Spawn(ClayEnemyKind.Regular, new Vector3(exit.transform.position.x - 15, shelf.bounds.max.y + .04f, 0), false);
                boss.name = "The Kiln King";
                boss.enabled = false;
                var marker = boss.GetComponent<ClayEnemy>();
                if (marker != null) marker.enabled = false;
                var originalArt = boss.GetComponentInChildren<SpriteRenderer>();
                int characterLayer = originalArt == null ? 0 : originalArt.sortingLayerID;
                int characterOrder = originalArt == null ? 20 : originalArt.sortingOrder + 2;
                foreach (var renderer in boss.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
                var box = boss.GetComponent<BoxCollider2D>();
                if (box == null) box = boss.gameObject.AddComponent<BoxCollider2D>();
                foreach (var collider in boss.GetComponents<Collider2D>()) collider.enabled = collider == box;
                if (box != null) { box.size = new Vector2(3.6f, 4.2f); box.offset = new Vector2(0,2.1f); }
                boss.transform.localScale = Vector3.one;
                var art = new GameObject("Kiln King clay sculpture");
                art.transform.SetParent(boss.transform, false);
                var sprite = art.AddComponent<SpriteRenderer>();
                if (bossSprite == null) bossSprite = LoadSprite("kiln-king.png", new Vector2(.5f,0), 4.8f);
                sprite.sprite = bossSprite;
                sprite.sortingLayerID = characterLayer;
                sprite.sortingOrder = characterOrder;
                SetHealth(boss.GetComponent<Health>(), 280);
                Boss = boss.gameObject.AddComponent<KilnKing>();
                Boss.Initialize(sprite, shelf.bounds.min.x + 6, exit.transform.position.x - 6, shelf.bounds.max.y);
            }
            Physics2D.SyncTransforms();
        }

        public static Enemy Spawn(ClayEnemyKind kind, Vector3 feet, bool mini)
        {
            if (prototypes.Count == 0) return null;
            var source = prototypes[kind == ClayEnemyKind.Lobber ? Mathf.Min(1, prototypes.Count - 1) : 0];
            var copy = Object.Instantiate(source);
            copy.transform.SetParent(null, true);
            copy.gameObject.SetActive(true);
            Configure(copy, kind, mini);
            var bounds = copy.GetComponent<Collider2D>().bounds;
            copy.transform.position += feet - new Vector3(bounds.center.x, bounds.min.y, 0);
            Physics2D.SyncTransforms();
            // Dynamic split children must count before the next physics event can reach an exit.
            if (mini) Progression.TrackHealth(copy.GetComponent<Health>());
            return copy;
        }

        private static AttackData ProjectileAttack()
        {
            foreach (var prototype in prototypes)
                foreach (var attack in ((EnemyStats)Stats.GetValue(prototype)).Attacks)
                    if (attack != null && attack.Kind == AttackKind.Projectile && attack.ProjectilePrefab != null) return attack;
            return null;
        }

        public static AttackData MakeBossShot()
        {
            var source = ProjectileAttack();
            if (source == null) return null;
            var attack = Object.Instantiate(source);
            attack.Damage = 2;
            attack.ProjectileCount = 1;
            attack.ProjectileSpeed = 10;
            attack.ProjectileLifetime = 4;
            attack.ProjectileGravityScale = 0;
            attack.KnockbackForce = 3;
            return attack;
        }

        private static void Configure(Enemy enemy, ClayEnemyKind kind, bool mini)
        {
            var stats = Object.Instantiate((EnemyStats)Stats.GetValue(enemy));
            var attacks = new AttackData[stats.Attacks.Length];
            for (int index = 0; index < attacks.Length; index++)
            {
                if (stats.Attacks[index] == null) continue;
                attacks[index] = Object.Instantiate(stats.Attacks[index]);
                attacks[index].Damage = Course.Stage >= 3 ? 2 : 1;
                attacks[index].TelegraphLeadTime = Mathf.Max(.45f, attacks[index].TelegraphLeadTime);
            }
            stats.Attacks = attacks;
            if (kind == ClayEnemyKind.Lobber)
            {
                var projectile = ProjectileAttack();
                if (projectile != null)
                {
                    var lob = Object.Instantiate(projectile);
                    lob.Damage = Course.Stage >= 3 ? 2 : 1;
                    lob.UseInitialDirection = true;
                    lob.InitialDirection = new Vector2(1,1.1f);
                    lob.AimAtTarget = true;
                    lob.ProjectileGravityScale = 1;
                    lob.ProjectileSpeed = 17;
                    lob.ProjectileCount = 2;
                    lob.ProjectileSpreadAngle = 10;
                    lob.ProjectileLifetime = 3;
                    lob.TelegraphLeadTime = .8f;
                    stats.Attacks = new[] {lob};
                }
                stats.KeepDistance = true; stats.AttackRange = 11; stats.AggroRange = 12; stats.LoseRange = 17; stats.AttackCooldown = 2.2f;
            }
            if (kind == ClayEnemyKind.Shieldguard) { stats.ChaseSpeed *= .7f; stats.KnockbackMultiplier = .3f; stats.AttackCooldown = 1.7f; }
            if (kind == ClayEnemyKind.Mender) { stats.ChaseSpeed *= .8f; stats.AttackCooldown = 2; }
            if (kind == ClayEnemyKind.Rushling) { stats.ChaseSpeed *= 1.35f; stats.AggroRange = 10; stats.AttackCooldown = .8f; }
            Stats.SetValue(enemy, stats);
            var marker = enemy.GetComponent<ClayEnemy>();
            if (marker == null) marker = enemy.gameObject.AddComponent<ClayEnemy>();
            marker.Initialize(kind, mini);
            if (mini) enemy.transform.localScale *= .65f;
            else if (kind == ClayEnemyKind.Shieldguard) enemy.transform.localScale *= 1.15f;
            typeof(Enemy).GetField("_baseScaleX", Hidden).SetValue(enemy, Mathf.Abs(enemy.transform.localScale.x));
            int maximum = mini ? Mathf.Max(4, EnemyHealth[Course.Stage] / 4) : EnemyHealth[Course.Stage];
            if (kind == ClayEnemyKind.Shieldguard) maximum += maximum / 3;
            SetHealth(enemy.GetComponent<Health>(), maximum);
            if (kind != ClayEnemyKind.Regular) enemy.name = kind.ToString();
        }
    }

    public sealed class ClayEnemy : MonoBehaviour
    {
        public ClayEnemyKind Kind { get; private set; }
        private Health health;
        private bool miniature;
        private float nextMend;
        private SpriteRenderer[] art;
        private Color tint;
        public void Initialize(ClayEnemyKind kind, bool mini)
        {
            Kind = kind; miniature = mini;
            health = GetComponent<Health>();
            art = GetComponentsInChildren<SpriteRenderer>();
            tint = kind == ClayEnemyKind.Lobber ? new Color(.8f,.63f,1) : kind == ClayEnemyKind.Shieldguard ? new Color(.55f,.84f,1) :
                kind == ClayEnemyKind.Mender ? new Color(.6f,1,.55f) : kind == ClayEnemyKind.Splitter ? new Color(1,.65f,.83f) : Color.white;
            foreach (var sprite in art) sprite.color = tint;
            nextMend = Time.time + 5;
            health.OnDeath += Split;
        }
        private void Update()
        {
            if (GameMenu.GameplayPaused || !Progression.RunActive || health.CurrentHealth <= 0) return;
            if (Kind == ClayEnemyKind.Mender && Time.time >= nextMend)
            {
                nextMend = Time.time + 5;
                foreach (var enemy in Object.FindObjectsByType<Enemy>())
                    if (enemy.gameObject != gameObject && (enemy.transform.position - transform.position).sqrMagnitude < 49)
                        enemy.GetComponent<Health>().Heal(2);
                foreach (var sprite in art) if (sprite != null) sprite.color = new Color(.85f,1,.7f);
            }
            else foreach (var sprite in art) if (sprite != null) sprite.color = Color.Lerp(sprite.color, tint, Time.deltaTime * 4);
        }
        private void Split()
        {
            if (miniature || Kind != ClayEnemyKind.Splitter || !Progression.RunActive) return;
            var feet = GetComponent<Collider2D>().bounds.min.y;
            DemoContent.Spawn(ClayEnemyKind.Rushling, new Vector3(transform.position.x - .7f,feet,0), true);
            DemoContent.Spawn(ClayEnemyKind.Rushling, new Vector3(transform.position.x + .7f,feet,0), true);
        }
    }

    public sealed class KilnKing : MonoBehaviour
    {
        private Health health;
        private Rigidbody2D body;
        private SpriteRenderer art;
        private AttackData shot;
        private float left, right, floor, timer, chargeUntil;
        private int move;
        private bool winding, summoned;
        private float flashUntil;
        private float direction;
        public string Telegraph { get; private set; }
        public int Phase { get { return health.CurrentHealth * 2 <= health.MaxHealth ? 2 : 1; } }
        public Health Health { get { return health; } }
        public void Initialize(SpriteRenderer sprite, float minimum, float maximum, float ground)
        {
            art = sprite; left = minimum; right = maximum; floor = ground;
            health = GetComponent<Health>(); body = GetComponent<Rigidbody2D>();
            body.gravityScale = 2; body.freezeRotation = true;
            shot = DemoContent.MakeBossShot();
            health.OnDeath += () => { Telegraph = ""; Destroy(gameObject); };
            health.OnDamaged += damage => flashUntil = Time.time + .1f;
            timer = Time.time + 2;
        }
        private void Update()
        {
            if (!Progression.RunActive || GameMenu.GameplayPaused || health.CurrentHealth <= 0) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            var offset = player.transform.position - transform.position;
            float wobble = Mathf.Sin(Mathf.Floor(Time.time * 10) * .9f) * .015f;
            art.transform.localScale = new Vector3(1 + wobble,1 - wobble,1);
            art.color = Time.time < flashUntil ? new Color(1,.5f,.35f) : winding ? new Color(1,.76f,.4f) : Phase == 2 ? new Color(1,.75f,.65f) : Color.white;
            if (Mathf.Abs(offset.x) > 24 || Mathf.Abs(offset.y) > 8)
            {
                body.linearVelocity = new Vector2(0,body.linearVelocity.y);
                winding = false; Telegraph = ""; timer = Time.time + .5f;
                return;
            }
            if (Phase == 2 && !summoned)
            {
                summoned = true;
                DemoContent.Spawn(ClayEnemyKind.Rushling, new Vector3(transform.position.x - 4,floor,0), true);
                DemoContent.Spawn(ClayEnemyKind.Rushling, new Vector3(transform.position.x + 4,floor,0), true);
            }
            if (Time.time < chargeUntil)
            {
                float velocity = transform.position.x <= left + 1 && direction < 0 || transform.position.x >= right - 1 && direction > 0 ? 0 : direction * 10;
                body.linearVelocity = new Vector2(velocity,body.linearVelocity.y);
                return;
            }
            body.linearVelocity = new Vector2(0,body.linearVelocity.y);
            if (Time.time < timer) return;
            if (!winding)
            {
                winding = true;
                direction = Mathf.Sign(offset.x);
                Telegraph = move == 0 ? "Slam incoming · Jump!" : move == 1 ? "Volley incoming · Keep moving!" : "Charge incoming · Dodge!";
                art.color = new Color(1,.76f,.4f);
                timer = Time.time + (Phase == 2 ? .75f : 1.1f);
            }
            else
            {
                winding = false; Telegraph = "";
                art.color = Phase == 2 ? new Color(1,.75f,.65f) : Color.white;
                if (move == 0) { Fire(new Vector2(-1,0),new Vector2(transform.position.x - 2,floor + .4f)); Fire(new Vector2(1,0),new Vector2(transform.position.x + 2,floor + .4f)); }
                else if (move == 1 && shot != null)
                {
                    shot.ProjectileCount = Phase == 2 ? 5 : 3; shot.ProjectileSpreadAngle = Phase == 2 ? 55 : 32;
                    Fire(((Vector2)player.transform.position - new Vector2(transform.position.x, floor+2.3f)).normalized, new Vector2(transform.position.x, floor+2.3f));
                    shot.ProjectileCount = 1;
                }
                else chargeUntil = Time.time + .65f;
                move = (move + 1) % 3;
                timer = Time.time + (Phase == 2 ? 1.5f : 2.1f);
            }
        }
        private void Fire(Vector2 direction, Vector2 origin)
        {
            if (shot == null) return;
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            Projectile.Fire(shot, origin, direction, (LayerMask)(1 << player.gameObject.layer), (LayerMask)0);
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (Time.time >= chargeUntil || GameMenu.GameplayPaused || !collision.gameObject.CompareTag("Player")) return;
            var target = collision.gameObject.GetComponent<Health>();
            if (target != null) target.TakeDamage(new DamageData(2,transform.position,4,.15f));
        }
        private void FixedUpdate()
        {
            if (body != null && !GameMenu.GameplayPaused && (body.position.x < left || body.position.x > right))
                body.position = new Vector2(Mathf.Clamp(body.position.x,left,right),body.position.y);
        }
        private void OnDestroy() { if (shot != null) Destroy(shot); }
    }
}
