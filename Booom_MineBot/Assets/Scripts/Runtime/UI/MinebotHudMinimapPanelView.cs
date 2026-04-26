using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotHudMinimapPanelView : MonoBehaviour
    {
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private RawImage minimapImage;

        [SerializeField]
        private TMP_Text summaryText;

        public Texture MapTexture => minimapImage != null ? minimapImage.texture : null;
        public string Summary => summaryText != null ? summaryText.text : string.Empty;

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.MinimapPanelLayout layout)
        {
            MinebotHudUiFactory.StretchToParent((RectTransform)transform);
            backgroundImage = MinebotHudUiFactory.EnsureStretchImage(backgroundImage, transform, "Background", new Color(0.05f, 0.08f, 0.09f, 0.86f));
            minimapImage = MinebotHudUiFactory.EnsureTopLeftRawImage(
                minimapImage,
                transform,
                "Minimap Image",
                layout.SidePadding,
                layout.TopPadding,
                layout.MapSize,
                layout.MapSize);
            summaryText = MinebotHudUiFactory.EnsureBottomStretchText(
                summaryText,
                transform,
                "Summary Text",
                layout.SidePadding,
                10f,
                layout.SidePadding,
                layout.SummaryHeight,
                layout.SummaryFontSize,
                TextAnchor.MiddleLeft,
                runtimeFontAsset);
        }

        public void SetTexture(Texture texture)
        {
            if (minimapImage != null)
            {
                minimapImage.texture = texture;
                minimapImage.enabled = texture != null;
            }
        }

        public void SetSummary(string text)
        {
            if (summaryText != null)
            {
                summaryText.text = text ?? string.Empty;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ApplyGraphics(Sprite background)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.type = background != null ? Image.Type.Sliced : Image.Type.Simple;
                backgroundImage.color = background != null ? Color.white : new Color(0.05f, 0.08f, 0.09f, 0.86f);
            }
        }
    }
}
