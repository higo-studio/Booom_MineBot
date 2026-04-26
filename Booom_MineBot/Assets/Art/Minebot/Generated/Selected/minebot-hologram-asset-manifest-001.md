# Minebot 全息反馈资产台账 001

## 采用结果

- overlay atlas: `Assets/Art/Minebot/Sprites/UI/Hologram/hologram_overlay_atlas.png`
- BMFont atlas: `Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.png`
- BMFont descriptor: `Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.fnt`
- digit sprites: `Assets/Art/Minebot/Sprites/UI/Hologram/Glyphs/hologram_digit_0.png` - `hologram_digit_9.png`
- bitmap glyph font asset: `Assets/Resources/Minebot/MinebotBitmapGlyphFont_Default.asset`
- runtime art set: `Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset`

## 绑定关系

- `bitmapGlyphFont` -> `MinebotBitmapGlyphFont_Default.asset`
- `bitmapGlyphAtlas` -> `hologram_bmfont_digits.png`
- `bitmapGlyphDescriptor` -> `hologram_bmfont_digits.fnt`
- `hologramOverlayAtlas` -> `hologram_overlay_atlas.png`
- `dangerTile` / `markerTile` / `scanHintTile` / `dangerOutlineTiles` / `dangerContourTiles` 使用同一批次的全息几何语言
