using UnityEngine;
using UnityEngine.UIElements;

namespace RageRefinement
{
    public static class TitleCaptions
    {
        private static Label play;
        private static Label exit;

        static TitleCaptions()
        {
            Locale.Changed += Refresh;
        }

        public static void Attach(VisualElement root)
        {
            play = Caption(root.Q<VisualElement>("StartButton"));
            exit = Caption(root.Q<VisualElement>("ExitButton"));
            Refresh();
        }

        public static void Clear()
        {
            play = null;
            exit = null;
        }

        private static Label Caption(VisualElement button)
        {
            if (button == null) return null;
            var existing = button.Q<Label>("LocalizedCaption");
            if (existing != null) return existing;
            var label = new Label { name = "LocalizedCaption", pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.top = Length.Percent(96);
            label.style.alignSelf = Align.Center;
            GameMenu.StylePaperBadge(label);
            label.style.unityFontDefinition = GameMenu.HandwrittenFont;
            label.style.fontSize = 24;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.paddingLeft = 14;
            label.style.paddingRight = 14;
            label.style.paddingTop = 2;
            label.style.paddingBottom = 2;
            button.Add(label);
            return label;
        }

        private static void Refresh()
        {
            Set(play, "Play");
            Set(exit, "Exit");
        }

        private static void Set(Label label, string key)
        {
            if (label == null) return;
            label.text = Locale.UI(key);
            label.style.display = Locale.Language == 1 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
