using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RageRefinement
{
    [DefaultExecutionOrder(900)]
    public sealed class PlayerTricks : MonoBehaviour
    {
        private static readonly Dictionary<AttackData, AttackData> prepared = new Dictionary<AttackData, AttackData>();
        private static PlayerTricks current;
        private static int rhythm;
        private PlayerController controller;
        private Rigidbody2D body;
        private float dashUntil, readyAt, direction;
        public static bool Dashing { get { return current != null && Time.time < current.dashUntil; } }
        public static float Cooldown { get { return current == null ? 0 : Mathf.Max(0,current.readyAt - Time.time); } }

        public static void Install(Component player)
        {
            if (player.GetComponent<PlayerTricks>() == null) player.gameObject.AddComponent<PlayerTricks>();
        }
        public static void BeginRun()
        {
            rhythm = 0;
            foreach (var attack in prepared.Values) if (attack != null) Destroy(attack);
            prepared.Clear();
        }
        public static int RhythmDamage(int damage)
        {
            int rank = Progression.ActiveRank(7);
            if (rank <= 0) return damage;
            rhythm++;
            return rhythm % (7 - rank) == 0 ? damage * 2 : damage;
        }
        public static AttackData PrepareProjectile(AttackData original)
        {
            int rank = Progression.ActiveRank(6);
            if (original == null || rank <= 0 || !Progression.RunActive) return original;
            AttackData value;
            if (prepared.TryGetValue(original, out value) && value != null) return value;
            value = Instantiate(original);
            value.name = original.name + " (forked clay)";
            value.ProjectileCount = Mathf.Min(5,original.ProjectileCount + rank);
            value.ProjectileSpreadAngle = 18 + rank * 8;
            prepared[original] = value;
            return value;
        }
        private void Awake()
        {
            current = this;
            controller = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        private void Update()
        {
            if (!Progression.RunActive || GameMenu.BlockGameplayInput || !controller.HasControl || Progression.ActiveRank(4) <= 0) return;
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            bool dash = keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)
                || pad != null && pad.buttonEast.wasPressedThisFrame;
            if (!dash || Time.time < readyAt) return;
            direction = Mathf.Sign(transform.localScale.x);
            dashUntil = Time.time + .18f;
            readyAt = Time.time + 4 - Progression.ActiveRank(4) * .75f;
        }
        private void FixedUpdate()
        {
            if (Dashing && !GameMenu.GameplayPaused) body.linearVelocity = new Vector2(direction * 17,0);
        }
    }

    public sealed class ProjectileTricks : MonoBehaviour
    {
        private int bounces;
        private Rigidbody2D body;
        private Vector2 previous;
        private Collider2D lastGround;
        private float ignoreUntil;

        public static void Launched(Component projectile, AttackData attack, Vector2 direction, LayerMask targets)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (!Progression.RunActive || player == null || (targets.value & (1 << player.gameObject.layer)) != 0) return;
            int rank = Progression.ActiveRank(5);
            if (rank <= 0) return;
            var tricks = projectile.gameObject.AddComponent<ProjectileTricks>();
            tricks.bounces = rank;
            tricks.body = projectile.GetComponent<Rigidbody2D>();
            tricks.previous = tricks.body.position;
        }

        public static bool TryBounce(Component projectile, Collider2D ground, LayerMask groundLayers)
        {
            if (ground == null || (groundLayers.value & (1 << ground.gameObject.layer)) == 0) return false;
            var tricks = projectile.GetComponent<ProjectileTricks>();
            if (tricks == null || tricks.body == null) return false;
            if (ground == tricks.lastGround && Time.time < tricks.ignoreUntil) return true;
            if (tricks.bounces <= 0) return false;
            tricks.bounces--;
            tricks.lastGround = ground;
            tricks.ignoreUntil = Time.time + .1f;
            var closest = ground.ClosestPoint(tricks.previous);
            Vector2 normal = tricks.previous - closest;
            if (normal.sqrMagnitude < .0001f)
            {
                var velocity = tricks.body.linearVelocity;
                normal = Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y) ? new Vector2(-Mathf.Sign(velocity.x),0) : new Vector2(0,-Mathf.Sign(velocity.y));
            }
            normal.Normalize();
            tricks.body.linearVelocity = Vector2.Reflect(tricks.body.linearVelocity,normal);
            tricks.body.position += normal * .12f;
            return true;
        }
        private void FixedUpdate()
        {
            if (body == null || GameMenu.GameplayPaused) return;
            previous = body.position;
            var velocity = body.linearVelocity;
            if (velocity.sqrMagnitude > .01f) transform.rotation = Quaternion.Euler(0,0,Mathf.Atan2(velocity.y,velocity.x) * Mathf.Rad2Deg);
        }
    }
}
