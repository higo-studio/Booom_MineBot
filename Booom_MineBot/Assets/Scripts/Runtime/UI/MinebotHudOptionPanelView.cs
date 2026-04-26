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
        private Image titleIcon;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private Button[] optionButtons = Array.Empty<Button>();

        private Color buttonColor = new Color(0.18f, 0.22f, 0.2f, 0.96f);
        private Color selectedButtonColor = new Color(0.28f, 0.44f, 0.4f, 1f);

        public TMP_Text TitleText => titleText;
        public Button[] OptionButtons => optionButtons;

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, int buttonCount, MinebotHudDefaults.OptionPanelLayout layout)
        {
            MinebotHudUiFactory.StretchToParent((RectTransform)transform);

            backgroundImage = MinebotHudUiFactory.GetOrAdd<Image>(gameObject);
            backgroundImage.color = layout.BackgroundColor;
            buttonColor = layout.ButtonColor;
            selectedButtonColor = layout.SelectedButtonColor;

            titleIcon = MinebotHudUiFactory.EnsureTopLeftImage(titleIcon, transform, "Title Icon", layout.SidePadding, layout.TitleTop + 2f, 24f);

            titleText = MinebotHudUiFactory.EnsureTopStretchText(
                titleText,
                transform,
                "Title",
                layout.SidePadding + 32f,
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
            SetButton(index, visible, label, false);
        }

        public void SetButton(int index, bool visible, string label, bool selected)
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

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = selected ? selectedButtonColor : buttonColor;
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

        public void ApplyGraphics(Sprite background, Sprite icon)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = background;
                backgroundImage.type = background != null ? Image.Type.Sliced : Image.Type.Simple;
                backgroundImage.color = background != null ? Color.white : backgroundImage.color;
            }

            if (titleIcon != null)
            {
                titleIcon.sprite = icon;
                titleIcon.enabled = icon != null;
            }
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
                if (layout.ButtonFlow == MinebotHudDefaults.OptionPanelFlow.Horizontal)
                {
                    optionButtons[i] = MinebotHudUiFactory.EnsureTopLeftButton(
                        optionButtons[i],
                        transform,
                        $"Option Button {i + 1}",
                        layout.SidePadding + i * layout.ButtonSpacing,
                        layout.ButtonTop,
                        layout.ButtonWidth,
                        layout.ButtonHeight,
                        layout.ButtonFontSize,
                        layout.ButtonColor,
                        layout.ButtonTextAlignment,
                        runtimeFontAsset);
                }
                else
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
