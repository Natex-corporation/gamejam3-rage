using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RageRefinement;
using DialogueSystem.Runtime;

namespace RageBench {
public sealed class Runner : MonoBehaviour {
 private Keyboard kb;
 private Key left,right,jump,talk,attack;
 private Key[] keys = new Key[0];
 private PlayerController p;
 private Health hp;
 private int loads, deaths, enemiesDefeated, totalDamage;
 private float nextAttack, attackEnd;
 private float nextDodge;
 private bool failed;
 private bool jumpLanded;

 public static void StartBenchmark() {
   var go = new GameObject("Full input playthrough"); DontDestroyOnLoad(go); go.AddComponent<Runner>();
 }

 private static object Field(object item,string name) {
   return item.GetType().GetField(name, BindingFlags.Instance|BindingFlags.NonPublic).GetValue(item);
 }

 private void Set(params Key[] value) {
   if (keys.SequenceEqual(value)) return;
   keys = value;
   InputSystem.QueueStateEvent(kb,new KeyboardState(keys));
 }

 private void Die(string message) {
    if (failed) return;
    failed=true; Debug.LogError("RUN FAIL " + message); Application.Quit(1);
 }

 private IEnumerator Start() {
  Application.runInBackground = true;
  Application.targetFrameRate = 90;
  var type=typeof(Locale);
  type.GetField("language",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,1);
  var refresh=(Action)type.GetField("Changed",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
  if(refresh!=null)refresh();
  foreach(var device in InputSystem.devices.ToArray())
   if(device is Keyboard || device is Gamepad || device is Mouse) InputSystem.DisableDevice(device);
  kb = InputSystem.AddDevice<Keyboard>();
  left=Key.LeftArrow; right=Key.RightArrow;
  jump=GetKey(2); attack=GetKey(3); talk=GetKey(4);
  SceneManager.sceneLoaded += TrackScene;
  yield return null;
  Set(Key.Escape);
  yield return new WaitForSecondsRealtime(0.18f);
  Set();
  var menu = FindAnyObjectByType<GameMenu>();
  var button=menu.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList().Single(x=>x.text=="Start game" || x.text=="Start run");
  using(var evt=NavigationSubmitEvent.GetPooled()) {evt.target=button;button.SendEvent(evt);}
  yield return new WaitForSecondsRealtime(0.7f);
  if (SceneManager.GetActiveScene().buildIndex==1) yield return PlayIntro();
  else if (SceneManager.GetActiveScene().buildIndex!=2) {Die("Start did not load the game"); yield break;}
  if (failed) yield break;
  for (int run=0;run<18 && !Progression.Won;run++) {
   while(Progression.RunActive && !failed) {
    yield return new WaitForSecondsRealtime(.25f);
    yield return PlayStage();
   }
   if(failed) yield break;
   if(Progression.Won) break;
   Debug.Log("RUN DEFEAT attempt="+Progression.Attempts+" clay="+Progression.Clay+" kills="+Progression.RunKills);
   Set();
   Submit(menu,"Workshop");
   yield return null;yield return null;
   bool bought;
   do {
    bought=false;
    // Damage first, then sustain, then a larger reserve for climbing and ranged enemies.
    foreach(int slot in new[]{0,3,1,2}) {
     if(Progression.Rank(slot)>=Progression.Limits[slot] || Progression.Clay<Progression.Cost(slot)) continue;
     int before=Progression.Rank(slot);
     Submit(menu,Progression.Names[slot],true);
     yield return null;yield return null;
     if(Progression.Rank(slot)!=before+1) {Die("workshop purchase failed");yield break;}
     Debug.Log("RUN BOUGHT "+Progression.Names[slot]+" rank="+Progression.Rank(slot)+" balance="+Progression.Clay);
     bought=true;
    }
   } while(bought);
   Submit(menu,"Start next run");
   yield return new WaitForSecondsRealtime(.5f);
  }
  if(!Course.Finished || !Progression.Won || !GameMenu.GameplayPaused) {Die("the incremental run never reached the ending");yield break;}
  var end=menu.GetComponent<UIDocument>().rootVisualElement.Query<Label>().ToList().Any(label=>label.text=="Course complete");
  if(!end){Die("English ending text missing");yield break;}
  Debug.Log("RUN COMPLETE "+(int)Time.realtimeSinceStartup+" seconds, "+deaths+" deaths, "+enemiesDefeated+" enemies defeated, "+totalDamage+" total damage");
  Set();
  yield return new WaitForSecondsRealtime(.25f);

  Application.Quit();
 }

 private static void Submit(GameMenu menu,string text,bool prefix=false) {
  var button=menu.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList()
    .FirstOrDefault(b=>b.enabledInHierarchy && (prefix?b.text.StartsWith(text,StringComparison.Ordinal):b.text==text));
  if(button==null) throw new InvalidOperationException("Missing button "+text);
  using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}
 }

 private Key GetKey(int index) {return (Key)Enum.Parse(typeof(Key),KeyboardBindings.KeyFor(index),true);}

 private void TrackScene(Scene scene, LoadSceneMode mode) {
   loads++;
   p=FindAnyObjectByType<PlayerController>();
   if (p!=null) {
    hp=p.GetComponent<Health>();
    int old=hp.CurrentHealth;
    hp.OnHealthChanged+=(value,max)=>{
     if(value<old) {totalDamage+=old-value; Debug.Log("RUN hit stage="+(Course.Stage+1)+" HP="+value+" at "+p.transform.position);}
     old=value;
    };
    hp.OnDeath+=()=>{deaths++;Debug.Log("RUN death at "+p.transform.position);};
   }
   foreach(var enemy in FindObjectsByType<Enemy>()) {
    Debug.Log("RUN enemy "+enemy.name+" "+enemy.transform.position+" HP="+enemy.GetComponent<Health>().MaxHealth);
    var health=enemy.GetComponent<Health>();
    health.OnDeath+=()=>{enemiesDefeated++; Debug.Log("RUN defeated "+enemy.name+" at "+enemy.transform.position);};
   }
 }

 private IEnumerator PlayIntro() {
  var npcs=FindObjectsByType<TalkableEntity>().OrderBy(t=>t.transform.position.x).ToArray();
  var dm=FindAnyObjectByType<DialogueManager>();
  dm.OnDialogueLine+=(speaker,line)=>Debug.Log("RUN EN "+(speaker==null?"":speaker.DisplayName)+": "+DialogueTranslations.Translate(line));
  foreach(var npc in npcs) {
   if(failed) yield break;
   var id=(string)Field(npc,"_conversationId");
   var until=Time.realtimeSinceStartup+12;
   while (!DialogueProgress.IsCompleted(id) && Time.realtimeSinceStartup<until) {
    if(!dm.IsRunning && p.HasControl && p.transform.position.x<npc.transform.position.x-2.4f) {Set(right);yield return null;continue;}
    Set(talk);
    yield return new WaitForSecondsRealtime(.055f);
    Set();
    yield return new WaitForSecondsRealtime(.10f);
   }
   if(!DialogueProgress.IsCompleted(id)) {Die("conversation stalled "+npc.name+" at "+p.transform.position);yield break;}
   Debug.Log("RUN spoke to "+npc.name);
  }
  float deadline=Time.realtimeSinceStartup+12;
  while(SceneManager.GetActiveScene().buildIndex==1 && Time.realtimeSinceStartup<deadline) yield return null;
  if(SceneManager.GetActiveScene().buildIndex!=2) Die("intro cutscene never loaded the course");
  else Debug.Log("RUN intro finished naturally");
 }

 private IEnumerator PlayStage() {
  var token=loads;
  int stage=Course.Stage;
  Debug.Log("RUN stage "+(stage+1)+" attempt, enemies active="+FindObjectsByType<Enemy>().Length);
  var env=GameObject.Find("Enviro").transform;
  var shelf=env.Find("Ground (3)").GetComponentInChildren<BoxCollider2D>();
  yield return ClearFloor(token,false,shelf.bounds.max.y);
  if(!Progression.RunActive || failed) yield break;
  var steps = new List<Collider2D>();
  if(stage==0) {
    foreach(int index in new[]{6,7,8,9}) steps.Add(env.Find("bgV2_2 ("+index+")").GetComponent<Collider2D>());
  } else {
    steps.AddRange(GameObject.Find("Generated clay steps").GetComponentsInChildren<Collider2D>().OrderBy(c=>c.bounds.center.x));
  }
  float fromY=-3.36f;
  if(!Course.HasCheckpoint) {
    var first=steps[0].bounds;
    yield return Traverse(first.min.x-2.15f,token,20f,false);
    if(loads!=token || failed || !Progression.RunActive) yield break;
    var route=steps.Select(step=>step.bounds).Concat(new[]{shelf.bounds}).ToArray();
    int misses=0;
    for(int index=0;index<route.Length;) {
      yield return JumpTo(route[index],fromY,token,index==route.Length-1);
      if(loads!=token || failed || !Progression.RunActive) yield break;
      if(jumpLanded) {
        yield return ClearNearby(token);
        if(!Progression.RunActive || failed) yield break;
        fromY=route[index].max.y;
        index++;
        continue;
      }
      if(++misses>10) {Die("too many missed jumps at stage "+(stage+1)+" step "+(index+1));yield break;}
      yield return new WaitForSecondsRealtime(.35f);
      if(loads!=token) yield break;
      float feet=p.transform.position.y-1.05f;
      int below=Array.FindLastIndex(route, b => Mathf.Abs(b.max.y-feet)<.26f && b.min.x-.25f<=p.transform.position.x && b.max.x+.25f>=p.transform.position.x);
      index=below+1;
      fromY=below<0?-3.36f:route[below].max.y;
    }
  } else Debug.Log("RUN respawned at the upper checkpoint "+p.transform.position);
  yield return ClearFloor(token,true,shelf.bounds.max.y);
  if(!Progression.RunActive || failed) yield break;
  var exit=FindAnyObjectByType<WinTrigger>();
  yield return Traverse(exit.transform.position.x+1.0f,token,40f,true);
  Set();
  if(Progression.RunActive && !Course.Finished && loads==token && Course.Stage==stage) Die("exit not reached, stage="+(stage+1)+" pos="+p.transform.position+" hp="+hp.CurrentHealth+" remaining="+Progression.EnemiesRemaining);
  else Debug.Log("RUN stage "+(stage+1)+" exit reached");
 }

 private IEnumerator ClearFloor(int token,bool upper,float shelfTop) {
  float deadline=Time.realtimeSinceStartup+100;
  float nextReport=0;
  nextAttack=attackEnd=0;
  while(loads==token && Progression.RunActive && Time.realtimeSinceStartup<deadline) {
   var target=FindObjectsByType<Enemy>().Where(e=>e.GetComponent<Health>().CurrentHealth>0 &&
     (upper ? e.GetComponent<Collider2D>().bounds.min.y>shelfTop-2 : e.GetComponent<Collider2D>().bounds.min.y<0))
     .OrderBy(e=>Mathf.Abs(e.transform.position.x-p.transform.position.x)).FirstOrDefault();
   if(target==null){Set();yield break;}
   float dx=target.transform.position.x-p.transform.position.x;
   if(Time.realtimeSinceStartup>=nextReport) {
    Debug.Log("RUN combat "+p.transform.position+" target="+target.name+" "+target.transform.position+" HP="+hp.CurrentHealth+" control="+p.HasControl);
    nextReport=Time.realtimeSinceStartup+5;
   }
   var dir=dx>=0?right:left;
   if(Mathf.Abs(dx)>1.65f) Set(dir);
   else if(Time.time>=nextAttack){Set(dir,attack);nextAttack=Time.time+.24f;attackEnd=Time.time+.065f;}
   else if(Time.time>=attackEnd) Set();
   else Set(dir,attack);
   yield return null;
  }
  Set();
  if(Progression.RunActive && loads==token) Die("combat stalled on "+(upper?"upper":"lower")+" floor, remaining="+Progression.EnemiesRemaining);
 }

 private IEnumerator ClearNearby(int token) {
  float deadline=Time.realtimeSinceStartup+35;
  nextAttack=attackEnd=0;
  while(loads==token && Progression.RunActive && Time.realtimeSinceStartup<deadline) {
   float feet=p.transform.position.y-1.05f;
   var target=FindObjectsByType<Enemy>().Where(e=>e.GetComponent<Health>().CurrentHealth>0 &&
     Mathf.Abs(e.GetComponent<Collider2D>().bounds.min.y-feet)<1.5f && Mathf.Abs(e.transform.position.x-p.transform.position.x)<8)
     .OrderBy(e=>Mathf.Abs(e.transform.position.x-p.transform.position.x)).FirstOrDefault();
   if(target==null){Set();yield break;}
   float dx=target.transform.position.x-p.transform.position.x;
   var dir=dx>=0?right:left;
   if(Mathf.Abs(dx)>1.65f) Set(dir);
   else if(Time.time>=nextAttack){Set(dir,attack);nextAttack=Time.time+.24f;attackEnd=Time.time+.065f;}
   else if(Time.time>=attackEnd)Set();
   else Set(dir,attack);
   yield return null;
  }
  Set();
 }

 private IEnumerator Traverse(float goal,int token,float seconds,bool shelf) {
  float until=Time.realtimeSinceStartup+seconds;
  nextAttack=attackEnd=nextDodge=0;
  while(loads==token && Progression.RunActive && !Course.Finished && Time.realtimeSinceStartup<until && p.transform.position.x<goal) {
   if(GameMenu.GameplayPaused) { Die("game paused unexpectedly during combat at " + p.transform.position); yield break; }
   if (!p.HasControl && Time.frameCount % 180 == 0) Debug.Log("RUN no control at "+p.transform.position);
   Vector3 here=p.transform.position;
   var nearest=FindObjectsByType<Enemy>().Where(e=>Mathf.Abs(e.transform.position.y-here.y)<2.2f && Mathf.Abs(e.transform.position.x-here.x)<3.5f)
       .OrderBy(e=>Mathf.Abs(e.transform.position.x-here.x)).FirstOrDefault();
   if(shelf && (hp.CurrentHealth <= 4 || nearest == null || nearest.transform.position.x < here.x-.8f)) {
    if(nearest!=null && nearest.transform.position.x >= here.x && Mathf.Abs(nearest.transform.position.x-here.x)<2.4f && Time.time>=nextDodge && p.IsGrounded) {
     Set(right,jump); nextDodge=Time.time+1.18f;
    } else if(nextDodge-Time.time>.44f) Set(right,jump);
    else Set(right);
   } else if(nearest!=null) {
    float dx=nearest.transform.position.x-here.x;
    int desired=dx>=0?1:-1;
    var dir=desired>0?right:left;
    if(Mathf.Abs(dx)>1.75f) Set(dir);
    else if(Time.time>=nextAttack) {
      Set(dir,attack);
      nextAttack=Time.time+.25f;
      attackEnd=Time.time+.08f;
    } else if(Time.time>=attackEnd) Set();
    else Set(dir,attack);
   } else Set(right);
   yield return null;
  }
  Set();
 }

 private IEnumerator JumpTo(Bounds target,float prior,int token,bool shelf) {
   jumpLanded=false;
   int dir=1;
   var move=dir>0?right:left;
   float edge=dir>0?target.min.x:target.max.x;
   float landing=target.min.x+Mathf.Min(1.5f,target.size.x*.45f);
   float until=Time.realtimeSinceStartup+4f;
   float takeoff=edge-dir*2.15f;
   while(loads==token && Progression.RunActive && Time.realtimeSinceStartup<until) {
     float dx=takeoff-p.transform.position.x;
     if(Mathf.Abs(dx)>.30f) Set(dx>0?right:left);
     else {
       Set();
       if(p.IsGrounded && p.HasControl) break;
     }
     yield return null;
   }
   if(loads!=token || !Progression.RunActive) yield break;
   Set();
   yield return new WaitForSecondsRealtime(.10f);
   if(loads!=token) yield break;
   float x0=p.transform.position.x;
   float peak=p.transform.position.y;
   bool groundedAtTakeoff=p.IsGrounded;
   float begin=Time.time;
   until=Time.realtimeSinceStartup+2.2f;
   do {
    if(loads!=token || !Progression.RunActive) yield break;
    var pos=p.transform.position;
    peak=Mathf.Max(peak,pos.y);
    if(landing-pos.x>.9f) Set(right,jump);
    else if(landing-pos.x<-.6f) Set(left,jump);
    else Set(jump);
    yield return null;
   } while(loads==token && !(Time.time>begin+.18f && p.IsGrounded && p.GetComponent<Rigidbody2D>().linearVelocity.y<=0.05f &&
      p.transform.position.y>=target.max.y+.75f && p.transform.position.x>=target.min.x-.2f)
      && Time.realtimeSinceStartup<until);
   Set();
   if(loads!=token) yield break;
   var end=p.transform.position;
   if(end.y < target.max.y+.75f || end.x < target.min.x-.25f || end.x > target.max.x+.25f) {
     Debug.Log("RUN jump missed, recovering: stage="+(Course.Stage+1)+" takeoff="+x0+" grounded="+groundedAtTakeoff+" previous floor="+prior+" target="+target+" max y="+peak+" ended="+end+" HP="+hp.CurrentHealth);
   } else {
     jumpLanded=true;
     Debug.Log("RUN jump landed "+end+" from "+x0.ToString("F1"));
   }
 }
}
}
