using TMPro;
using UnityEngine;

namespace Minebot.UI
{
    public sealed class MinebotHudTextPanelView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text contentText;

        public TMP_Text ContentText => contentText;
        public string Content => contentText != null ? contentText.text : string.Empty;

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.TextPanelLayout layout)
        {
            MinebotHudUiFactory.StretchToParent((RectTransform)transform);
            contentText = MinebotHudUiFactory.EnsureFillText(contentText, transform, "Content Text", layout.FontSize, layout.Alignment, layout.Padding, runtimeFontAsset);
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
    }
}
