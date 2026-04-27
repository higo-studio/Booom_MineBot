using UnityEngine;

namespace Minebot.UI
{
    public static class MinebotHudDefaults
    {
        public const string RootResourcePath = "Minebot/UI/MainUI";
        public const string RootAssetPath = "Assets/Resources/Minebot/UI/MainUI.prefab";
        public const string PanelFolderResourcePath = "Minebot/UI/Panels";
        public const string PanelFolderAssetPath = "Assets/Resources/Minebot/UI/Panels";

        public const string StatusPanelResourcePath = PanelFolderResourcePath + "/StatusPanel";
        public const string InteractionPanelResourcePath = PanelFolderResourcePath + "/InteractionPanel";
        public const string FeedbackPanelResourcePath = PanelFolderResourcePath + "/FeedbackPanel";
        public const string WarningPanelResourcePath = PanelFolderResourcePath + "/WarningPanel";
        public const string GameOverPanelResourcePath = PanelFolderResourcePath + "/GameOverPanel";
        public const string MinimapPanelResourcePath = PanelFolderResourcePath + "/MinimapPanel";
        public const string UpgradePanelResourcePath = PanelFolderResourcePath + "/UpgradePanel";
        public const string BuildPanelResourcePath = PanelFolderResourcePath + "/BuildPanel";
        public const string BuildingInteractionPanelResourcePath = PanelFolderResourcePath + "/BuildingInteractionPanel";

        public const string StatusPanelAssetPath = PanelFolderAssetPath + "/StatusPanel.prefab";
        public const string InteractionPanelAssetPath = PanelFolderAssetPath + "/InteractionPanel.prefab";
        public const string FeedbackPanelAssetPath = PanelFolderAssetPath + "/FeedbackPanel.prefab";
        public const string WarningPanelAssetPath = PanelFolderAssetPath + "/WarningPanel.prefab";
        public const string GameOverPanelAssetPath = PanelFolderAssetPath + "/GameOverPanel.prefab";
        public const string MinimapPanelAssetPath = PanelFolderAssetPath + "/MinimapPanel.prefab";
        public const string UpgradePanelAssetPath = PanelFolderAssetPath + "/UpgradePanel.prefab";
        public const string BuildPanelAssetPath = PanelFolderAssetPath + "/BuildPanel.prefab";
        public const string BuildingInteractionPanelAssetPath = PanelFolderAssetPath + "/BuildingInteractionPanel.prefab";

        public const string StatusPanelObjectName = "Status Panel";
        public const string InteractionPanelObjectName = "Interaction Panel";
        public const string FeedbackPanelObjectName = "Feedback Panel";
        public const string WarningPanelObjectName = "Warning Panel";
        public const string GameOverPanelObjectName = "Game Over Panel";
        public const string MinimapPanelObjectName = "Minimap Panel";
        public const string UpgradePanelObjectName = "Upgrade Panel";
        public const string BuildPanelObjectName = "Build Panel";
        public const string BuildingInteractionPanelObjectName = "Building Interaction Panel";

        public const string UpgradeTitle = "升级可用：按 1/2/3 或点击";
        public const string BuildTitle = "建筑模式：选择建筑后点击空地";
        public const string BuildingInteractionTitle = "建筑交互";

        public const int UpgradeButtonCount = 3;
        public const int MinimumBuildButtonCount = 4;
        public const int BuildingInteractionButtonCount = 2;
        public const float HudPanelSliceBorder = 12f;

        public static readonly SlotLayout StatusSlot = new SlotLayout(
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(28f, -22f),
            new Vector2(254f, 116f));

        public static readonly SlotLayout InteractionSlot = new SlotLayout(
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(28f, 150f),
            new Vector2(168f, 104f));

        public static readonly SlotLayout FeedbackSlot = new SlotLayout(
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-28f, 166f),
            new Vector2(140f, 36f));

        public static readonly SlotLayout WarningSlot = new SlotLayout(
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -18f),
            new Vector2(528f, 86f));

        public static readonly SlotLayout GameOverSlot = new SlotLayout(
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 160f));

        public static readonly SlotLayout MinimapSlot = new SlotLayout(
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(28f, 24f),
            new Vector2(392f, 170f));

        public static readonly SlotLayout UpgradeSlot = new SlotLayout(
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-28f, 174f),
            new Vector2(396f, 168f));

        public static readonly SlotLayout BuildingInteractionSlot = new SlotLayout(
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-28f, 142f),
            new Vector2(332f, 120f));

        public static readonly TextPanelLayout StatusText = new TextPanelLayout(22, TextAnchor.UpperLeft, new Vector4(4f, 4f, 4f, 4f));
        public static readonly TextPanelLayout InteractionText = new TextPanelLayout(20, TextAnchor.MiddleLeft, new Vector4(4f, 4f, 4f, 4f));
        public static readonly TextPanelLayout FeedbackText = new TextPanelLayout(15, TextAnchor.MiddleRight, new Vector4(4f, 4f, 4f, 4f));
        public static readonly TextPanelLayout WarningText = new TextPanelLayout(19, TextAnchor.MiddleCenter, new Vector4(8f, 4f, 8f, 4f));
        public static readonly TextPanelLayout GameOverText = new TextPanelLayout(42, TextAnchor.MiddleCenter, Vector4.zero);
        public static readonly MinimapPanelLayout MinimapPanel = new MinimapPanelLayout(16f, 16f, 104f, 40f, 18);

        public static readonly OptionPanelLayout UpgradeOptions = new OptionPanelLayout(
            new Color(0.05f, 0.07f, 0.08f, 0.92f),
            new Color(0.16f, 0.23f, 0.24f, 0.96f),
            new Color(0.24f, 0.38f, 0.42f, 1f),
            20,
            17,
            16f,
            16f,
            42f,
            64f,
            44f,
            52f,
            0f,
            TextAnchor.MiddleLeft,
            OptionPanelFlow.Vertical);

        public static readonly OptionPanelLayout BuildOptions = new OptionPanelLayout(
            new Color(0.03f, 0.05f, 0.06f, 0.9f),
            new Color(0.04f, 0.12f, 0.15f, 0.96f),
            new Color(0.02f, 0.33f, 0.38f, 1f),
            16,
            13,
            8f,
            4f,
            18f,
            28f,
            72f,
            90f,
            74f,
            TextAnchor.MiddleCenter,
            OptionPanelFlow.Horizontal);

        public static readonly OptionPanelLayout BuildingInteractionOptions = new OptionPanelLayout(
            new Color(0.06f, 0.08f, 0.08f, 0.92f),
            new Color(0.17f, 0.24f, 0.22f, 0.98f),
            new Color(0.24f, 0.36f, 0.32f, 1f),
            18,
            16,
            14f,
            12f,
            30f,
            42f,
            38f,
            44f,
            0f,
            TextAnchor.MiddleLeft,
            OptionPanelFlow.Vertical);

        public static SlotLayout BuildSlot(int buttonCount)
        {
            int safeCount = Mathf.Max(MinimumBuildButtonCount, buttonCount);
            float width = 24f + safeCount * BuildOptions.ButtonSpacing;
            return new SlotLayout(
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-28f, 24f),
                new Vector2(Mathf.Max(336f, width), 112f));
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

        public readonly struct MinimapPanelLayout
        {
            public MinimapPanelLayout(float sidePadding, float topPadding, float mapSize, float summaryHeight, int summaryFontSize)
            {
                SidePadding = sidePadding;
                TopPadding = topPadding;
                MapSize = mapSize;
                SummaryHeight = summaryHeight;
                SummaryFontSize = summaryFontSize;
            }

            public float SidePadding { get; }
            public float TopPadding { get; }
            public float MapSize { get; }
            public float SummaryHeight { get; }
            public int SummaryFontSize { get; }
        }

        public enum OptionPanelFlow
        {
            Vertical = 0,
            Horizontal = 1
        }

        public readonly struct OptionPanelLayout
        {
            public OptionPanelLayout(Color backgroundColor, Color buttonColor, Color selectedButtonColor, int titleFontSize, int buttonFontSize, float sidePadding, float titleTop, float titleHeight, float buttonTop, float buttonHeight, float buttonSpacing, float buttonWidth, TextAnchor buttonTextAlignment, OptionPanelFlow buttonFlow)
            {
                BackgroundColor = backgroundColor;
                ButtonColor = buttonColor;
                SelectedButtonColor = selectedButtonColor;
                TitleFontSize = titleFontSize;
                ButtonFontSize = buttonFontSize;
                SidePadding = sidePadding;
                TitleTop = titleTop;
                TitleHeight = titleHeight;
                ButtonTop = buttonTop;
                ButtonHeight = buttonHeight;
                ButtonSpacing = buttonSpacing;
                ButtonWidth = buttonWidth;
                ButtonTextAlignment = buttonTextAlignment;
                ButtonFlow = buttonFlow;
            }

            public Color BackgroundColor { get; }
            public Color ButtonColor { get; }
            public Color SelectedButtonColor { get; }
            public int TitleFontSize { get; }
            public int ButtonFontSize { get; }
            public float SidePadding { get; }
            public float TitleTop { get; }
            public float TitleHeight { get; }
            public float ButtonTop { get; }
            public float ButtonHeight { get; }
            public float ButtonSpacing { get; }
            public float ButtonWidth { get; }
            public TextAnchor ButtonTextAlignment { get; }
            public OptionPanelFlow ButtonFlow { get; }
        }
    }
}
