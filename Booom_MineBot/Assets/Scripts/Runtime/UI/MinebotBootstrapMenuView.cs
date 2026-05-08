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

        public void EnsureDefaultStructure(TMP_FontAsset runtimeFontAsset, MinebotHudDefaults.BootstrapMenuLayout layout)
        {
            MinebotHudUiFactory.ConfigureCanvasRoot(gameObject);

            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                MinebotHudUiFactory.StretchToParent(rootRect);
            }

            scrimImage = MinebotHudUiFactory.EnsureStretchImage(scrimImage, transform, "Scrim", layout.ScrimColor);
            if (scrimImage != null)
            {
                scrimImage.color = layout.ScrimColor;
                scrimImage.raycastTarget = true;
            }

            panelRoot = EnsureCenteredPanel(panelRoot, transform, "Panel", layout.PanelSize);
            panelBackgroundImage = MinebotHudUiFactory.GetOrAdd<Image>(panelRoot.gameObject);
            panelBackgroundImage.color = layout.PanelColor;
            panelBackgroundImage.raycastTarget = true;

            titleText = MinebotHudUiFactory.EnsureTopStretchText(
                titleText,
                panelRoot,
                "Title",
                24f,
                28f,
                24f,
                56f,
                layout.TitleFontSize,
                TextAnchor.MiddleCenter,
                runtimeFontAsset);
            titleText.text = "BOOOM MINEBOT";

            subtitleText = MinebotHudUiFactory.EnsureTopStretchText(
                subtitleText,
                panelRoot,
                "Subtitle",
                28f,
                92f,
                28f,
                48f,
                layout.SubtitleFontSize,
                TextAnchor.MiddleCenter,
                runtimeFontAsset);
            subtitleText.text = "启动页";

            startButton = MinebotHudUiFactory.EnsureTopLeftButton(
                startButton,
                panelRoot,
                "Start Button",
                28f,
                156f,
                332f,
                52f,
                layout.SubtitleFontSize,
                layout.ButtonColor,
                TextAnchor.MiddleCenter,
                runtimeFontAsset);
            SetButtonLabel(startButton, "开始游戏");

            quitButton = MinebotHudUiFactory.EnsureTopLeftButton(
                quitButton,
                panelRoot,
                "Quit Button",
                400f,
                156f,
                332f,
                52f,
                layout.SubtitleFontSize,
                layout.ButtonColor,
                TextAnchor.MiddleCenter,
                runtimeFontAsset);
            SetButtonLabel(quitButton, "退出游戏");

            statusText = MinebotHudUiFactory.EnsureTopStretchText(
                statusText,
                panelRoot,
                "Status Text",
                28f,
                220f,
                28f,
                28f,
                16,
                TextAnchor.MiddleCenter,
                runtimeFontAsset);
            statusText.text = string.Empty;

            leaderboardTitleText = MinebotHudUiFactory.EnsureTopStretchText(
                leaderboardTitleText,
                panelRoot,
                "Leaderboard Title",
                28f,
                266f,
                28f,
                32f,
                layout.SubtitleFontSize,
                TextAnchor.MiddleLeft,
                runtimeFontAsset);
            leaderboardTitleText.text = "本地前十";

            leaderboardEntriesText = MinebotHudUiFactory.EnsureBottomStretchText(
                leaderboardEntriesText,
                panelRoot,
                "Leaderboard Entries",
                28f,
                28f,
                28f,
                278f,
                layout.LeaderboardFontSize,
                TextAnchor.UpperLeft,
                runtimeFontAsset);
            leaderboardEntriesText.text = "暂无成绩";
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

        public void SetLeaderboardSummary(string text)
        {
            if (leaderboardEntriesText != null)
            {
                leaderboardEntriesText.text = text ?? string.Empty;
            }
        }

        public void SetLoadingState(bool isLoading)
        {
            if (startButton != null)
            {
                startButton.interactable = !isLoading;
            }

            if (quitButton != null)
            {
                quitButton.interactable = !isLoading;
            }

            if (statusText != null)
            {
                statusText.text = isLoading ? "正在进入玩法..." : string.Empty;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private static RectTransform EnsureCenteredPanel(RectTransform current, Transform parent, string objectName, Vector2 size)
        {
            Transform candidate = current != null ? current : parent.Find(objectName);
            if (candidate == null)
            {
                candidate = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).transform;
                candidate.SetParent(parent, false);
            }

            RectTransform rect = candidate as RectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
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
