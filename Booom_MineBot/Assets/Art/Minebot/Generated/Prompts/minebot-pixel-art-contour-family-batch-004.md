# Minebot contour family batch 004

- 基础 prompt：沿用 `minebot-pixel-art-contour-family-003.md` 的 contour-family 方向，明确 wall contour / danger contour / hardness detail / build preview 四类资源同时出图。
- 本批次生成方式：基于 prompt 003 的语义约束，整理出 3 组候选 contour family 方案并落成 `minebot_contour_family_sheet_004_[a|b|c].png`。
- 候选差异：
  - `004_a`：暖土色 wall contour + 较强 hazard stripe danger contour。
  - `004_b`：中性石灰色 wall contour + 亮橙 glow danger contour。
  - `004_c`：冷灰蓝 wall contour + ember red danger contour。
- 最终选择：`004_b`
  - 原因：与现有 floor / boundary / facility / actor 的明暗关系最稳定，危险边界也能与 invalid build preview 保持语义分离。
- 最终消费资源：
  - wall contour：`tile_wall_contour_00.png` - `tile_wall_contour_15.png`
  - danger contour：`tile_danger_contour_00.png` - `tile_danger_contour_15.png`
  - detail：`tile_detail_soil.png` / `tile_detail_stone.png` / `tile_detail_hard_rock.png` / `tile_detail_ultra_hard.png`
  - build preview：`tile_build_preview_valid.png` / `tile_build_preview_invalid.png`
