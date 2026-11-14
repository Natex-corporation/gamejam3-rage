using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RageRefinement
{
    [DefaultExecutionOrder(-1000)]
    public sealed class GameMenu : MonoBehaviour
    {
        private const string GraphicsPreference = "RageRefinement.Graphics";
        private const string WindowPreference = "RageRefinement.Windowed";
        private static readonly float[] RenderScales = { 0.65f, 0.80f, 1f };
        private static readonly string[] Presets = { "Fast", "Balanced", "Sharp" };
        private static readonly string[] IntroNeighbors = { "Aaron", "Henry", "Oldrich", "Pie", "Spinio", "Cha" };
        internal static readonly Color Paper = new Color32(252, 234, 190, 255);
        internal static readonly Color Ink = new Color32(79, 48, 29, 255);
        private static readonly Color Accent = new Color32(232, 166, 77, 255);
        private static readonly Color Muted = new Color32(111, 73, 43, 255);
        private static readonly Color Clay = new Color32(239, 108, 86, 255);

        private static GameMenu instance;
        private static int blockedInputFrame = -1;
        private bool isOpen;
        private bool inGame;
        private bool paused;
        private bool previousAudioPause;
        private float gameplayTimeScale;
        private int preset;
        private bool loading;
        private Action confirmedAction;
        private PanelSettings panelSettings;
        private VisualElement root;
        private VisualElement overlay;
        private VisualElement card;
        private Button shortcut;
        private Button languageShortcut;
        private Label stageLabel;
        private Label clayLabel;
        private Label bossLabel;
        private Label dashLabel;
        private bool showingWorkshop;
        private bool showingMap;
        private int workshopPage;
        private float nextWorldRefresh;
        private float lockedExitUntil;
        private int nextNeighbor;
        private bool showingControls;
        private bool showingLanguages;
        private int bindingIndex = -1;
        private int bindingFrame = -1;
        private Label bindingMessage;
        private static FontDefinition handwrittenFont;

        internal static FontDefinition HandwrittenFont
        {
            get
            {
                if (handwrittenFont.fontAsset != null || handwrittenFont.font != null) return handwrittenFont;

                foreach (var asset in Resources.FindObjectsOfTypeAll<UnityEngine.TextCore.Text.FontAsset>())
                {
                    if (asset.name != "StrawberryMuffins SDF") continue;
                    handwrittenFont = FontDefinition.FromSDFFont(asset);
                    return handwrittenFont;
                }

                foreach (var font in Resources.FindObjectsOfTypeAll<Font>())
                {
                    if (font.name != "StrawberryMuffins") continue;
                    handwrittenFont = FontDefinition.FromFont(font);
                    return handwrittenFont;
                }

                return FontDefinition.FromFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            }
        }

        public static bool GameplayPaused { get { return instance != null && instance.paused; } }
        public static bool BlockGameplayInput
        {
            get { return GameplayPaused || (blockedInputFrame >= 0 && Time.frameCount <= blockedInputFrame); }
        }

        public static void ShowFinish()
        {
            if (instance == null) return;
            instance.OpenMenu();
        }

        public static void ShowRunResult()
        {
            if (instance == null) return;
            if (!instance.isOpen) instance.OpenMenu();
            else instance.ShowActions();
        }

        public static void StartJourney()
        {
            if (instance == null) return;
            instance.StartRun(!Progression.StorySeen);
        }

        private void StartRun(bool story)
        {
            int district = Progression.Won ? 0 : Progression.UnlockedDistrict;
            StartDistrict(story ? 0 : district, story);
        }

        private void StartDistrict(int district, bool story)
        {
            Progression.BeginRun();
            Course.BeginRun(district);
            LoadScene(story ? 1 : Course.LevelScene);
        }

        public static void ShowLockedExit()
        {
            if (instance == null) return;
            instance.lockedExitUntil = Time.unscaledTime + 3f;
            instance.stageLabel.text = string.Format(Locale.UI("Defeat {0} more enemies to open the exit."), Progression.EnemiesRemaining);
        }

        public static void StyleHearts(VisualElement container)
        {
            if (container == null) return;
            container.style.width = 480;
            container.style.maxWidth = Length.Percent(40);
            container.style.flexWrap = Wrap.Wrap;
            foreach (var heart in container.Children())
            {
                heart.style.width = 48;
                heart.style.height = 52;
                heart.style.marginLeft = heart.style.marginRight = 2;
            }
        }

        public static void OnConversationCompleted(string id)
        {
            if (instance == null || SceneManager.GetActiveScene().buildIndex != 1) return;
            int next = instance.nextNeighbor;
            if (next < IntroNeighbors.Length && string.Equals(id, IntroNeighbors[next] + "_intro", StringComparison.OrdinalIgnoreCase))
            {
                instance.nextNeighbor++;
                instance.ShowIntroObjective();
            }
        }

        public static void StyleSpeakerName(Label label)
        {
            if (label == null) return;
            if (label.text == "ElSpinio") label.text = "El Spinio";
            label.style.color = (Color)new Color32(28, 13, 10, 255);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Install()
        {
            if (instance != null) return;

            var host = new GameObject("Game options");
            host.SetActive(false);
            DontDestroyOnLoad(host);
            instance = host.AddComponent<GameMenu>();
            host.SetActive(true);
        }

        public static float DialogueDeltaTime()
        {
            return GameplayPaused ? 0f : Time.unscaledDeltaTime;
        }

        public static void SetLocalScaleIfChanged(Transform transform, Vector3 scale)
        {
            if (!transform.localScale.Equals(scale)) transform.localScale = scale;
        }

        public static void SetGameplayTimeScale(float value)
        {
            if (GameplayPaused)
                instance.gameplayTimeScale = value;
            else
                Time.timeScale = value;
        }

        private void Awake()
        {
            preset = Mathf.Clamp(PlayerPrefs.GetInt(GraphicsPreference, 1), 0, Presets.Length - 1);
            if (PlayerPrefs.HasKey(WindowPreference))
                SetDisplay(PlayerPrefs.GetInt(WindowPreference) == 1);
            ApplyGraphics();

            var sourcePanel = FindAnyObjectByType<PanelRenderer>();
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            if (sourcePanel != null && sourcePanel.panelSettings != null)
                panelSettings.themeStyleSheet = sourcePanel.panelSettings.themeStyleSheet;
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1280, 720);
            panelSettings.screenMatchMode = PanelScreenMatchMode.Shrink;
            panelSettings.sortingOrder = 100;

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            root.style.unityFontDefinition = HandwrittenFont;

            shortcut = new Button(OpenMenu) { focusable = false };
            shortcut.style.position = Position.Absolute;
            shortcut.style.right = 24;
            shortcut.style.color = Paper;
            shortcut.style.unityFontDefinition = HandwrittenFont;
            shortcut.style.fontSize = 19;
            shortcut.style.borderTopWidth = 0;
            shortcut.style.borderBottomWidth = 0;
            shortcut.style.borderLeftWidth = 0;
            shortcut.style.borderRightWidth = 0;
            shortcut.style.bottom = 22;
            shortcut.style.paddingLeft = 14;
            shortcut.style.paddingRight = 14;
            shortcut.style.paddingTop = 8;
            shortcut.style.paddingBottom = 8;
            StylePaperBadge(shortcut);
            root.Add(shortcut);

            languageShortcut = new Button(() =>
            {
                OpenMenu();
                ShowLanguages();
            }) { focusable = false };
            languageShortcut.style.position = Position.Absolute;
            languageShortcut.style.left = 24;
            languageShortcut.style.bottom = 22;
            languageShortcut.style.backgroundColor = Ink;
            languageShortcut.style.color = Paper;
            languageShortcut.style.unityFontDefinition = HandwrittenFont;
            languageShortcut.style.fontSize = 19;
            languageShortcut.style.paddingLeft = 14;
            languageShortcut.style.paddingRight = 14;
            languageShortcut.style.paddingTop = 8;
            languageShortcut.style.paddingBottom = 8;
            StylePaperBadge(languageShortcut);
            root.Add(languageShortcut);

            stageLabel = Text("", 19, Paper);
            stageLabel.style.whiteSpace = WhiteSpace.NoWrap;
            stageLabel.style.position = Position.Absolute;
            stageLabel.style.right = 28;
            stageLabel.style.top = 24;
            stageLabel.style.maxWidth = Length.Percent(54);
            stageLabel.style.whiteSpace = WhiteSpace.Normal;
            stageLabel.style.backgroundColor = Ink;
            stageLabel.style.paddingLeft = 12;
            stageLabel.style.paddingRight = 12;
            stageLabel.style.paddingTop = 6;
            stageLabel.style.paddingBottom = 6;
            StylePaperBadge(stageLabel);
            stageLabel.pickingMode = PickingMode.Ignore;
            root.Add(stageLabel);

            clayLabel = Text("", 19, Ink);
            clayLabel.style.position = Position.Absolute;
            clayLabel.style.left = 24;
            clayLabel.style.bottom = 22;
            clayLabel.style.paddingLeft = clayLabel.style.paddingRight = 14;
            clayLabel.style.paddingTop = clayLabel.style.paddingBottom = 8;
            clayLabel.pickingMode = PickingMode.Ignore;
            StylePaperBadge(clayLabel);
            root.Add(clayLabel);

            bossLabel = Text("", 21, Ink);
            bossLabel.style.position = Position.Absolute;
            bossLabel.style.left = Length.Percent(34);
            bossLabel.style.right = Length.Percent(34);
            bossLabel.style.top = 84;
            bossLabel.style.paddingLeft = bossLabel.style.paddingRight = 14;
            bossLabel.style.paddingTop = bossLabel.style.paddingBottom = 8;
            bossLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            bossLabel.pickingMode = PickingMode.Ignore;
            StylePaperBadge(bossLabel);
            root.Add(bossLabel);
            dashLabel = Text("", 18, Ink);
            dashLabel.style.position = Position.Absolute;
            dashLabel.style.left = 24; dashLabel.style.bottom = 62;
            dashLabel.style.paddingLeft = dashLabel.style.paddingRight = 12;
            dashLabel.style.paddingTop = dashLabel.style.paddingBottom = 6;
            dashLabel.pickingMode = PickingMode.Ignore;
            StylePaperBadge(dashLabel);
            root.Add(dashLabel);

            overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;
            overlay.style.backgroundColor = (Color)new Color32(43, 30, 23, 160);
            root.Add(overlay);
            root.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                if (!isOpen || bindingIndex >= 0 || Time.frameCount == bindingFrame)
                    evt.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            card = new VisualElement();
            card.style.width = 460;
            card.style.maxWidth = Length.Percent(94);
            card.style.paddingTop = 26;
            card.style.paddingBottom = 28;
            card.style.paddingLeft = 32;
            card.style.paddingRight = 32;
            card.style.backgroundColor = Paper;
            SetBorder(card, 5, Accent);
            SetCorners(card, 24);
            ClayUI.Panel(card);
            overlay.Add(card);

            Locale.Changed += RefreshLanguage;
            Progression.Changed += RefreshProgress;
            SceneManager.activeSceneChanged += OnSceneChanged;
            OnSceneChanged(default(Scene), SceneManager.GetActiveScene());
            CloseMenu();
        }

        private void OnDestroy()
        {
            Locale.Changed -= RefreshLanguage;
            Progression.Changed -= RefreshProgress;
            SceneManager.activeSceneChanged -= OnSceneChanged;
            ReleasePause();
            if (panelSettings != null) Destroy(panelSettings);
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (loading) return;
            if (!paused && Course.UpdateProgress()) ShowStageObjective();
            if (!paused && Time.unscaledTime >= nextWorldRefresh)
            {
                nextWorldRefresh = Time.unscaledTime + .25f;
                Progression.UpdateWorld();
                RefreshProgress();
                if (SceneManager.GetActiveScene().buildIndex == Course.LevelScene) ShowStageObjective();
            }
            var keyboard = Keyboard.current;
            var pad = Gamepad.current;
            if (bindingIndex >= 0 && keyboard != null && Time.frameCount > bindingFrame)
            {
                if (pad != null && (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame))
                    ShowControls();
                else
                    CaptureKey(keyboard);
                return;
            }
            bool toggle = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (pad != null && pad.startButton.wasPressedThisFrame);
            if (toggle || (isOpen && pad != null && pad.buttonEast.wasPressedThisFrame))
            {
                if (confirmedAction != null || showingControls || showingLanguages || showingWorkshop || showingMap)
                    ShowActions();
                else if (isOpen && !Course.Finished && !Progression.RunEnded)
                    CloseMenu();
                else if (!isOpen)
                    OpenMenu();
            }
        }

        private void OnSceneChanged(Scene previous, Scene current)
        {
            lockedExitUntil = 0;
            Course.OnSceneChanged(current);
            Progression.SceneChanged(previous, current);
            if (current.buildIndex == Course.LevelScene) DemoContent.Install(current.GetRootGameObjects());
            inGame = current.buildIndex > 0;
            if (inGame) TitleCaptions.Clear();
            bool intro = current.buildIndex == 1;
            shortcut.style.top = intro ? (StyleLength)22f : StyleKeyword.Auto;
            shortcut.style.bottom = intro ? (StyleLength)StyleKeyword.Auto : 22f;
            shortcut.style.left = intro ? (StyleLength)24f : StyleKeyword.Auto;
            shortcut.style.right = intro ? (StyleLength)StyleKeyword.Auto : 24f;
            RefreshLanguage();
            stageLabel.style.display = inGame ? DisplayStyle.Flex : DisplayStyle.None;
            clayLabel.style.display = current.buildIndex == Course.LevelScene ? DisplayStyle.Flex : DisplayStyle.None;
            if (current.buildIndex == 1)
            {
                nextNeighbor = 0;
                ShowIntroObjective();
            }
            else
                ShowStageObjective();
            loading = false;
            CloseMenu();
            ApplyGraphics();
            RefreshProgress();
        }

        private void ShowStageObjective()
        {
            if (Time.unscaledTime < lockedExitUntil) return;
            string stage = string.Format(Locale.UI("Stage {0} / {1}"), Course.Stage + 1, Course.StageCount);
            stageLabel.text = Locale.UI(DemoContent.Districts[Course.Stage]) + " · " + stage + "\n" + string.Format(Locale.UI("Enemies left: {0}"), Progression.EnemiesRemaining);
            var player = FindAnyObjectByType<PlayerController>();
            var enemy = player == null ? null : Progression.NearestEnemy(player.transform.position);
            if (enemy != null)
            {
                var offset = enemy.transform.position - player.transform.position;
                string direction = Mathf.Abs(offset.y) > 3f ? offset.y > 0 ? "Above" : "Below" : offset.x > 0 ? "Right" : "Left";
                string name = enemy.name.Split('(')[0].Trim();
                if (name == "Spinio") name = "El Spinio";
                name = Locale.UI(name);
                stageLabel.text += "\n" + string.Format(Locale.UI("Nearest: {0} · {1}"), name, Locale.UI(direction));
            }
            else if (Progression.RunActive) stageLabel.text += "\n" + Locale.UI("Exit open · Head right");
        }

        private void ShowIntroObjective()
        {
            stageLabel.text = nextNeighbor < IntroNeighbors.Length
                ? string.Format(Locale.UI("Talk to {0}  ·  {1}"), (nextNeighbor == 4 ? "El Spinio" : IntroNeighbors[nextNeighbor]), KeyboardBindings.DisplayKey(4))
                : Locale.UI("Watch the cutscene");
        }

        private void OpenMenu()
        {
            if (isOpen || loading) return;
            isOpen = true;
            if (inGame)
            {
                gameplayTimeScale = Time.timeScale;
                previousAudioPause = AudioListener.pause;
                paused = true;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            shortcut.style.display = DisplayStyle.None;
            languageShortcut.style.display = DisplayStyle.None;
            overlay.style.display = DisplayStyle.Flex;
            ShowActions();
        }

        private void ReleasePause()
        {
            if (!paused) return;
            paused = false;
            Time.timeScale = gameplayTimeScale;
            AudioListener.pause = previousAudioPause;
            blockedInputFrame = Time.frameCount + 1;
        }

        private void CloseMenu()
        {
            isOpen = false;
            confirmedAction = null;
            showingControls = false;
            showingLanguages = false;
            showingWorkshop = false;
            bindingIndex = -1;
            showingMap = false;
            ReleasePause();
            DialogueTranslations.RefreshCaption();
            overlay.style.display = DisplayStyle.None;
            shortcut.style.display = DisplayStyle.Flex;
            languageShortcut.style.display = inGame ? DisplayStyle.None : DisplayStyle.Flex;
            if (SceneManager.GetActiveScene().buildIndex == 1) ShowIntroObjective();
        }

        private void LoadScene(int index)
        {
            if (index == 0) Progression.LeaveRun();
            loading = true;
            CloseMenu();
            Time.timeScale = 1f;
            SceneManager.LoadScene(index);
        }

        private void ShowActions()
        {
            confirmedAction = null;
            showingControls = false;
            showingLanguages = false;
            showingWorkshop = false;
            bindingIndex = -1;
            card.Clear();
            showingMap = false;
            card.style.width = 500;
            if (Progression.RunEnded)
            {
                AddHeading(Progression.Won ? "Course complete" : "Reshape and try again");
                AddNotice(string.Format(Locale.UI("This run: {0} clay · {1} enemies defeated"), Progression.RunClay, Progression.RunKills));
                AddNotice(string.Format(Locale.UI("Saved clay: {0}"), Progression.Clay));
                AddNotice("Your clay and buffs stay with you.");
                var finish = AddButton("Workshop", () => ShowWorkshop(-1), true);
                AddButton("Start next run", () => StartRun(false));
                AddButton("Return to title", () => LoadScene(0));
                AddButton("Choose district", ShowDistricts);
                AddButton("Quit game", () => Confirm("Quit the game?", "Quit game", () => Application.Quit()));
                finish.Focus();
                return;
            }
            AddHeading(inGame ? "Paused" : "Game options");
            var primary = AddButton(inGame ? "Resume game" : Progression.StorySeen ? "Start run" : "Start game",
                inGame ? (Action)CloseMenu : StartJourney, true);
            AddButton("Workshop", () => ShowWorkshop(-1));
            if (!inGame && Progression.StorySeen) AddButton("Choose district", ShowDistricts);
            if (!inGame)
                AddNotice("Fight, earn clay, buy buffs, and try again.", 18);
            AddButton("Controls", ShowControls);
            AddButton(Locale.UI("Language") + "   " + Locale.Names[Locale.Language], ShowLanguages);
            var graphics = AddButton(GraphicsLabel(), null);
            graphics.clicked += () =>
            {
                preset = (preset + 1) % Presets.Length;
                PlayerPrefs.SetInt(GraphicsPreference, preset);
                PlayerPrefs.Save();
                ApplyGraphics();
                graphics.text = GraphicsLabel();
            };
            var display = AddButton(DisplayLabel(Screen.fullScreen), null);
            display.clicked += () =>
            {
                bool windowed = Screen.fullScreen;
                SetDisplay(windowed);
                PlayerPrefs.SetInt(WindowPreference, windowed ? 1 : 0);
                PlayerPrefs.Save();
                display.text = DisplayLabel(!windowed);
            };
            if (SceneManager.GetActiveScene().buildIndex == Course.LevelScene)
                AddButton("Restart run", () => Confirm("Restart this run?", "Restart run", () => StartRun(false)));
            if (inGame)
                AddButton("Return to main menu", () => Confirm("Return to main menu?", "Return to main menu", () => LoadScene(0)));
            AddButton("Quit game", () => Confirm("Quit the game?", "Quit game", () => Application.Quit()));

            primary.Focus();
        }

        private void RefreshLanguage()
        {
            shortcut.text = Locale.UI(inGame ? "Pause" : "Options").ToUpperInvariant() + "  ·  ESC";
            languageShortcut.text = Locale.UI("Language") + "  ·  " + Locale.Names[Locale.Language];
            if (SceneManager.GetActiveScene().buildIndex == 1) ShowIntroObjective();
            else if (inGame) ShowStageObjective();
            RefreshProgress();
        }

        private void RefreshProgress()
        {
            if (clayLabel == null) return;
            clayLabel.text = string.Format(Locale.UI("Clay: {0} · Shells: {1}"), Progression.Clay, Progression.Shields);
            var boss = DemoContent.Boss;
            bossLabel.style.display = boss != null && boss.Health.CurrentHealth > 0 && !Progression.RunEnded ? DisplayStyle.Flex : DisplayStyle.None;
            if (boss != null)
                bossLabel.text = Locale.UI("The Kiln King") + "  " + boss.Health.CurrentHealth + " / " + boss.Health.MaxHealth
                    + "\n" + Locale.UI(string.IsNullOrEmpty(boss.Telegraph) ? boss.Phase == 2 ? "Phase 2 · Blazing clay" : "Phase 1 · Fired clay" : boss.Telegraph);
            dashLabel.style.display = Progression.RunActive && Progression.ActiveRank(4) > 0 && SceneManager.GetActiveScene().buildIndex == Course.LevelScene ? DisplayStyle.Flex : DisplayStyle.None;
            dashLabel.text = PlayerTricks.Cooldown <= 0 ? Locale.UI("Dash ready · Shift / B") : string.Format(Locale.UI("Dash ready in {0}s"),PlayerTricks.Cooldown.ToString("0.0"));
            if (SceneManager.GetActiveScene().buildIndex == Course.LevelScene)
            {
                stageLabel.style.display = Progression.RunEnded ? DisplayStyle.None : DisplayStyle.Flex;
                ShowStageObjective();
            }
        }

        private void ShowWorkshop(int purchased)
        {
            showingWorkshop = true;
            showingMap = false;
            showingControls = showingLanguages = false;
            confirmedAction = null;
            bindingIndex = -1;
            card.Clear();
            card.style.width = 540;
            AddHeading("Workshop");
            var back = AddButton("Back", ShowActions, true);
            AddNotice(string.Format(Locale.UI("Saved clay: {0}"), Progression.Clay), 23);
            AddButton(workshopPage == 0 ? "View tricks" : "View body buffs", () => { workshopPage = 1 - workshopPage; ShowWorkshop(-1); });
            AddNotice(purchased >= 0 ? "Purchased. Ready for your next run." : "Permanent buffs apply to your next run.", 18);
            for (int index = workshopPage * 4; index < workshopPage * 4 + 4; index++)
            {
                int slot = index;
                bool maxed = Progression.Rank(slot) >= Progression.Limits[slot];
                var label = Locale.UI(Progression.Names[slot]) + "   " + Progression.Rank(slot) + " / " + Progression.Limits[slot];
                var buy = AddButton(label, () => { if (Progression.TryBuy(slot)) ShowWorkshop(slot); });
                buy.SetEnabled(!maxed && Progression.Clay >= Progression.Cost(slot));
                string effect = slot == 0 ? "Hit damage: {0}" : slot == 1 ? "Hearts: {0}" : slot == 2 ? "Blocked hits per run: {0}" : "Heals 1 heart every {0} defeats";
                int value = slot == 0 ? 1 + Progression.Rank(slot) : slot == 1 ? 3 + 2 * Progression.Rank(slot) : slot == 3 ? 4 - Progression.Rank(slot) : Progression.Rank(slot);
                if (slot == 3 && Progression.Rank(slot) == 0) effect = "Healing not unlocked";
                if (slot == 4) { effect = "Shift / B dash · cooldown {0}s"; value = 4; }
                if (slot == 5) { effect = "Projectile bounces: {0}"; value = Progression.Rank(slot); }
                if (slot == 6) { effect = "Extra charged-shot pellets: {0}"; value = Progression.Rank(slot); }
                if (slot == 7) { effect = "Double damage every {0} hits"; value = 7 - Progression.Rank(slot); }
                string price = maxed ? Locale.UI("Maxed") : string.Format(Locale.UI("{0} clay"), Progression.Cost(slot));
                if (slot >= 4 && Progression.Rank(slot) == 0) effect = "Buy to unlock this trick.";
                object displayed = slot == 4 ? (object)(4 - Progression.Rank(slot) * .75f).ToString("0.00") : value;
                AddNotice(string.Format(Locale.UI(effect), displayed) + "   ·   " + price, 18);
            }
            if (Progression.RunEnded) AddButton("Start next run", () => StartRun(false), true);
            back.Focus();
        }

        private void ShowDistricts()
        {
            showingMap = true;
            showingWorkshop = showingControls = showingLanguages = false;
            confirmedAction = null; bindingIndex = -1;
            card.Clear(); card.style.width = 540;
            AddHeading("Choose district");
            var back = AddButton("Back", ShowActions, true);
            AddNotice("Cleared districts stay unlocked.",18);
            for (int index = 0; index < Course.StageCount; index++)
            {
                int district = index;
                var button = AddButton((index + 1) + ". " + Locale.UI(DemoContent.Districts[index]), () => StartDistrict(district,false));
                button.SetEnabled(index <= Progression.UnlockedDistrict);
            }
            back.Focus();
        }

        private void ShowTricks() { workshopPage = 1; ShowWorkshop(-1); }

        private void AddNotice(string text, int size = 19)
        {
            var notice = Text(Locale.UI(text), size, Muted);
            notice.style.marginTop = 5;
            notice.style.marginBottom = 3;
            card.Add(notice);
        }

        private void ShowLanguages()
        {
            showingLanguages = true;
            showingControls = false;
            card.Clear();
            card.style.width = 500;
            AddHeading("Language");
            var back = AddButton("Back", ShowActions, true);
            Button selected = null;
            for (int index = 0; index < Locale.Names.Length; index++)
            {
                int choice = index;
                bool active = choice == Locale.Language;
                var button = AddButton(Locale.Names[choice], () =>
                {
                    Locale.Select(choice);
                    ShowLanguages();
                }, active);
                if (active) selected = button;
            }
            (selected ?? back).Focus();
        }

        private void ShowControls()
        {
            showingControls = true;
            bindingIndex = -1;
            card.Clear();
            card.style.width = 500;
            AddHeading("Controls");
            var back = AddButton("Back", ShowActions, true);
            for (int index = 0; index < KeyboardBindings.Names.Length; index++)
            {
                int slot = index;
                AddButton(Locale.UI(KeyboardBindings.Names[slot]) + "    " + KeyboardBindings.DisplayKey(slot), () =>
                {
                    bindingIndex = slot;
                    bindingFrame = Time.frameCount;
                    bindingMessage.text = Locale.UI("Press a letter, Space or Enter. Esc cancels.");
                });
            }
            AddButton("Reset keys", () =>
            {
                KeyboardBindings.Reset();
                ShowControls();
            });
            bindingMessage = Text(Locale.UI("Tap Attack to kick. Hold and release to shoot.\nArrow keys move. Left click also attacks.\nController: stick to move, A jump, X attack, Y talk, Start pause."), 18, Muted);
            bindingMessage.style.marginTop = 12;
            card.Add(bindingMessage);
            AddNotice("Shift / B: dash after unlocking Clay dash.",18);
            back.Focus();
        }

        private void CaptureKey(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ShowControls();
                return;
            }

            foreach (var key in keyboard.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                string path = key.name;
                if (!KeyboardBindings.IsSupported(path)) continue;
                if (!KeyboardBindings.Set(bindingIndex, path))
                {
                    bindingMessage.text = Locale.UI("That key is in use. Press a different key or Esc.");
                    return;
                }
                bindingFrame = Time.frameCount;
                ShowControls();
                return;
            }
        }

        private void Confirm(string heading, string label, Action action, string warning = "Your clay and buffs are saved. This run will end.")
        {
            confirmedAction = action;
            card.Clear();
            card.style.width = 500;
            AddHeading(heading);
            if (inGame && !Course.Finished && !Progression.RunEnded)
            {
                var notice = Text(Locale.UI(warning), 19, Muted);
                notice.style.marginBottom = 10;
                card.Add(notice);
            }
            var back = AddButton("Go back", ShowActions, true);
            AddButton(label, () => { if (confirmedAction != null) confirmedAction(); });
            back.Focus();
        }

        private void AddHeading(string title)
        {
            var heading = Text(Locale.UI(title), 36, Ink);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 10;
            card.Add(heading);
        }

        private static Label Text(string text, int size, Color color)
        {
            var label = new Label(text);
            label.style.color = color;
            label.style.unityFontDefinition = HandwrittenFont;
            label.style.fontSize = size;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private Button AddButton(string text, Action clicked, bool primary = false)
        {
            var button = new Button(clicked);
            button.text = Locale.UI(text);
            button.style.height = 40;
            button.style.marginTop = 5;
            button.style.marginBottom = 0;
            button.style.marginLeft = 0;
            button.style.marginRight = 0;
            button.style.paddingLeft = 13;
            button.style.paddingTop = 5;
            button.style.paddingBottom = 5;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.unityFontDefinition = HandwrittenFont;
            button.style.fontSize = 22;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            Color normal = primary ? Clay : (Color)new Color32(246, 216, 163, 255);
            button.style.color = Ink;
            button.style.backgroundColor = normal;
            SetBorder(button, 2, Color.clear);
            SetCorners(button, 10);
            ClayUI.Button(button, primary);
            card.Add(button);
            return button;
        }

        internal static void StylePaperBadge(VisualElement element)
        {
            element.style.backgroundColor = Paper;
            element.style.color = Ink;
            SetBorder(element, 2, Accent);
            SetCorners(element, 10);
            ClayUI.Badge(element);
        }

        private static void SetBorder(VisualElement element, int width, Color color)
        {
            element.style.borderTopWidth = element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = element.style.borderRightWidth = width;
            element.style.borderTopColor = element.style.borderBottomColor = color;
            element.style.borderLeftColor = element.style.borderRightColor = color;
        }

        private static void SetCorners(VisualElement element, int radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
        }

        private string GraphicsLabel()
        {
            return Locale.UI("Graphics") + "   " + Locale.UI(Presets[preset]);
        }

        private static string DisplayLabel(bool fullScreen)
        {
            return Locale.UI("Display") + "   " + Locale.UI(fullScreen ? "Fullscreen" : "Windowed");
        }

        private void ApplyGraphics()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null) pipeline.renderScale = RenderScales[preset];
        }

        private static void SetDisplay(bool windowed)
        {
            if (windowed)
            {
                var desktop = Screen.currentResolution;
                Screen.SetResolution(Mathf.Min(1280, (int)(desktop.width * 0.9f)),
                    Mathf.Min(720, (int)(desktop.height * 0.85f)), FullScreenMode.Windowed);
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
        }
    }
}
