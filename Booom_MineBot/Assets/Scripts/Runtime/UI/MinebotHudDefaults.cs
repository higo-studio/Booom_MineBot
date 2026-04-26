using UnityEngine;

namespace Minebot.UI
{
    public static class MinebotHudDefaults
    {
        public const string RootResourcePath = "Minebot/UI/MinebotHud";
        public const string RootAssetPath = "Assets/Resources/Minebot/UI/MinebotHud.prefab";
        public const string PanelFolderResourcePath = "Minebot/UI/Panels";
        public const string PanelFolderAssetPath = "Assets/Resources/Minebot/UI/Panels";

        public const string StatusPanelResourcePath = PanelFolderResourcePath + "/StatusPanel";
        public const string InteractionPanelResourcePath = PanelFolderResourcePath + "/InteractionPanel";
        public const string FeedbackPanelResourcePath = PanelFolderResourcePath + "/FeedbackPanel";
        public const string WarningPanelResourcePath = PanelFolderResourcePath + "/WarningPanel";
        public const string GameOverPanelResourcePath = PanelFolderResourcePath + "/GameOverPanel";
        public const string UpgradePanelResourcePath = PanelFolderResourcePath + "/UpgradePanel";
        public const string BuildPanelResourcePath = PanelFolderResourcePath + "/BuildPanel";
        public const string BuildingInteractionPanelResourcePath = PanelFolderResourcePath + "/BuildingInteractionPanel";

        public const string StatusPanelAssetPath = PanelFolderAssetPath + "/StatusPanel.prefab";
        public const string InteractionPanelAssetPath = PanelFolderAssetPath + "/InteractionPanel.prefab";
        public const string FeedbackPanelAssetPath = PanelFolderAssetPath + "/FeedbackPanel.prefab";
        public const string WarningPanelAssetPath = PanelFolderAssetPath + "/WarningPanel.prefab";
        public const string GameOverPanelAssetPath = PanelFolderAssetPath + "/GameOverPanel.prefab";
        public const string UpgradePanelAssetPath = PanelFolderAssetPath + "/UpgradePanel.prefab";
        public const string BuildPanelAssetPath = PanelFolderAssetPath + "/BuildPanel.prefab";
        public const string BuildingInteractionPanelAssetPath = PanelFolderAssetPath + "/BuildingInteractionPanel.prefab";

        public const string StatusPanelObjectName = "Status Panel";
        public const string InteractionPanelObjectName = "Interaction Panel";
        public const string FeedbackPanelObjectName = "Feedback Panel";
        public const string WarningPanelObjectName = "Warning Panel";
        public const string GameOverPanelObjectName = "Game Over Panel";
        public const string UpgradePanelObjectName = "Upgrade Panel";
        public const string BuildPanelObjectName = "Build Panel";
        public const string BuildingInteractionPanelObjectName = "Building Interaction Panel";

        public const string UpgradeTitle = "升级可用：按 1/2/3 或点击";
        public const string BuildTitle = "建筑模式：选择建筑后点击空地";
        public const string BuildingInteractionTitle = "建筑交互";

        public const int UpgradeButtonCount = 3;
        public const int MinimumBuildButtonCount = 2;
        public const int BuildingInteractionButtonCount = 2;

        public static readonly SlotLayout StatusSlot = new SlotLayout(
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(16f, -16f),
            new Vector2(520f, 128f));

        public static readonly SlotLayout InteractionSlot = new SlotLayout(
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(16f, -136f),
            new Vector2(760f, 110f));

        public static readonly SlotLayout FeedbackSlot = new SlotLayout(
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(16f, -250f),
            new Vector2(720f, 96f));

        public static readonly SlotLayout WarningSlot = new SlotLayout(
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-16f, -16f),
            new Vector2(420f, 110f));

        public static readonly SlotLayout GameOverSlot = new SlotLayout(
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 160f));

        public static readonly SlotLayout UpgradeSlot = new SlotLayout(
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-24f, 24f),
            new Vector2(420f, 230f));

        public static readonly SlotLayout BuildingInteractionSlot = new SlotLayout(
            Vector2.zero,
            Vector2.zero,
            Vector2.zero,
            new Vector2(16f, 24f),
            new Vector2(420f, 142f));

        public static readonly TextPanelLayout StatusText = new TextPanelLayout(22, TextAnchor.UpperLeft, Vector4.zero);
        public static readonly TextPanelLayout InteractionText = new TextPanelLayout(18, TextAnchor.UpperLeft, Vector4.zero);
        public static readonly TextPanelLayout FeedbackText = new TextPanelLayout(20, TextAnchor.UpperLeft, Vector4.zero);
        public static readonly TextPanelLayout WarningText = new TextPanelLayout(22, TextAnchor.UpperRight, Vector4.zero);
        public static readonly TextPanelLayout GameOverText = new TextPanelLayout(42, TextAnchor.MiddleCenter, Vector4.zero);

        public static readonly OptionPanelLayout UpgradeOptions = new OptionPanelLayout(
            new Color(0.05f, 0.07f, 0.08f, 0.92f),
            new Color(0.16f, 0.23f, 0.24f, 0.96f),
            20,
            17,
            16f,
            16f,
            42f,
            64f,
            44f,
            52f);

        public static readonly OptionPanelLayout BuildOptions = new OptionPanelLayout(
            new Color(0.07f, 0.09f, 0.1f, 0.93f),
            new Color(0.18f, 0.22f, 0.2f, 0.96f),
            20,
            17,
            16f,
            12f,
            38f,
            56f,
            44f,
            52f);

        public static readonly OptionPanelLayout BuildingInteractionOptions = new OptionPanelLayout(
            new Color(0.06f, 0.08f, 0.08f, 0.92f),
            new Color(0.17f, 0.24f, 0.22f, 0.98f),
            19,
            17,
            16f,
            12f,
            34f,
            48f,
            40f,
            48f);

        public static SlotLayout BuildSlot(int buttonCount)
        {
            int safeCount = Mathf.Max(MinimumBuildButtonCount, buttonCount);
            return new SlotLayout(
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-24f, -144f),
                new Vector2(420f, Mathf.Max(190f, 86f + safeCount * 52f)));
        }

        public readonly struct SlotLayout
        {
            public SlotLayout(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
            {
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                Pivot = pivot;
                AnchoredPosition = anchoredPosition;
                Size = size;
            }

            public Vector2 AnchorMin { get; }
            public Vector2 AnchorMax { get; }
            public Vector2 Pivot { get; }
            public Vector2 AnchoredPosition { get; }
            public Vector2 Size { get; }
        }

        public readonly struct TextPanelLayout
        {
            public TextPanelLayout(int fontSize, TextAnchor alignment, Vector4 padding)
            {
                FontSize = fontSize;
                Alignment = alignment;
                Padding = padding;
            }

            public int FontSize { get; }
            public TextAnchor Alignment { get; }
            public Vector4 Padding { get; }
        }

        public readonly struct OptionPanelLayout
        {
            public OptionPanelLayout(Color backgroundColor, Color buttonColor, int titleFontSize, int buttonFontSize, float sidePadding, float titleTop, float titleHeight, float buttonTop, float buttonHeight, float buttonSpacing)
            {
                BackgroundColor = backgroundColor;
                ButtonColor = buttonColor;
                TitleFontSize = titleFontSize;
                ButtonFontSize = buttonFontSize;
                SidePadding = sidePadding;
                TitleTop = titleTop;
                TitleHeight = titleHeight;
                ButtonTop = buttonTop;
                ButtonHeight = buttonHeight;
                ButtonSpacing = buttonSpacing;
            }

            public Color BackgroundColor { get; }
            public Color ButtonColor { get; }
            public int TitleFontSize { get; }
            public int ButtonFontSize { get; }
            public float SidePadding { get; }
            public float TitleTop { get; }
            public float TitleHeight { get; }
            public float ButtonTop { get; }
            public float ButtonHeight { get; }
            public float ButtonSpacing { get; }
        }
    }
}
