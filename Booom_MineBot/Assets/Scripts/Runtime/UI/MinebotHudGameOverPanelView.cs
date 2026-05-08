using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotHudGameOverPanelView : MonoBehaviour
    {
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Image titleIcon;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text summaryText;

        [SerializeField]
        private TMP_Text promptText;

        [SerializeField]
        private TMP_InputField nameInputField;

        [SerializeField]
        private Button submitButton;

        [SerializeField]
        private TMP_Text statusText;

        [SerializeField]
        private TMP_Text leaderboardTitleText;

        [SerializeField]
        private TMP_Text leaderboardEntriesText;

        public TMP_InputField NameInputField => nameInputField;
        public Button SubmitButton => submitButton;
        public TMP_Text StatusText => statusText;
        public TMP_Text LeaderboardEntriesText => leaderboardEntriesText;

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.GameOverPanelLayout layout)
        {
            MinebotHudUiFactory.StretchToParent((RectTransform)transform);

            backgroundImage = MinebotHudUiFactory.EnsureStretchImage(backgroundImage, transform, "Background", layout.BackgroundColor);
            if (backgroundImage != null)
            {
                backgroundImage.color = layout.BackgroundColor;
            }

            titleIcon = MinebotHudUiFactory.EnsureTopLeftImage(titleIcon, transform, "Title Icon", 18f, 16f, 24f);

            titleText = MinebotHudUiFactory.EnsureTopStretchText(
                titleText,
                transform,
                "Title",
                52f,
                14f,
                18f,
                36f,
                layout.TitleFontSize,
                TextAnchor.MiddleLeft,
                runtimeFontAsset);
            titleText.text = "任务失败";

            summaryText = MinebotHudUiFactory.EnsureTopStretchText(
                summaryText,
                transform,
                "Summary",
                18f,
                58f,
                18f,
                62f,
                layout.SummaryFontSize,
                TextAnchor.UpperLeft,
                runtimeFontAsset);
            summaryText.text = string.Empty;

            promptText = MinebotHudUiFactory.EnsureTopStretchText(
                promptText,
                transform,
                "Prompt",
                18f,
                126f,
                18f,
                24f,
                layout.BodyFontSize,
                TextAnchor.MiddleLeft,
                runtimeFontAsset);
            promptText.text = "输入名字保存本地排行榜：";

            nameInputField = EnsureTopStretchInputField(
                nameInputField,
                transform,
                "Name Input",
                18f,
                160f,
                142f,
                42f,
                layout.BodyFontSize,
                layout.InputColor,
                layout.NameCharacterLimit,
                runtimeFontAsset);

            submitButton = MinebotHudUiFactory.EnsureTopLeftButton(
                submitButton,
                transform,
                "Submit Button",
                632f,
                160f,
                110f,
                42f,
                layout.BodyFontSize,
                new Color(0.16f, 0.23f, 0.24f, 0.96f),
                TextAnchor.MiddleCenter,
                runtimeFontAsset);
            SetButtonLabel(submitButton, "保存成绩");

            statusText = MinebotHudUiFactory.EnsureTopStretchText(
                statusText,
                transform,
                "Status",
                18f,
                210f,
                18f,
                26f,
                layout.BodyFontSize,
                TextAnchor.MiddleLeft,
                runtimeFontAsset);
            statusText.text = string.Empty;

            leaderboardTitleText = MinebotHudUiFactory.EnsureTopStretchText(
                leaderboardTitleText,
                transform,
                "Leaderboard Title",
                18f,
                246f,
                18f,
                28f,
                layout.BodyFontSize,
                TextAnchor.MiddleLeft,
                runtimeFontAsset);
            leaderboardTitleText.text = "本地前十";

            leaderboardEntriesText = MinebotHudUiFactory.EnsureBottomStretchText(
                leaderboardEntriesText,
                transform,
                "Leaderboard Entries",
                18f,
                18f,
                18f,
                110f,
                layout.LeaderboardFontSize,
                TextAnchor.UpperLeft,
                runtimeFontAsset);
            leaderboardEntriesText.text = "暂无成绩";
        }

        public void BindSubmit(Action<string> onSubmit)
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(() => onSubmit?.Invoke(GetNameInput()));
            }
        }

        public void BindNameChanged(Action<string> onChanged)
        {
            if (nameInputField == null)
            {
                return;
            }

            nameInputField.onValueChanged.RemoveAllListeners();
            nameInputField.onValueChanged.AddListener(value => onChanged?.Invoke(value));
        }

        public void SetSummary(string text)
        {
            if (summaryText != null)
            {
                summaryText.text = text ?? string.Empty;
            }
        }

        public void SetPrompt(string text)
        {
            if (promptText != null)
            {
                promptText.text = text ?? string.Empty;
            }
        }

        public void SetStatus(string text)
        {
            if (statusText != null)
            {
                statusText.text = text ?? string.Empty;
            }
        }

        public void SetLeaderboardSummary(string text)
        {
            if (leaderboardEntriesText != null)
            {
                leaderboardEntriesText.text = text ?? string.Empty;
            }
        }

        public void SetNameInput(string text)
        {
            if (nameInputField != null && nameInputField.text != (text ?? string.Empty))
            {
                nameInputField.SetTextWithoutNotify(text ?? string.Empty);
            }
        }

        public string GetNameInput()
        {
            return nameInputField != null ? nameInputField.text : string.Empty;
        }

        public void SetSubmissionEnabled(bool enabled)
        {
            if (nameInputField != null)
            {
                nameInputField.interactable = enabled;
            }

            if (submitButton != null)
            {
                submitButton.interactable = enabled;
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
                backgroundImage.color = background != null ? Color.white : backgroundImage.color;
            }

            if (titleIcon != null)
            {
                titleIcon.sprite = icon;
                titleIcon.enabled = icon != null;
            }
        }

        private static TMP_InputField EnsureTopStretchInputField(
            TMP_InputField current,
            Transform parent,
            string objectName,
            float left,
            float top,
            float right,
            float height,
            int fontSize,
            Color backgroundColor,
            int characterLimit,
            TMP_FontAsset runtimeFontAsset)
        {
            Transform child = current != null ? current.transform : parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField)).transform;
                child.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)child;
            MinebotHudUiFactory.ConfigureTopStretchRect(rect, left, top, right, height);

            Image image = MinebotHudUiFactory.GetOrAdd<Image>(child.gameObject);
            image.color = backgroundColor;

            TMP_InputField field = MinebotHudUiFactory.GetOrAdd<TMP_InputField>(child.gameObject);
            field.characterLimit = Mathf.Max(1, characterLimit);
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.richText = false;
            field.resetOnDeActivation = false;
            field.caretColor = Color.white;
            field.selectionColor = new Color(0.24f, 0.38f, 0.42f, 0.72f);

            RectTransform textArea = EnsureTextArea(child, "Text Area");
            TMP_Text text = MinebotHudUiFactory.EnsureFillText(null, textArea, "Text", fontSize, TextAnchor.MiddleLeft, Vector4.zero, runtimeFontAsset);
            TMP_Text placeholder = MinebotHudUiFactory.EnsureFillText(null, textArea, "Placeholder", fontSize, TextAnchor.MiddleLeft, Vector4.zero, runtimeFontAsset);
            placeholder.text = "PLAYER";
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);

            field.textViewport = textArea;
            field.textComponent = text;
            field.placeholder = placeholder;

            return field;
        }

        private static RectTransform EnsureTextArea(Transform parent, string objectName)
        {
            Transform child = parent.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(RectTransform), typeof(RectMask2D)).transform;
                child.SetParent(parent, false);
            }

            RectTransform rect = (RectTransform)child;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(14f, 8f);
            rect.offsetMax = new Vector2(-14f, -8f);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetButtonLabel(Button button, string text)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>() : null;
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }
    }
}
