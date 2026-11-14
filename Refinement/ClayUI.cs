using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RageRefinement
{
    internal static class ClayUI
    {
        private static Sprite[] slabs;
        private static bool attempted;

        private static bool Load()
        {
            if (attempted) return slabs != null;
            attempted = true;
            try
            {
                var path = Path.Combine(Application.dataPath, "Refinement", "Assets", "clay-ui.png");
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = "Handmade clay interface";
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
                {
                    UnityEngine.Object.Destroy(texture);
                    throw new InvalidDataException("The clay UI atlas could not be decoded.");
                }
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                slabs = new Sprite[4];
                float width = texture.width / 2f, height = texture.height / 2f;
                for (int index = 0; index < slabs.Length; index++)
                {
                    // Image rows run top-to-bottom; Unity texture rectangles start at the bottom.
                    var rect = new Rect((index % 2) * width, (1 - index / 2) * height, width, height);
                    slabs[index] = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100, 0,
                        SpriteMeshType.FullRect, new Vector4(96, 96, 96, 96));
                    slabs[index].name = "Clay slab " + index;
                }
                return true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Clay UI artwork unavailable; using the readable fallback. " + error.Message);
                return false;
            }
        }

        private static void Surface(VisualElement element, int slab, float edgeScale)
        {
            element.style.backgroundColor = Color.clear;
            element.style.borderTopWidth = element.style.borderBottomWidth = 0;
            element.style.borderLeftWidth = element.style.borderRightWidth = 0;
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = 0;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 0;
            element.style.backgroundImage = new StyleBackground(slabs[slab]);
            element.style.unitySliceLeft = element.style.unitySliceRight = 96;
            element.style.unitySliceTop = element.style.unitySliceBottom = 96;
            element.style.unitySliceScale = edgeScale;
            element.style.unityBackgroundImageTintColor = Color.white;
        }

        internal static void Panel(VisualElement element)
        {
            if (Load()) Surface(element, 0, .30f);
        }

        internal static void Badge(VisualElement element)
        {
            element.style.color = GameMenu.Ink;
            if (!Load()) return;
            Surface(element, 0, .10f);
            if (element is Button) Interactions(element, 0, .10f);
        }

        internal static void Button(VisualElement element, bool primary)
        {
            if (!Load())
            {
                var normal = element.style.backgroundColor;
                element.RegisterCallback<FocusInEvent>(_ => element.style.backgroundColor = (Color)new Color32(181, 207, 127, 255));
                element.RegisterCallback<FocusOutEvent>(_ => element.style.backgroundColor = normal);
                return;
            }
            Surface(element, primary ? 2 : 1, .13f);
            Interactions(element, primary ? 2 : 1, .13f);
        }

        private static void Interactions(VisualElement element, int normal, float edgeScale)
        {
            bool hover = false, focus = false, pressed = false;
            Action refresh = () =>
            {
                Surface(element, hover || focus ? 3 : normal, edgeScale);
                element.style.unityBackgroundImageTintColor = pressed
                    ? new Color(.88f, .84f, .78f) : Color.white;
                element.style.scale = new Scale(pressed ? new Vector3(.985f, .96f, 1) : Vector3.one);
            };
            element.RegisterCallback<PointerEnterEvent>(_ => { hover = true; refresh(); });
            element.RegisterCallback<PointerLeaveEvent>(_ => { hover = false; pressed = false; refresh(); });
            element.RegisterCallback<FocusInEvent>(_ => { focus = true; refresh(); });
            element.RegisterCallback<FocusOutEvent>(_ => { focus = false; pressed = false; refresh(); });
            element.RegisterCallback<PointerDownEvent>(_ => { pressed = true; refresh(); });
            element.RegisterCallback<PointerUpEvent>(_ => { pressed = false; refresh(); });
            element.RegisterCallback<PointerCaptureOutEvent>(_ => { pressed = false; refresh(); });
        }
    }
}
