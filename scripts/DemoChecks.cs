using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RageRefinement;
using Object = UnityEngine.Object;

namespace RageBench
{
    public sealed class Runner : MonoBehaviour
    {
        private static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private int failures;
        private GameMenu menu;
        public static void StartBenchmark() { var host = new GameObject("Expanded demo regressions"); DontDestroyOnLoad(host); host.AddComponent<Runner>(); }
        private void Expect(bool condition, string name)
        {
            if (condition) Debug.Log("DEMO PASS " + name);
            else { failures++; Debug.LogError("DEMO FAIL " + name); }
        }
        private static void Hurt(Health health, int amount, Vector2 source)
        {
            typeof(Health).GetProperty("IsInvincible").GetSetMethod(true).Invoke(health,new object[] {false});
            health.TakeDamage(new DamageData(amount,source,0,0));
        }
        private IEnumerator District(int index)
        {
            typeof(GameMenu).GetMethod("StartDistrict",Hidden).Invoke(menu,new object[] {index,false});
            yield return new WaitForSecondsRealtime(.7f);
            Time.timeScale = 0;
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath),name+".png"));
            yield return new WaitForSecondsRealtime(.2f);
        }
        private IEnumerator Start()
        {
            Application.runInBackground = true; Application.targetFrameRate = 60;
            Locale.Select(1);
            yield return new WaitForSecondsRealtime(7f);
            Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            yield return new WaitForSecondsRealtime(.3f);
            var args = Environment.GetCommandLineArgs();
            int profile = Array.IndexOf(args,"-rage-qa-profile");
            if (profile < 0) throw new InvalidOperationException("Isolated QA profile required.");
            string key = "RageRefinement.QA.Progression." + args[profile+1];
            PlayerPrefs.SetString(key,"{\"version\":1,\"clay\":5000,\"ranks\":[2,1,1,1],\"attempts\":3,\"victories\":0,\"storySeen\":true}");
            typeof(Progression).GetField("save",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,null);
            Expect(Progression.Clay == 5000 && Progression.Rank(0) == 2 && Progression.Rank(4) == 0,"legacy four-buff saves migrate without losing clay");
            for (int slot=4;slot<8;slot++) Expect(Progression.TryBuy(slot),"new trick purchase " + Progression.Names[slot]);
            menu = FindAnyObjectByType<GameMenu>();
            yield return District(1);
            Expect(FindObjectsByType<ClayEnemy>().Any(e=>e.Kind==ClayEnemyKind.Lobber),"Sunflower district contains lobbers");
            yield return District(2);
            var shield = FindObjectsByType<ClayEnemy>().First(e=>e.Kind==ClayEnemyKind.Shieldguard);
            var shieldHealth = shield.GetComponent<Health>();
            PlayerTricks.BeginRun();
            float facing = Mathf.Sign(shield.transform.localScale.x);
            int before = shieldHealth.CurrentHealth;
            Hurt(shieldHealth,2,shield.transform.position+Vector3.right*facing*5);
            int front = before-shieldHealth.CurrentHealth;
            before=shieldHealth.CurrentHealth;
            Hurt(shieldHealth,2,shield.transform.position-Vector3.right*facing*5);
            Expect(before-shieldHealth.CurrentHealth>front,"shieldguards take more damage from behind");
            yield return District(3);
            var mender = FindObjectsByType<ClayEnemy>().First(e=>e.Kind==ClayEnemyKind.Mender);
            var patient = DemoContent.Spawn(ClayEnemyKind.Regular,new Vector3(mender.transform.position.x+2,mender.GetComponent<Collider2D>().bounds.min.y,0),false);
            patient.enabled=false;
            var patientHealth=patient.GetComponent<Health>();
            Hurt(patientHealth,8,Vector2.zero);
            int wounded=patientHealth.CurrentHealth;
            typeof(ClayEnemy).GetField("nextMend",Hidden).SetValue(mender,-1f);
            yield return null;
            Expect(patientHealth.CurrentHealth>wounded,"menders really heal nearby enemies");
            yield return District(4);
            var splitter=FindObjectsByType<ClayEnemy>().First(e=>e.Kind==ClayEnemyKind.Splitter);
            int count=Progression.EnemiesRemaining;
            Hurt(splitter.GetComponent<Health>(),10000,Vector2.zero);
            Expect(Progression.EnemiesRemaining==count+1,"split children count immediately and keep exits locked");
            bool exitsReachable=true;
            for(int sample=0;sample<30;sample++)
            {
                yield return District(1+sample%5);
                var exit=FindAnyObjectByType<WinTrigger>().GetComponent<Collider2D>().bounds;
                var endBounds=GameObject.Find("Enviro").transform.Find("Wall (2)").GetComponentInChildren<Collider2D>().bounds;
                var shelf=GameObject.Find("Enviro").transform.Find("Ground (3)").GetComponentInChildren<BoxCollider2D>().bounds;
                exitsReachable &= exit.center.x<endBounds.min.x-1.5f && exit.center.x>shelf.min.x && exit.center.x<shelf.max.x;
            }
            Expect(exitsReachable,"30 generated district exits remain on reachable ground before the wall");
            yield return District(5);
            var boss=DemoContent.Boss;
            Expect(boss!=null && boss.Health.MaxHealth==280,"final district has the 280-health Kiln King");
            var player=FindAnyObjectByType<PlayerController>();
            var body=player.GetComponent<Rigidbody2D>();
            float floor=boss.GetComponents<Collider2D>().First(c=>c.enabled).bounds.min.y;
            player.transform.position=new Vector3(boss.transform.position.x-13,floor+1.08f,0);
            body.linearVelocity=Vector2.zero;
            Physics2D.SyncTransforms();
            Time.timeScale=1;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            float x=player.transform.position.x;
            int hearts=player.GetComponent<Health>().CurrentHealth, shells=Progression.Shields;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));
            InputSystem.Update();
            Debug.Log("DEMO dash input="+keyboard.leftShiftKey.wasPressedThisFrame+" control="+player.HasControl+" blocked="+GameMenu.BlockGameplayInput+" rank="+Progression.ActiveRank(4)+" active="+Progression.RunActive);
            typeof(PlayerTricks).GetMethod("Update",Hidden).Invoke(player.GetComponent<PlayerTricks>(),null);
            Expect(PlayerTricks.Dashing,"Shift triggers the purchased dash");
            Hurt(player.GetComponent<Health>(),1,Vector2.zero);
            Expect(player.GetComponent<Health>().CurrentHealth==hearts && Progression.Shields==shells,"dash blocks damage before consuming shells");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(.22f);
            Expect(Mathf.Abs(player.transform.position.x-x)>1.5f && PlayerTricks.Cooldown>0,"dash moves through physics and enters cooldown");
            Time.timeScale=0;
            var original=Resources.FindObjectsOfTypeAll<AttackData>().First(a=>a.Kind==AttackKind.Projectile && a.ProjectilePrefab!=null);
            int pellets=original.ProjectileCount;
            var forked=PlayerTricks.PrepareProjectile(original);
            Expect(forked.ProjectileCount==Mathf.Min(5,pellets+1) && original.ProjectileCount==pellets,"forked shots add pellets without mutating shared attack assets");
            Vector3 origin=Camera.main.ViewportToWorldPoint(new Vector3(.5f,.5f,Camera.main.WorldToViewportPoint(Vector3.zero).z));
            var shot=Instantiate(original.ProjectilePrefab,origin,Quaternion.identity);
            typeof(Projectile).GetMethod("Launch",Hidden).Invoke(shot,new object[]{original,Vector2.right,(LayerMask)~(1<<player.gameObject.layer),(LayerMask)1,null,10f});
            var wall=new GameObject("Ricochet test wall"); wall.transform.position=origin+Vector3.right;
            var wallCollider=wall.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            typeof(Projectile).GetMethod("OnTriggerEnter2D",Hidden).Invoke(shot,new object[]{wallCollider});
            Expect(shot.GetComponent<Rigidbody2D>().linearVelocity.x<0 && !(bool)typeof(Projectile).GetField("_spent",Hidden).GetValue(shot),"ricochet reflects a real projectile instead of despawning it");
            Destroy(wall); Destroy(shot.gameObject);
            PlayerTricks.BeginRun();
            int[] hits=new int[6];
            for(int hit=0;hit<6;hit++) { int hp=boss.Health.CurrentHealth; Hurt(boss.Health,1,player.transform.position); hits[hit]=hp-boss.Health.CurrentHealth; }
            Expect(hits[5]>hits[0],"rhythm punch produces a stronger sixth hit at rank one");
            Hurt(boss.Health,120,player.transform.position);
            player.transform.position=new Vector3(boss.transform.position.x-9,floor+1.08f,0); body.linearVelocity=Vector2.zero;
            typeof(Health).GetProperty("IsInvincible").GetSetMethod(true).Invoke(player.GetComponent<Health>(),new object[]{true});
            Time.timeScale=1;
            yield return new WaitForSecondsRealtime(.5f);
            Expect(boss.Phase==2 && FindObjectsByType<ClayEnemy>().Count(e=>e.Kind==ClayEnemyKind.Rushling)>=2,"boss enters phase two and summons reinforcements");
            bool sawTelegraph=false, sawShot=false;
            float observeUntil=Time.realtimeSinceStartup+4;
            while(Time.realtimeSinceStartup<observeUntil)
            {
                typeof(Health).GetProperty("IsInvincible").GetSetMethod(true).Invoke(player.GetComponent<Health>(),new object[]{true});
                sawTelegraph |= !string.IsNullOrEmpty(boss.Telegraph);
                sawShot |= FindObjectsByType<Projectile>().Length>0;
                yield return null;
            }
            Expect(sawTelegraph && sawShot,"boss telegraphs and fires real attacks");
            Debug.Log("DEMO camera="+Camera.main.transform.position+" player="+player.transform.position+" boss="+boss.transform.position+" target="+typeof(CameraController2D).GetField("_target",Hidden).GetValue(CameraController2D.Instance));
            var bossArt=boss.GetComponentsInChildren<SpriteRenderer>().First(s=>s.enabled);
            Debug.Log("DEMO boss art="+bossArt.bounds+" layer="+bossArt.sortingLayerName+" order="+bossArt.sortingOrder+" camera size="+Camera.main.orthographicSize);
            var characterArt=boss.GetComponentsInChildren<SpriteRenderer>().First(s=>!s.enabled);
            Expect(bossArt.sortingLayerID==characterArt.sortingLayerID,"boss uses the visible enemy sorting layer");
            Expect(boss.GetComponents<Collider2D>().Count(c=>c.enabled)==1,"boss uses one fitted collider without the old body collider");
            yield return Capture("kiln-king");
            Time.timeScale=0;
            typeof(GameMenu).GetMethod("OpenMenu",Hidden).Invoke(menu,null);
            typeof(GameMenu).GetMethod("ShowTricks",Hidden).Invoke(menu,null);
            yield return null; yield return null;
            yield return Capture("tricks");
            typeof(GameMenu).GetMethod("ShowDistricts",Hidden).Invoke(menu,null);
            yield return null; yield return null;
            yield return Capture("districts");
            Debug.Log("DEMO complete; failures="+failures);
            if(Array.IndexOf(args,"-rage-qa-preview")>=0)yield return new WaitForSecondsRealtime(45);
            Application.Quit(failures==0?0:1);
        }
    }
}
