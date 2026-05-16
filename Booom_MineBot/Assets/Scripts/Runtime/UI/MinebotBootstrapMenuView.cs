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
        private Button skipStoryButton;

        [SerializeField]
        private Button quitButton;

        [SerializeField]
        private Button leaderboardButton;

        [SerializeField]
        private TMP_Text statusText;

        [SerializeField]
        private TMP_Text leaderboardTitleText;

        [SerializeField]
        private TMP_Text leaderboardEntriesText;

        public Button StartButton => startButton;
        public Button SkipStoryButton => skipStoryButton;
        public Button QuitButton => quitButton;
        public Button LeaderboardButton => leaderboardButton;
        public TMP_Text LeaderboardEntriesText => leaderboardEntriesText;
        public TMP_Text StatusText => statusText;

        public bool HasRequiredBindings(out string missingBindings)
        {
            bool missingStart = startButton == null;
            bool missingQuit = quitButton == null;
            bool missingLeaderboard = leaderboardButton == null;
            if (!missingStart && !missingQuit && !missingLeaderboard)
            {
                missingBindings = null;
                return true;
            }

            if (missingStart && missingQuit && missingLeaderboard)
            {
                missingBindings = "startButton, quitButton, leaderboardButton";
                return false;
            }

            var missing = new System.Collections.Generic.List<string>(3);
            if (missingStart)
            {
                missing.Add("startButton");
            }

            if (missingQuit)
            {
                missing.Add("quitButton");
            }

            if (missingLeaderboard)
            {
                missing.Add("leaderboardButton");
            }

            missingBindings = string.Join(", ", missing);
            return false;
        }

        public void BindButtons(Action onStart, Action onQuit, Action onLeaderboard)
        {
            BindButtons(onStart, null, onQuit, onLeaderboard);
        }

        public void BindButtons(Action onStart, Action onSkipStory, Action onQuit, Action onLeaderboard)
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners();
                startButton.onClick.AddListener(() => onStart?.Invoke());
            }

            if (skipStoryButton != null)
            {
                skipStoryButton.onClick.RemoveAllListeners();
                skipStoryButton.onClick.AddListener(() => onSkipStory?.Invoke());
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(() => onQuit?.Invoke());
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.RemoveAllListeners();
                leaderboardButton.onClick.AddListener(() => onLeaderboard?.Invoke());
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
