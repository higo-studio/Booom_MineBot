using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotScorePageView
    {
        private const string ScoreTextName = "Score Text";
        private const string NameInputName = "InputField (TMP)";
        private const string ConfirmButtonName = "ConfirmButton";

        private readonly GameObject root;
        private readonly TMP_Text scoreText;
        private readonly TMP_InputField nameInputField;
        private readonly Button confirmButton;

        public MinebotScorePageView(GameObject root)
        {
            this.root = root;
            Transform rootTransform = root != null ? root.transform : null;
            scoreText = FindDescendant(rootTransform, ScoreTextName)?.GetComponent<TMP_Text>();
            nameInputField = FindDescendant(rootTransform, NameInputName)?.GetComponent<TMP_InputField>();
            confirmButton = FindDescendant(rootTransform, ConfirmButtonName)?.GetComponent<Button>();
        }

        public GameObject GameObject => root;
        public bool IsVisible => root != null && root.activeSelf;
        public TMP_Text ScoreText => scoreText;
        public TMP_InputField NameInputField => nameInputField;
        public Button SubmitButton => confirmButton;

        public bool HasRequiredBindings(out string missingBindings)
        {
            var missing = new List<string>();
            if (root == null)
            {
                missing.Add("root");
            }

            if (scoreText == null)
            {
                missing.Add("scoreText");
            }

            if (nameInputField == null)
            {
                missing.Add("nameInputField");
            }

            if (confirmButton == null)
            {
                missing.Add("confirmButton");
            }

            missingBindings = missing.Count > 0 ? string.Join(", ", missing) : null;
            return missing.Count == 0;
        }

        public void BindSubmit(Action<string> onSubmit)
        {
            if (confirmButton == null)
            {
                return;
            }

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => onSubmit?.Invoke(GetNameInput()));
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

        public void SetScore(int score)
        {
            SetText(scoreText, Mathf.Max(0, score).ToString());
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

            if (confirmButton != null)
            {
                confirmButton.interactable = enabled;
            }
        }

        public void SetVisible(bool visible)
        {
            if (root == null)
            {
                return;
            }

            if (visible)
            {
                root.transform.SetAsLastSibling();
            }

            root.SetActive(visible);
        }

        private static Transform FindDescendant(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            if (string.Equals(parent.name.Trim(), objectName, StringComparison.Ordinal))
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform match = FindDescendant(parent.GetChild(i), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
