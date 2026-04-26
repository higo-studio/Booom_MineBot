using System.Collections.Generic;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class BitmapGlyphLabel : MonoBehaviour
    {
        private readonly List<SpriteRenderer> glyphRenderers = new List<SpriteRenderer>();

        public string CurrentText { get; private set; } = string.Empty;

        public int VisibleGlyphCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < glyphRenderers.Count; i++)
                {
                    if (glyphRenderers[i] != null && glyphRenderers[i].gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void SetText(string text, BitmapGlyphFontDefinition font, Color tint, float fontSize, int sortingOrder)
        {
            CurrentText = text ?? string.Empty;
            if (font == null || string.IsNullOrEmpty(CurrentText))
            {
                HideUnused(0);
                return;
            }

            float scale = Mathf.Max(0.1f, fontSize) / font.ReferenceFontSize;
            float totalAdvance = 0f;
            int visibleCount = 0;

            for (int i = 0; i < CurrentText.Length; i++)
            {
                if (!font.TryGetGlyph(CurrentText[i], out BitmapGlyphFontDefinition.GlyphDefinition glyph) || glyph.Sprite == null)
                {
                    continue;
                }

                totalAdvance += GlyphAdvanceUnits(glyph);
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                HideUnused(0);
                return;
            }

            float cursor = -totalAdvance * 0.5f;
            int rendererIndex = 0;

            for (int i = 0; i < CurrentText.Length; i++)
            {
                if (!font.TryGetGlyph(CurrentText[i], out BitmapGlyphFontDefinition.GlyphDefinition glyph) || glyph.Sprite == null)
                {
                    continue;
                }

                SpriteRenderer renderer = EnsureGlyphRenderer(rendererIndex++);
                float advance = GlyphAdvanceUnits(glyph);
                renderer.sprite = glyph.Sprite;
                renderer.color = tint;
                renderer.sortingOrder = sortingOrder;
                renderer.transform.localPosition = new Vector3((cursor + advance * 0.5f) * scale, 0f, 0f);
                renderer.transform.localScale = Vector3.one * scale;
                renderer.gameObject.SetActive(true);
                cursor += advance;
            }

            HideUnused(rendererIndex);
        }

        private SpriteRenderer EnsureGlyphRenderer(int index)
        {
            while (glyphRenderers.Count <= index)
            {
                var glyphObject = new GameObject($"Glyph {glyphRenderers.Count + 1}");
                glyphObject.transform.SetParent(transform, false);
                glyphRenderers.Add(glyphObject.AddComponent<SpriteRenderer>());
            }

            return glyphRenderers[index];
        }

        private void HideUnused(int usedCount)
        {
            for (int i = usedCount; i < glyphRenderers.Count; i++)
            {
                glyphRenderers[i].gameObject.SetActive(false);
            }
        }

        private static float GlyphAdvanceUnits(BitmapGlyphFontDefinition.GlyphDefinition glyph)
        {
            float pixelsPerUnit = glyph.Sprite != null && glyph.Sprite.pixelsPerUnit > 0f ? glyph.Sprite.pixelsPerUnit : 32f;
            return glyph.Advance / pixelsPerUnit;
        }
    }
}
