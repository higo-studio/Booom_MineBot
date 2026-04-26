using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    internal static class MinebotHudUiFactory
    {
        public static void ConfigureCanvasRoot(GameObject target)
        {
            RectTransform rootRect = target.transform as RectTransform;
            if (rootRect != null)
            {
                StretchToParent(rootRect);
            }

            Canvas canvas = GetOrAdd<Canvas>(target);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = GetOrAdd<CanvasScaler>(target);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAdd<GraphicRaycaster>(target);
        }

        public static RectTransform EnsureSlot(ref RectTransform slot, Transform parent, string objectName, MinebotHudDefaults.SlotLayout layout)
        {
            Transform candidate = slot != null ? slot : parent.Find(objectName);
            if (candidate == null)
            {
                candidate = new GameObject(objectName, typeof(RectTransform)).transform;
                candidate.SetParent(parent, false);
            }

            RectTransform rect = candidate as RectTransform;
            rect.anchorMin = layout.AnchorMin;
            rect.anchorMax = layout.AnchorMax;
            rect.pivot = layout.Pivot;
            rect.anchoredPosition = layout.AnchoredPosition;
            rect.sizeDelta = layout.Size;
            rect.localScale = Vector3.one;
            slot = rect;
            return rect;
        }

        public static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public static TMP_Text EnsureFillText(TMP_Text current, Transform parent, string objectName, int fontSize, TextAnchor alignment, Vector4 padding, TMP_FontAsset runtimeFontAsset)
        {
            Transform child = current != null ? current.transform : parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).transform;
                child.SetParent(parent, false);
            }

            RemoveLegacyText(child);

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = child.gameObject.AddComponent<TextMeshProUGUI>();
            }
            RectTransform rect = (RectTransform)child;
            StretchToParent(rect);
            rect.offsetMin = new Vector2(padding.x, padding.w);
            rect.offsetMax = new Vector2(-padding.z, -padding.y);

            text.fontSize = fontSize;
            text.alignment = ToTmpAlignment(alignment);
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            if (runtimeFontAsset != null)
            {
                text.font = runtimeFontAsset;
            }

            return text;
        }

        public static TMP_Text EnsureTopStretchText(TMP_Text current, Transform parent, string objectName, float left, float top, float right, float height, int fontSize, TextAnchor alignment, TMP_FontAsset runtimeFontAsset)
        {
            Transform child = current != null ? current.transform : parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).transform;
                child.SetParent(parent, false);
            }

            RemoveLegacyText(child);

            TMP_Text text = child.GetComponent<TMP_Text>();
            if (text == null)
            {
                text = child.gameObject.AddComponent<TextMeshProUGUI>();
            }
            RectTransform rect = (RectTransform)child;
            ConfigureTopStretchRect(rect, left, top, right, height);

            text.fontSize = fontSize;
            text.alignment = ToTmpAlignment(alignment);
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            if (runtimeFontAsset != null)
            {
                text.font = runtimeFontAsset;
            }

            return text;
        }

        public static Button EnsureTopStretchButton(Button current, Transform parent, string objectName, float left, float top, float right, float height, int fontSize, Color backgroundColor, TMP_FontAsset runtimeFontAsset)
        {
            Transform child = current != null ? current.transform : parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).transform;
                child.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)child;
            ConfigureTopStretchRect(rect, left, top, right, height);

            Image image = GetOrAdd<Image>(child.gameObject);
            image.color = backgroundColor;

            Button button = GetOrAdd<Button>(child.gameObject);
            EnsureFillText(null, child, "Label", fontSize, TextAnchor.MiddleLeft, new Vector4(10f, 5f, 10f, 5f), runtimeFontAsset);
            return button;
        }

        public static void ConfigureTopStretchRect(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            rect.localScale = Vector3.one;
        }

        public static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void RemoveLegacyText(Transform target)
        {
            Text legacyText = target.GetComponent<Text>();
            if (legacyText == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(legacyText);
            }
            else
            {
                Object.DestroyImmediate(legacyText);
            }
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.TopLeft;
            }
        }
    }
}
