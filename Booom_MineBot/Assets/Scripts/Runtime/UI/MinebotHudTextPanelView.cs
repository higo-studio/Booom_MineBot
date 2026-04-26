using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotHudTextPanelView : MonoBehaviour
    {
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TMP_Text contentText;

        public TMP_Text ContentText => contentText;
        public string Content => contentText != null ? contentText.text : string.Empty;

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.TextPanelLayout layout)
        {
            MinebotHudUiFactory.StretchToParent((RectTransform)transform);
            backgroundImage = MinebotHudUiFactory.EnsureStretchImage(backgroundImage, transform, "Background", new Color(0.05f, 0.08f, 0.09f, 0.86f));
            iconImage = MinebotHudUiFactory.EnsureTopLeftImage(iconImage, transform, "Panel Icon", 12f, 12f, 26f);
            contentText = MinebotHudUiFactory.EnsureFillText(
                contentText,
                transform,
                "Content Text",
                layout.FontSize,
                layout.Alignment,
                new Vector4(layout.Padding.x + 42f, layout.Padding.y + 4f, layout.Padding.z + 8f, layout.Padding.w + 8f),
                runtimeFontAsset);
        }

        public void SetText(string text)
        {
            if (contentText != null)
            {
                contentText.text = text ?? string.Empty;
            }
        }

        public void SetColor(Color color)
        {
            if (contentText != null)
            {
                contentText.color = color;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ApplyGraphics(Sprite background, Sprite icon)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.type = background != null ? Image.Type.Sliced : Image.Type.Simple;
                backgroundImage.color = background != null ? Color.white : new Color(0.05f, 0.08f, 0.09f, 0.86f);
            }

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
        }
    }
}
