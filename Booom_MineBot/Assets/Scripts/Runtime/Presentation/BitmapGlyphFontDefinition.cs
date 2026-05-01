using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/表现/位图字形字体")]
    public sealed class BitmapGlyphFontDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class GlyphDefinition
        {
            [SerializeField]
            [InspectorLabel("字符")]
            private string character = string.Empty;

            [SerializeField]
            [InspectorLabel("精灵")]
            private Sprite sprite;

            [SerializeField]
            [InspectorLabel("步进")]
            private float advance = 10f;

            public GlyphDefinition()
            {
            }

            public GlyphDefinition(char glyphCharacter, Sprite glyphSprite, float glyphAdvance)
            {
                character = glyphCharacter.ToString();
                sprite = glyphSprite;
                advance = glyphAdvance;
            }

            public char Character => string.IsNullOrEmpty(character) ? '\0' : character[0];
            public Sprite Sprite => sprite;
            public float Advance => Mathf.Max(1f, advance);
        }

        [SerializeField]
        [InspectorLabel("图集纹理")]
        private Texture2D atlasTexture;

        [SerializeField]
        [InspectorLabel("描述文件")]
        private TextAsset descriptor;

        [SerializeField]
        [InspectorLabel("行高")]
        private float lineHeight = 16f;

        [SerializeField]
        [InspectorLabel("参考字号")]
        private float referenceFontSize = 4f;

        [SerializeField]
        [InspectorLabel("字形列表")]
        private GlyphDefinition[] glyphs = Array.Empty<GlyphDefinition>();

        [NonSerialized]
        private Dictionary<char, GlyphDefinition> glyphLookup;

        public Texture2D AtlasTexture => atlasTexture;
        public TextAsset Descriptor => descriptor;
        public float LineHeight => Mathf.Max(1f, lineHeight);
        public float ReferenceFontSize => Mathf.Max(0.1f, referenceFontSize);
        public IReadOnlyList<GlyphDefinition> Glyphs => glyphs ?? Array.Empty<GlyphDefinition>();

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        public bool TryGetGlyph(char character, out GlyphDefinition glyph)
        {
            if (glyphLookup == null)
            {
                RebuildLookup();
            }

            return glyphLookup.TryGetValue(character, out glyph);
        }

        public void Configure(
            Texture2D configuredAtlasTexture,
            TextAsset configuredDescriptor,
            float configuredLineHeight,
            float configuredReferenceFontSize,
            GlyphDefinition[] configuredGlyphs)
        {
            atlasTexture = configuredAtlasTexture;
            descriptor = configuredDescriptor;
            lineHeight = Mathf.Max(1f, configuredLineHeight);
            referenceFontSize = Mathf.Max(0.1f, configuredReferenceFontSize);
            glyphs = configuredGlyphs ?? Array.Empty<GlyphDefinition>();
            RebuildLookup();
        }

        private void RebuildLookup()
        {
            glyphLookup = new Dictionary<char, GlyphDefinition>();
            if (glyphs == null)
            {
                return;
            }

            for (int i = 0; i < glyphs.Length; i++)
            {
                GlyphDefinition glyph = glyphs[i];
                if (glyph == null || glyph.Sprite == null || glyph.Character == '\0')
                {
                    continue;
                }

                glyphLookup[glyph.Character] = glyph;
            }
        }
    }
}
