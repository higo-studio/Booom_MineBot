using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Minebot.UI
{
    public static class MinebotHudFontUtility
    {
        private const string BundledChineseFontRelativePath = "Minebot/Fonts/NotoSansSC-Regular.ttf";
        private static TMP_FontAsset defaultFontAsset;

        public static TMP_FontAsset GetDefaultFontAsset()
        {
            if (defaultFontAsset != null)
            {
                return defaultFontAsset;
            }

            defaultFontAsset = CreateBundledChineseFontAsset();
            if (defaultFontAsset != null)
            {
                return defaultFontAsset;
            }

            defaultFontAsset = CreateRuntimeChineseFontAsset();
            if (defaultFontAsset != null)
            {
                return defaultFontAsset;
            }

            defaultFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return defaultFontAsset;
        }

        private static TMP_FontAsset CreateBundledChineseFontAsset()
        {
            return CreateFontAssetFromFile(Path.Combine(Application.streamingAssetsPath, BundledChineseFontRelativePath));
        }

        private static TMP_FontAsset CreateRuntimeChineseFontAsset()
        {
            string[] preferredFontFiles =
            {
                "/System/Library/Fonts/Supplemental/Arial Unicode.ttf",
                "/Library/Fonts/Arial Unicode.ttf",
                "/System/Library/Fonts/STHeiti Medium.ttc",
                "/System/Library/Fonts/Hiragino Sans GB.ttc"
            };

            foreach (string fontPath in preferredFontFiles)
            {
                TMP_FontAsset fontAsset = CreateFontAssetFromFile(fontPath);
                if (fontAsset != null)
                {
                    return fontAsset;
                }
            }

            string[] preferredFonts =
            {
                "PingFang SC",
                "Hiragino Sans GB",
                "Heiti SC",
                "STHeiti",
                "Noto Sans CJK SC",
                "Noto Sans CJK",
                "Microsoft YaHei",
                "SimHei",
                "Arial Unicode MS"
            };

            foreach (string fontName in preferredFonts)
            {
                if (!IsOsFontInstalled(fontName))
                {
                    continue;
                }

                Font font = Font.CreateDynamicFontFromOSFont(fontName, 90);
                if (font == null)
                {
                    continue;
                }

                TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                if (fontAsset == null)
                {
                    continue;
                }

                fontAsset.name = $"Minebot TMP {fontName}";
                WarmupMinebotGlyphs(fontAsset);
                return fontAsset;
            }

            return null;
        }

        private static TMP_FontAsset CreateFontAssetFromFile(string fontPath)
        {
            if (!File.Exists(fontPath))
            {
                return null;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(fontPath, 0, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048);
            if (fontAsset == null)
            {
                return null;
            }

            fontAsset.name = $"Minebot TMP {Path.GetFileNameWithoutExtension(fontPath)}";
            WarmupMinebotGlyphs(fontAsset);
            return fontAsset;
        }

        private static void WarmupMinebotGlyphs(TMP_FontAsset fontAsset)
        {
            fontAsset.TryAddCharacters("方向键移动挖掘金属能量等级经验波次当前位置钻头可交互维修站机器人工厂恢复生命生产从属机器人升级可用点击地震倒计时红色区域危险风险立即避开尚未探测上次任务失败核心机体失效炸药标记取消不足完成应用选择暂停土层石层硬岩极硬已挖开触发目标无效地形阻挡强度未知结果空格当前版本暂未冻结时间周边感知启动前沿读壁刷新新无需手会附近最高值处蓝色中心输入已锁定先选择升级下一波危险带厚度已标记格自由贴墙自动建筑模式鼠标空地右键退出占地不可建造按钮执行轮廓边界");
        }

        private static bool IsOsFontInstalled(string fontName)
        {
            string[] installedFonts = Font.GetOSInstalledFontNames();
            for (int i = 0; i < installedFonts.Length; i++)
            {
                if (string.Equals(installedFonts[i], fontName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
