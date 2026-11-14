using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RageRefinement
{
    public static class KeyboardBindings
    {
        public static readonly string[] Names = { "Move left", "Move right", "Jump", "Attack", "Talk / continue" };
        private static readonly string[] Actions = { "Move", "Move", "Jump", "Attack1", "Interact" };
        private static readonly string[] Defaults = { "a", "d", "space", "enter", "e" };
        private static readonly List<InputActionAsset> Assets = new List<InputActionAsset>();

        public static void Register(InputActionAsset asset)
        {
            AddArrowKeys(asset.FindActionMap("Player", true).FindAction("Move", true));
            AddArrowKeys(asset.FindActionMap("UI", true).FindAction("Navigate", true));
            Assets.Add(asset);
            Apply(asset);
        }

        public static void Unregister(InputActionAsset asset)
        {
            Assets.Remove(asset);
        }

        public static string KeyFor(int index)
        {
            string key = PlayerPrefs.GetString("RageRefinement.Key." + Actions[index] + "." + Defaults[index], Defaults[index]);
            return IsSupported(key) ? key : Defaults[index];
        }

        public static string DisplayKey(int index)
        {
            string key = KeyFor(index);
            return key == "space" ? Locale.UI("Space") : key.Length == 1 ? key.ToUpperInvariant() : "Enter";
        }

        public static bool IsSupported(string key)
        {
            return !string.IsNullOrEmpty(key) && ((key.Length == 1 && key[0] >= 'a' && key[0] <= 'z')
                || key == "space" || key == "enter");
        }

        public static bool Set(int index, string key)
        {
            if (!IsSupported(key)) return false;
            for (int other = 0; other < Names.Length; other++)
                if (other != index && string.Equals(KeyFor(other), key, StringComparison.OrdinalIgnoreCase))
                    return false;
            PlayerPrefs.SetString("RageRefinement.Key." + Actions[index] + "." + Defaults[index], key);
            Save();
            return true;
        }

        public static void Reset()
        {
            for (int index = 0; index < Names.Length; index++)
                PlayerPrefs.DeleteKey("RageRefinement.Key." + Actions[index] + "." + Defaults[index]);
            Save();
        }

        private static void Save()
        {
            foreach (var asset in Assets)
                if (asset != null) Apply(asset);
            PlayerPrefs.Save();
        }

        private static void AddArrowKeys(InputAction action)
        {
            // Each keyboard composite gets one binding per direction.
            for (int index = 0; index < action.bindings.Count; index++)
            {
                var binding = action.bindings[index];
                if (binding.isPartOfComposite && binding.path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)
                    && binding.path.EndsWith("Arrow", StringComparison.OrdinalIgnoreCase))
                    action.ApplyBindingOverride(index, string.Empty);
            }
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }

        private static void Apply(InputActionAsset asset)
        {
            var map = asset.FindActionMap("Player", true);
            for (int index = 0; index < Names.Length; index++)
            {
                var action = map.FindAction(Actions[index], true);
                string original = "<Keyboard>/" + Defaults[index];
                string replacement = "<Keyboard>/" + KeyFor(index);
                for (int binding = 0; binding < action.bindings.Count; binding++)
                {
                    var entry = action.bindings[binding];
                    if (!string.Equals(entry.path, original, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.Equals(entry.effectivePath, replacement, StringComparison.OrdinalIgnoreCase))
                        action.ApplyBindingOverride(binding, replacement);
                    break;
                }
            }
        }
    }
}
