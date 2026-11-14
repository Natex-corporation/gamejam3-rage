using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using RageRefinement;

namespace RageBench
{
    public sealed class DamageProbe : MonoBehaviour, IDamageable
    {
        public int Hits;
        public void TakeDamage(DamageData damage) { Hits++; }
    }

    public static class ProjectileChecks
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public static int Failures { get; private set; }
        private static AttackData attack;

        private static void Expect(bool condition, string name)
        {
            if (!condition) { Failures++; Debug.LogError("PROJECTILE FAIL " + name); }
            else Debug.Log("PROJECTILE PASS " + name);
        }
        private static Projectile Shot(Camera camera, Vector2 viewport)
        {
            float depth = camera.WorldToViewportPoint(Vector3.zero).z;
            var host = new GameObject("Projectile regression probe");
            host.transform.position = camera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
            var body = host.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            host.AddComponent<BoxCollider2D>().size = new Vector2(.6f, .6f);
            var shot = host.AddComponent<Projectile>();
            typeof(Projectile).GetField("_attack", Private).SetValue(shot, attack);
            typeof(Projectile).GetField("_targetLayers", Private).SetValue(shot, (LayerMask)1);
            Physics2D.SyncTransforms();
            return shot;
        }
        private static void Clean(Projectile shot)
        {
            if (shot == null) return;
            shot.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(shot.gameObject);
        }
        public static IEnumerator Run()
        {
            Failures = 0;
            var camera = Camera.main;
            Expect(camera != null, "gameplay camera exists");
            if (camera == null) yield break;
            attack = ScriptableObject.CreateInstance<AttackData>();
            attack.Damage = 1;
            foreach (var point in new[] {new Vector2(.5f,.5f), new Vector2(-.2f,.5f),
                new Vector2(1.2f,.5f), new Vector2(.5f,-.2f), new Vector2(.5f,1.2f), new Vector2(0,.5f)})
            {
                bool outside = point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1;
                var shot = Shot(camera, point);
                typeof(Projectile).GetMethod("FixedUpdate", Private).Invoke(shot, null);
                bool spent = (bool)typeof(Projectile).GetField("_spent", Private).GetValue(shot);
                Expect(spent == outside, "physics cleanup at " + point);
                Clean(shot);
            }
            foreach (bool outside in new[] {false, true})
            {
                var shot = Shot(camera, new Vector2(outside ? 1.2f : .5f, .5f));
                var target = new GameObject("Damage regression probe");
                target.transform.position = shot.transform.position;
                var probe = target.AddComponent<DamageProbe>();
                var collider = target.AddComponent<BoxCollider2D>();
                Physics2D.SyncTransforms();
                // Invoke the real patched collision path to cover a fast shot crossing in one physics step.
                typeof(Projectile).GetMethod("OnTriggerEnter2D", Private).Invoke(shot, new object[] {collider});
                Expect(probe.Hits == (outside ? 0 : 1), outside ? "offscreen hit suppressed" : "onscreen hit retained");
                target.SetActive(false);
                UnityEngine.Object.Destroy(target);
                Clean(shot);
            }
            var moving = Shot(camera, new Vector2(.98f, .5f));
            moving.GetComponent<Rigidbody2D>().linearVelocity = Vector2.right * 150;
            yield return new WaitForSeconds(.25f);
            Expect(moving == null, "moving projectile destroyed after exiting viewport");
            Clean(moving);
            var stationary = Shot(camera, new Vector2(.5f, .5f));
            var originalPosition = camera.transform.position;
            camera.transform.position += Vector3.right * 200;
            Expect(ProjectileBounds.IsOutsideView(stationary), "bounds follow camera movement");
            camera.transform.position = originalPosition;
            Clean(stationary);
            UnityEngine.Object.Destroy(attack);
            Debug.Log("PROJECTILE complete; failures=" + Failures);
        }
    }
}
