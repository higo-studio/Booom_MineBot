using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotUpgradePageView
    {
        private const string LayoutName = "Layout";
        private const string SkillCardName = "Skill";
        private const string TitleTextName = "Title Text";
        private const string InfoTextName = "Info Text";
        private const string CostTextName = "Cost Text";
        private const string CancelButtonName = "CancelButton";

        private readonly GameObject root;
        private readonly Button[] optionButtons;
        private readonly TMP_Text[] optionTitleTexts;
        private readonly TMP_Text[] optionInfoTexts;
        private readonly TMP_Text[] optionHintTexts;
        private readonly Button cancelButton;

        public MinebotUpgradePageView(GameObject root)
        {
            this.root = root;

            Transform layout = FindDirectChild(root != null ? root.transform : null, LayoutName);
            optionButtons = new Button[MinebotHudDefaults.UpgradeButtonCount];
            optionTitleTexts = new TMP_Text[MinebotHudDefaults.UpgradeButtonCount];
            optionInfoTexts = new TMP_Text[MinebotHudDefaults.UpgradeButtonCount];
            optionHintTexts = new TMP_Text[MinebotHudDefaults.UpgradeButtonCount];

            for (int i = 0; i < MinebotHudDefaults.UpgradeButtonCount; i++)
            {
                Transform card = layout != null && i < layout.childCount ? layout.GetChild(i) : null;
                if (card == null || !string.Equals(card.name, SkillCardName, StringComparison.Ordinal))
                {
                    continue;
                }

                optionButtons[i] = card.GetComponent<Button>();
                optionTitleTexts[i] = FindDirectChildByTrimmedName(card, TitleTextName)?.GetComponent<TMP_Text>();
                optionInfoTexts[i] = FindDirectChildByTrimmedName(card, InfoTextName)?.GetComponent<TMP_Text>();
                optionHintTexts[i] = FindDirectChildByTrimmedName(card, CostTextName)?.GetComponent<TMP_Text>();
            }

            cancelButton = FindDirectChild(root != null ? root.transform : null, CancelButtonName)?.GetComponent<Button>();
        }

        public GameObject GameObject => root;
        public bool IsVisible => root != null && root.activeSelf;

        public bool HasRequiredBindings(out string missingBindings)
        {
            if (root == null)
            {
                missingBindings = "root";
                return false;
            }

            string[] missing = Array.Empty<string>();
            for (int i = 0; i < MinebotHudDefaults.UpgradeButtonCount; i++)
            {
                AddMissing(ref missing, optionButtons[i] == null, $"optionButtons[{i}]");
                AddMissing(ref missing, optionTitleTexts[i] == null, $"optionTitleTexts[{i}]");
                AddMissing(ref missing, optionInfoTexts[i] == null, $"optionInfoTexts[{i}]");
                AddMissing(ref missing, optionHintTexts[i] == null, $"optionHintTexts[{i}]");
            }

            AddMissing(ref missing, cancelButton == null, "cancelButton");
            missingBindings = missing.Length > 0 ? string.Join(", ", missing) : null;
            return missing.Length == 0;
        }

        public void BindButtons(Action<int> onSelect, Action onCancel)
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
                button.onClick.AddListener(() => onSelect?.Invoke(capturedIndex));
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(() => onCancel?.Invoke());
            }
        }

        public void SetOption(int index, bool visible, string title, string info, string hint)
        {
            if (index < 0 || index >= optionButtons.Length)
            {
                return;
            }

            Button button = optionButtons[index];
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(visible);
            button.interactable = visible;

            SetText(optionTitleTexts[index], title);
            SetText(optionInfoTexts[index], info);
            SetText(optionHintTexts[index], hint);
        }

        public void SetCancelVisible(bool visible)
        {
            if (cancelButton == null)
            {
                return;
            }

            cancelButton.gameObject.SetActive(visible);
            cancelButton.interactable = visible;
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

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static Transform FindDirectChildByTrimmedName(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name.Trim(), childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static void AddMissing(ref string[] missing, bool condition, string entry)
        {
            if (!condition)
            {
                return;
            }

            Array.Resize(ref missing, missing.Length + 1);
            missing[missing.Length - 1] = entry;
        }
    }
}
