using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using RageRefinement;

// Optional integration runner. Compile into a disposable player copy, never the installed game.
namespace RageBench
{
    public sealed class Runner : MonoBehaviour
    {
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private int failures;
        public static void StartBenchmark()
        {
            var host = new GameObject("Visual smoke check");
            DontDestroyOnLoad(host);
            host.AddComponent<Runner>();
        }
        private static T Field<T>(object target, string name)
        {
            return (T)target.GetType().GetField(name, Private).GetValue(target);
        }
        private static void Call(object target, string name)
        {
            target.GetType().GetMethod(name, Private).Invoke(target, null);
        }
        private void Check(VisualElement card, string context)
        {
            var viewport = card.panel.visualTree.worldBound;
            foreach (var text in card.Query<TextElement>().ToList())
            {
                var bounds = text.worldBound;
                var inner = text.contentRect;
                var measured = text.MeasureTextSize(text.text, inner.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                if (inner.width <= 0 || inner.height <= 0 || measured.y > inner.height + 2 ||
                    bounds.yMin < viewport.yMin - 2 || bounds.yMax > viewport.yMax + 2 ||
                    bounds.xMin < viewport.xMin - 2 || bounds.xMax > viewport.xMax + 2)
                {
                    failures++;
                    Debug.LogError("SMOKE layout " + context + " / " + text.text);
                }
            }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(Application.dataPath), name + ".png"));
            yield return new WaitForSecondsRealtime(.15f);
        }
        private IEnumerator Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            // Let the player's startup splash finish before capturing the title UI.
            yield return new WaitForSecondsRealtime(7f);
            var menu = FindAnyObjectByType<GameMenu>();
            // Capture menus with the game's scene camera so player screenshots include UI rendering.
            SceneManager.LoadScene(1);
            yield return new WaitForSecondsRealtime(.5f);
            var languageField = typeof(Locale).GetField("language", BindingFlags.Static | BindingFlags.NonPublic);
            var events = typeof(Locale).GetField("Changed", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (int width in new[] {1280, 800, 1024})
            {
                Screen.SetResolution(width, width == 1280 ? 720 : width == 800 ? 600 : 480, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(.3f);
                for (int language = 0; language < Locale.Names.Length; language++)
                {
                    languageField.SetValue(null, language);
                    var changed = (Action)events.GetValue(null);
                    if (changed != null) changed();
                    Call(menu, "OpenMenu");
                    foreach (string screen in new[] {"ShowActions", "ShowControls", "ShowLanguages"})
                    {
                        Call(menu, screen);
                        yield return null;
                        yield return null;
                        Check(Field<VisualElement>(menu, "card"), width + " " + language + " " + screen);
                        if (width == 1280 && language == 1)
                            yield return Capture(screen == "ShowActions" ? "options" : screen == "ShowControls" ? "controls" : "languages");
                    }
                    Call(menu, "CloseMenu");
                    if (width == 1280 && language == 1) yield return Capture("title");
                }
            }
            languageField.SetValue(null, 1);
            var refresh = (Action)events.GetValue(null);
            if (refresh != null) refresh();
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            SceneManager.LoadScene(1);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("village");
            Call(menu, "OpenMenu");
            yield return null;
            if (!GameMenu.GameplayPaused || Time.timeScale != 0) failures++;
            Check(Field<VisualElement>(menu, "card"), "pause");
            yield return Capture("pause");
            Call(menu, "ShowControls");
            yield return null;
            yield return null;
            yield return Capture("controls");
            Call(menu, "ShowLanguages");
            yield return null;
            yield return null;
            yield return Capture("languages");
            Call(menu, "CloseMenu");
            if (GameMenu.GameplayPaused || Time.timeScale == 0) failures++;
            SceneManager.LoadScene(2);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture("course");
            yield return ProjectileChecks.Run();
            failures += ProjectileChecks.Failures;
            Debug.Log("SMOKE complete; failures=" + failures);
            Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
