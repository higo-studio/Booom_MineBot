using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotHudOptionPanelView : MonoBehaviour
    {
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private Button[] optionButtons = Array.Empty<Button>();

        public TMP_Text TitleText => titleText;
        public Button[] OptionButtons => optionButtons;

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, int buttonCount, MinebotHudDefaults.OptionPanelLayout layout)
        {
            MinebotHudUiFactory.StretchToParent((RectTransform)transform);

            backgroundImage = MinebotHudUiFactory.GetOrAdd<Image>(gameObject);
            backgroundImage.color = layout.BackgroundColor;

            titleText = MinebotHudUiFactory.EnsureTopStretchText(
                titleText,
                transform,
                "Title",
                layout.SidePadding,
                layout.TitleTop,
                layout.SidePadding,
                layout.TitleHeight,
                layout.TitleFontSize,
                TextAnchor.UpperLeft,
                runtimeFontAsset);

            EnsureButtonArray(buttonCount, runtimeFontAsset, layout);
        }

        public void BindButtons(Action<int> onClick)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button button = optionButtons[i];
                if (button == null)
                {
                    continue;
                }

                int capturedIndex = i;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(capturedIndex));
            }
        }

        public void BindButton(int index, Action onClick)
        {
            Button button = GetButton(index);
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        public void SetTitle(string text)
        {
            if (titleText != null)
            {
                titleText.text = text ?? string.Empty;
            }
        }

        public void SetButton(int index, bool visible, string label)
        {
            Button button = GetButton(index);
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.interactable = visible;
            TMP_Text labelText = button.GetComponentInChildren<TMP_Text>();
            if (labelText != null)
            {
                labelText.text = label ?? string.Empty;
            }
        }

        public Button GetButton(int index)
        {
            return index >= 0 && index < optionButtons.Length ? optionButtons[index] : null;
        }

        public bool IsButtonVisible(int index)
        {
            Button button = GetButton(index);
            return button != null && button.gameObject.activeInHierarchy;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void EnsureButtonArray(int buttonCount, TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.OptionPanelLayout layout)
        {
            int safeCount = Mathf.Max(0, buttonCount);
            if (optionButtons == null || optionButtons.Length != safeCount)
            {
                Array.Resize(ref optionButtons, safeCount);
            }

            for (int i = 0; i < safeCount; i++)
            {
                optionButtons[i] = MinebotHudUiFactory.EnsureTopStretchButton(
                    optionButtons[i],
                    transform,
                    $"Option Button {i + 1}",
                    layout.SidePadding,
                    layout.ButtonTop + i * layout.ButtonSpacing,
                    layout.SidePadding,
                    layout.ButtonHeight,
                    layout.ButtonFontSize,
                    layout.ButtonColor,
                    runtimeFontAsset);
            }

            Button[] existingButtons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < existingButtons.Length; i++)
            {
                if (Array.IndexOf(optionButtons, existingButtons[i]) < 0)
                {
                    existingButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
