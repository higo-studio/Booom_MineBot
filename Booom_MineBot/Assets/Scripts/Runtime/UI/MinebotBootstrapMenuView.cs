using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minebot.UI
{
    public sealed class MinebotBootstrapMenuView : MonoBehaviour
    {
        [SerializeField]
        private Image scrimImage;

        [SerializeField]
        private RectTransform panelRoot;

        [SerializeField]
        private Image panelBackgroundImage;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text subtitleText;

        [SerializeField]
        private Button startButton;

        [SerializeField]
        private Button quitButton;

        [SerializeField]
        private TMP_Text statusText;

        [SerializeField]
        private TMP_Text leaderboardTitleText;

        [SerializeField]
        private TMP_Text leaderboardEntriesText;

        public Button StartButton => startButton;
        public Button QuitButton => quitButton;
        public TMP_Text LeaderboardEntriesText => leaderboardEntriesText;
        public TMP_Text StatusText => statusText;

        public bool HasRequiredBindings(out string missingBindings)
        {
            bool missingStart = startButton == null;
            bool missingQuit = quitButton == null;
            if (!missingStart && !missingQuit)
            {
                missingBindings = null;
                return true;
            }

            if (missingStart && missingQuit)
            {
                missingBindings = "startButton, quitButton";
                return false;
            }

            missingBindings = missingStart ? "startButton" : "quitButton";
            return false;
        }

        public void BindButtons(Action onStart, Action onQuit)
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => onStart?.Invoke());
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(() => onQuit?.Invoke());
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
