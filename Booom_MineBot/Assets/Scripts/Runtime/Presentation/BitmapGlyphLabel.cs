using System.Collections.Generic;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class BitmapGlyphLabel : MonoBehaviour
    {
        private readonly List<SpriteRenderer> glyphRenderers = new List<SpriteRenderer>();

        public string CurrentText { get; private set; } = string.Empty;

        [Header("玩家靠近检测")]
        [SerializeField]
        [InspectorLabel("检测半径")]
        private float playerDetectionRadius = 1f;

        [SerializeField]
        [InspectorLabel("靠近时透明度")]
        private float nearbyAlpha = 0.1f;

        private Color baseColor = Color.white;
        private float targetAlpha = 1f;
        private float currentAlpha = 1f;
        private Transform playerTransform;
        private bool isNearPlayer;

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

        private void Start()
        {
            // 延迟一帧获取玩家对象，等待场景初始化
            Invoke(nameof(FindPlayer), 0.1f);
        }

        private void FindPlayer()
        {
            // 尝试查找玩家对象
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // 如果没有Player标签，尝试通过名称查找
                player = GameObject.Find("Player View");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }

        private void Update()
        {
            // 检测玩家是否靠近
            CheckPlayerProximity();

            // 平滑过渡透明度
            if (!Mathf.Approximately(currentAlpha, targetAlpha))
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * 5f);
                UpdateGlyphAlpha();
            }
        }

        private void CheckPlayerProximity()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                return;
            }

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool shouldBeNear = distance <= playerDetectionRadius;

            if (shouldBeNear != isNearPlayer)
            {
                isNearPlayer = shouldBeNear;
                targetAlpha = isNearPlayer ? nearbyAlpha : 1f;
            }
        }

        private void UpdateGlyphAlpha()
        {
            for (int i = 0; i < glyphRenderers.Count; i++)
            {
                if (glyphRenderers[i] != null && glyphRenderers[i].gameObject.activeSelf)
                {
                    Color color = glyphRenderers[i].color;
                    color.a = currentAlpha;
                    glyphRenderers[i].color = color;
                }
            }
        }

        public void SetText(string text, BitmapGlyphFontDefinition font, Color tint, float fontSize, int sortingOrder)
        {
            CurrentText = text ?? string.Empty;
            baseColor = new Color(tint.r, tint.g, tint.b, 1f);

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
                renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, currentAlpha);
                renderer.sortingOrder = sortingOrder;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
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