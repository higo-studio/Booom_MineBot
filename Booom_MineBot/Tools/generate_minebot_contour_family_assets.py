#!/usr/bin/env python3
"""Generate Minebot contour-family art assets without third-party dependencies."""

from __future__ import annotations

import math
import os
import shutil
import struct
import textwrap
import zlib
from dataclasses import dataclass


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART_ROOT = os.path.join(ROOT, "Assets", "Art", "Minebot")
SPRITE_DIR = os.path.join(ART_ROOT, "Sprites", "Tiles")
SOURCE_DIR = os.path.join(ART_ROOT, "Generated", "SourceSheets")
SELECTED_DIR = os.path.join(ART_ROOT, "Generated", "Selected")
PROMPT_PATH = os.path.join(ART_ROOT, "Generated", "Prompts", "minebot-pixel-art-contour-family-batch-004.md")
MANIFEST_PATH = os.path.join(ART_ROOT, "Generated", "Selected", "minebot-contour-family-asset-manifest.md")

TILE_SIZE = 16
PREVIEW_SCALE = 6
SHEET_BG = (22, 23, 26, 255)
SHEET_FRAME = (44, 46, 52, 255)
CLEAR = (0, 0, 0, 0)


def clamp(value: int, low: int, high: int) -> int:
    return low if value < low else high if value > high else value


def rgba(hex_value: str, alpha: int = 255) -> tuple[int, int, int, int]:
    hex_value = hex_value.lstrip("#")
    return (
        int(hex_value[0:2], 16),
        int(hex_value[2:4], 16),
        int(hex_value[4:6], 16),
        alpha,
    )


@dataclass(frozen=True)
class FamilyPalette:
    name: str
    wall_light: tuple[int, int, int, int]
    wall_mid: tuple[int, int, int, int]
    wall_shadow: tuple[int, int, int, int]
    wall_accent: tuple[int, int, int, int]
    danger_light: tuple[int, int, int, int]
    danger_mid: tuple[int, int, int, int]
    danger_shadow: tuple[int, int, int, int]
    detail_soil_a: tuple[int, int, int, int]
    detail_soil_b: tuple[int, int, int, int]
    detail_stone_a: tuple[int, int, int, int]
    detail_stone_b: tuple[int, int, int, int]
    detail_hard_a: tuple[int, int, int, int]
    detail_hard_b: tuple[int, int, int, int]
    detail_ultra_a: tuple[int, int, int, int]
    detail_ultra_b: tuple[int, int, int, int]
    preview_valid: tuple[int, int, int, int]
    preview_invalid: tuple[int, int, int, int]
    wall_thickness: int
    danger_thickness: int
    danger_mode: str


PALETTES = (
    FamilyPalette(
        name="A",
        wall_light=rgba("#d7bf8b"),
        wall_mid=rgba("#b28c5a"),
        wall_shadow=rgba("#5c4229"),
        wall_accent=rgba("#efe1b6"),
        danger_light=rgba("#ff8160"),
        danger_mid=rgba("#db4025"),
        danger_shadow=rgba("#701d14"),
        detail_soil_a=rgba("#6f4e2e"),
        detail_soil_b=rgba("#8f6335"),
        detail_stone_a=rgba("#5d5c58"),
        detail_stone_b=rgba("#88857f"),
        detail_hard_a=rgba("#34353d"),
        detail_hard_b=rgba("#50535c"),
        detail_ultra_a=rgba("#2a2437"),
        detail_ultra_b=rgba("#4b405c"),
        preview_valid=rgba("#59d0ff"),
        preview_invalid=rgba("#ff6a49"),
        wall_thickness=3,
        danger_thickness=3,
        danger_mode="hazard",
    ),
    FamilyPalette(
        name="B",
        wall_light=rgba("#dad3c7"),
        wall_mid=rgba("#9d9589"),
        wall_shadow=rgba("#514b44"),
        wall_accent=rgba("#f3ece1"),
        danger_light=rgba("#ffae52"),
        danger_mid=rgba("#ff5f34"),
        danger_shadow=rgba("#7b2414"),
        detail_soil_a=rgba("#6c4a2e"),
        detail_soil_b=rgba("#93653b"),
        detail_stone_a=rgba("#5c5a58"),
        detail_stone_b=rgba("#908b85"),
        detail_hard_a=rgba("#2e3139"),
        detail_hard_b=rgba("#4f5664"),
        detail_ultra_a=rgba("#272433"),
        detail_ultra_b=rgba("#5a5371"),
        preview_valid=rgba("#4fd9ff"),
        preview_invalid=rgba("#ff6742"),
        wall_thickness=2,
        danger_thickness=2,
        danger_mode="glow",
    ),
    FamilyPalette(
        name="C",
        wall_light=rgba("#bfc8d4"),
        wall_mid=rgba("#74808f"),
        wall_shadow=rgba("#313846"),
        wall_accent=rgba("#e6edf5"),
        danger_light=rgba("#ff7d62"),
        danger_mid=rgba("#f34733"),
        danger_shadow=rgba("#6f1812"),
        detail_soil_a=rgba("#62462d"),
        detail_soil_b=rgba("#8d5f36"),
        detail_stone_a=rgba("#5a616d"),
        detail_stone_b=rgba("#909aa6"),
        detail_hard_a=rgba("#323743"),
        detail_hard_b=rgba("#555e70"),
        detail_ultra_a=rgba("#212938"),
        detail_ultra_b=rgba("#3d4d64"),
        preview_valid=rgba("#43c8ff"),
        preview_invalid=rgba("#ff7450"),
        wall_thickness=2,
        danger_thickness=3,
        danger_mode="ember",
    ),
)

SELECTED_PALETTE = PALETTES[1]


class Canvas:
    def __init__(self, width: int, height: int, fill: tuple[int, int, int, int] = CLEAR):
        self.width = width
        self.height = height
        self.pixels = [fill] * (width * height)

    def copy(self) -> "Canvas":
        duplicate = Canvas(self.width, self.height)
        duplicate.pixels = self.pixels[:]
        return duplicate

    def set(self, x: int, y: int, color: tuple[int, int, int, int]) -> None:
        if 0 <= x < self.width and 0 <= y < self.height:
            self.pixels[y * self.width + x] = color

    def get(self, x: int, y: int) -> tuple[int, int, int, int]:
        if 0 <= x < self.width and 0 <= y < self.height:
            return self.pixels[y * self.width + x]
        return CLEAR

    def fill(self, color: tuple[int, int, int, int]) -> None:
        self.pixels = [color] * (self.width * self.height)

    def fill_rect(self, x0: int, y0: int, width: int, height: int, color: tuple[int, int, int, int]) -> None:
        for y in range(y0, y0 + height):
            for x in range(x0, x0 + width):
                self.set(x, y, color)

    def blit(self, source: "Canvas", x0: int, y0: int) -> None:
        for y in range(source.height):
            for x in range(source.width):
                color = source.get(x, y)
                if color[3] == 0:
                    continue
                self.set(x0 + x, y0 + y, color)


def write_png(path: str, canvas: Canvas) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    raw = bytearray()
    for y in range(canvas.height):
        raw.append(0)
        for x in range(canvas.width):
            raw.extend(canvas.get(x, y))

    def chunk(chunk_type: bytes, data: bytes) -> bytes:
        return (
            struct.pack(">I", len(data))
            + chunk_type
            + data
            + struct.pack(">I", zlib.crc32(chunk_type + data) & 0xFFFFFFFF)
        )

    header = struct.pack(">IIBBBBB", canvas.width, canvas.height, 8, 6, 0, 0, 0)
    payload = (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", header)
        + chunk(b"IDAT", zlib.compress(bytes(raw), level=9))
        + chunk(b"IEND", b"")
    )
    with open(path, "wb") as file:
        file.write(payload)


def upscale(source: Canvas, factor: int) -> Canvas:
    scaled = Canvas(source.width * factor, source.height * factor, CLEAR)
    for y in range(source.height):
        for x in range(source.width):
            color = source.get(x, y)
            for yy in range(factor):
                for xx in range(factor):
                    scaled.set(x * factor + xx, y * factor + yy, color)
    return scaled


def framed_preview(source: Canvas) -> Canvas:
    scaled = upscale(source, PREVIEW_SCALE)
    preview = Canvas(scaled.width + 12, scaled.height + 12, SHEET_BG)
    preview.fill_rect(0, 0, preview.width, preview.height, SHEET_FRAME)
    preview.fill_rect(2, 2, preview.width - 4, preview.height - 4, SHEET_BG)
    preview.blit(scaled, 6, 6)
    return preview


def contour_flags(index: int) -> tuple[bool, bool, bool, bool]:
    return bool(index & 8), bool(index & 4), bool(index & 2), bool(index & 1)


def contour_color(distance: int, x: int, y: int, index: int, palette: FamilyPalette, danger: bool) -> tuple[int, int, int, int]:
    if danger:
        if palette.danger_mode == "hazard":
            pattern = ((x + y + index) // 2) % 4
            if pattern == 0:
                return palette.danger_shadow
            if pattern in (1, 2):
                return palette.danger_mid
            return palette.danger_light

        if palette.danger_mode == "ember":
            if distance == 0:
                return palette.danger_shadow
            if (x * 3 + y * 5 + index) % 5 == 0:
                return palette.danger_light
            return palette.danger_mid

        if distance == 0:
            return palette.danger_shadow
        if distance == 1:
            return palette.danger_mid
        return palette.danger_light

    sparkle = (x * 11 + y * 7 + index * 3) % 13 == 0
    if distance == 0:
        return palette.wall_shadow
    if sparkle:
        return palette.wall_accent
    if distance == 1:
        return palette.wall_mid
    return palette.wall_light


def make_contour_tile(index: int, palette: FamilyPalette, danger: bool) -> Canvas:
    thickness = palette.danger_thickness if danger else palette.wall_thickness
    canvas = Canvas(TILE_SIZE, TILE_SIZE, CLEAR)
    top_left, top_right, bottom_left, bottom_right = contour_flags(index)

    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            is_top = y < TILE_SIZE // 2
            is_left = x < TILE_SIZE // 2
            x_within = x if is_left else x - TILE_SIZE // 2
            y_within = y if is_top else y - TILE_SIZE // 2
            is_filled = (
                top_left if is_top and is_left else
                top_right if is_top else
                bottom_left if is_left else
                bottom_right
            )
            if not is_filled:
                continue

            distances: list[int] = []
            half = TILE_SIZE // 2
            if is_top and is_left:
                if top_left != top_right and x_within >= half - thickness:
                    distances.append(half - 1 - x_within)
                if top_left != bottom_left and y_within >= half - thickness:
                    distances.append(half - 1 - y_within)
            elif is_top:
                if top_right != top_left and x_within < thickness:
                    distances.append(x_within)
                if top_right != bottom_right and y_within >= half - thickness:
                    distances.append(half - 1 - y_within)
            elif is_left:
                if bottom_left != bottom_right and x_within >= half - thickness:
                    distances.append(half - 1 - x_within)
                if bottom_left != top_left and y_within < thickness:
                    distances.append(y_within)
            else:
                if bottom_right != bottom_left and x_within < thickness:
                    distances.append(x_within)
                if bottom_right != top_right and y_within < thickness:
                    distances.append(y_within)

            if not distances:
                continue

            distance = min(distances)
            color = contour_color(distance, x, y, index, palette, danger)
            canvas.set(x, y, color)

            if danger and palette.danger_mode == "glow":
                glow_positions = (
                    (x - 1, y),
                    (x + 1, y),
                    (x, y - 1),
                    (x, y + 1),
                )
                for glow_x, glow_y in glow_positions:
                    existing = canvas.get(glow_x, glow_y)
                    if existing[3] == 0 and 0 <= glow_x < TILE_SIZE and 0 <= glow_y < TILE_SIZE:
                        canvas.set(glow_x, glow_y, (255, 121, 68, 90))

    return canvas


def dot(canvas: Canvas, x0: int, y0: int, color: tuple[int, int, int, int]) -> None:
    for dy in range(2):
        for dx in range(2):
            canvas.set(x0 + dx, y0 + dy, color)


def make_soil_detail(palette: FamilyPalette) -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, palette.detail_soil_a)
    for y in range(TILE_SIZE):
        for x in range(TILE_SIZE):
            if (x * 5 + y * 3) % 7 == 0:
                canvas.set(x, y, palette.detail_soil_b)
    for x, y in ((2, 3), (11, 4), (5, 8), (9, 11), (3, 13)):
        dot(canvas, x, y, palette.detail_soil_b)
    return canvas


def make_stone_detail(palette: FamilyPalette) -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, palette.detail_stone_a)
    stones = (
        (1, 2, 5, 5),
        (8, 1, 6, 6),
        (3, 9, 5, 4),
        (10, 9, 4, 5),
    )
    for x0, y0, width, height in stones:
        for y in range(y0, y0 + height):
            for x in range(x0, x0 + width):
                if (x - x0 - width // 2) ** 2 + (y - y0 - height // 2) ** 2 <= max(width, height) ** 2 // 4:
                    canvas.set(x, y, palette.detail_stone_b)
    return canvas


def make_hard_detail(palette: FamilyPalette) -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, palette.detail_hard_a)
    for x in range(TILE_SIZE):
        y = 3 + (x // 2)
        if y < TILE_SIZE:
            canvas.set(x, y, palette.detail_hard_b)
    for y in range(TILE_SIZE):
        x = 10 - (y // 2)
        if 0 <= x < TILE_SIZE:
            canvas.set(x, y, palette.detail_hard_b)
    for x, y in ((2, 5), (12, 6), (8, 12)):
        dot(canvas, x, y, palette.detail_hard_b)
    return canvas


def make_ultra_detail(palette: FamilyPalette) -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, palette.detail_ultra_a)
    points = ((3, 2), (10, 3), (6, 7), (12, 10), (4, 11), (8, 13))
    for x, y in points:
        canvas.set(x, y, palette.detail_ultra_b)
        canvas.set(x + 1, y, palette.detail_ultra_b)
        canvas.set(x, y + 1, palette.detail_ultra_b)
    for y in range(2, 14, 3):
        for x in range(1, 15):
            if (x + y) % 5 == 0:
                canvas.set(x, y, palette.detail_ultra_b)
    return canvas


def make_build_preview(valid: bool, palette: FamilyPalette) -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, CLEAR)
    color = palette.preview_valid if valid else palette.preview_invalid
    shadow = (color[0] // 3, color[1] // 3, color[2] // 3, 210)

    corners = ((1, 1), (11, 1), (1, 11), (11, 11))
    for x0, y0 in corners:
        for i in range(4):
            canvas.set(x0 + i, y0, shadow if i == 0 else color)
            canvas.set(x0, y0 + i, shadow if i == 0 else color)

    if valid:
        for x in range(6, 10):
            canvas.set(x, 7, color)
        for y in range(5, 9):
            canvas.set(7, y, color)
        canvas.set(8, 6, color)
        canvas.set(8, 7, color)
    else:
        for i in range(3, 13):
            if i % 2 == 0:
                canvas.set(i, i - 1, color)
                canvas.set(15 - i, i - 1, color)
        for i in range(2, 14):
            if i % 2 == 1:
                canvas.set(i, 1, color)
                canvas.set(i, 14, color)
                canvas.set(1, i, color)
                canvas.set(14, i, color)
    return canvas


def make_reference_floor() -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, rgba("#2f261d"))
    for x, y in ((2, 3), (5, 7), (10, 4), (12, 11), (3, 12), (7, 2)):
        dot(canvas, x, y, rgba("#5a4329"))
    for x in range(TILE_SIZE):
        canvas.set(x, 0, rgba("#1b1612"))
        canvas.set(x, TILE_SIZE - 1, rgba("#1b1612"))
    for y in range(TILE_SIZE):
        canvas.set(0, y, rgba("#1b1612"))
        canvas.set(TILE_SIZE - 1, y, rgba("#1b1612"))
    return canvas


def make_reference_boundary() -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, rgba("#2e2f33"))
    canvas.fill_rect(1, 1, 14, 14, rgba("#4c4e55"))
    canvas.fill_rect(3, 3, 10, 10, rgba("#2c2d31"))
    for x in range(2, 14):
        canvas.set(x, 2, rgba("#757983"))
        canvas.set(x, 13, rgba("#757983"))
    for y in range(2, 14):
        canvas.set(2, y, rgba("#757983"))
        canvas.set(13, y, rgba("#757983"))
    return canvas


def make_reference_marker() -> Canvas:
    canvas = make_reference_floor()
    for y in range(3, 13):
        canvas.set(7, y, rgba("#c9c9c9"))
    for x in range(7, 13):
        canvas.set(x, 4, rgba("#ff5b4f"))
        canvas.set(x, 5, rgba("#ff5b4f"))
    for x in range(6, 9):
        canvas.set(x, 2, rgba("#d8d8d8"))
    return canvas


def make_reference_actor(primary: tuple[int, int, int, int], accent: tuple[int, int, int, int]) -> Canvas:
    canvas = Canvas(TILE_SIZE, TILE_SIZE, CLEAR)
    canvas.fill_rect(4, 3, 8, 8, primary)
    canvas.fill_rect(6, 6, 4, 2, accent)
    canvas.fill_rect(3, 11, 3, 4, rgba("#35373d"))
    canvas.fill_rect(10, 11, 3, 4, rgba("#35373d"))
    canvas.fill_rect(7, 11, 2, 4, rgba("#474b52"))
    for x in range(5, 11):
        canvas.set(x, 2, rgba("#d7d7d7"))
    return canvas


def generate_source_sheet(palette: FamilyPalette) -> Canvas:
    wall_tiles = [make_contour_tile(index, palette, False) for index in range(16)]
    danger_tiles = [make_contour_tile(index, palette, True) for index in range(16)]
    detail_tiles = [
        make_soil_detail(palette),
        make_stone_detail(palette),
        make_hard_detail(palette),
        make_ultra_detail(palette),
    ]
    build_tiles = [make_build_preview(True, palette), make_build_preview(False, palette)]
    references = [
        make_reference_floor(),
        make_reference_boundary(),
        make_reference_marker(),
        make_reference_actor(rgba("#f0b324"), rgba("#2c9df5")),
        make_reference_actor(rgba("#7ece34"), rgba("#173515")),
    ]

    previews = [framed_preview(tile) for tile in wall_tiles + danger_tiles + detail_tiles + build_tiles + references]
    columns = 6
    gap = 8
    cell_w = previews[0].width
    cell_h = previews[0].height
    rows = math.ceil(len(previews) / columns)
    sheet = Canvas(columns * cell_w + (columns + 1) * gap, rows * cell_h + (rows + 1) * gap, SHEET_BG)
    for i, preview in enumerate(previews):
        row = i // columns
        column = i % columns
        x0 = gap + column * (cell_w + gap)
        y0 = gap + row * (cell_h + gap)
        sheet.blit(preview, x0, y0)
    return sheet


def generate_final_assets(palette: FamilyPalette) -> dict[str, list[str]]:
    generated: dict[str, list[str]] = {
        "wall": [],
        "danger": [],
        "detail": [],
        "build": [],
    }

    for index in range(16):
        wall_path = os.path.join(SPRITE_DIR, f"tile_wall_contour_{index:02d}.png")
        danger_path = os.path.join(SPRITE_DIR, f"tile_danger_contour_{index:02d}.png")
        write_png(wall_path, make_contour_tile(index, palette, False))
        write_png(danger_path, make_contour_tile(index, palette, True))
        generated["wall"].append(wall_path)
        generated["danger"].append(danger_path)

    detail_generators = (
        ("soil", make_soil_detail),
        ("stone", make_stone_detail),
        ("hard_rock", make_hard_detail),
        ("ultra_hard", make_ultra_detail),
    )
    for suffix, builder in detail_generators:
        path = os.path.join(SPRITE_DIR, f"tile_detail_{suffix}.png")
        write_png(path, builder(palette))
        generated["detail"].append(path)

    for suffix, valid in (("valid", True), ("invalid", False)):
        path = os.path.join(SPRITE_DIR, f"tile_build_preview_{suffix}.png")
        write_png(path, make_build_preview(valid, palette))
        generated["build"].append(path)

    return generated


def write_prompt_and_batch_notes() -> None:
    body = textwrap.dedent(
        """
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
        """
    ).strip() + "\n"

    with open(PROMPT_PATH, "w", encoding="utf-8") as file:
        file.write(body)


def write_manifest(generated: dict[str, list[str]]) -> None:
    rows = []
    selected_sheet = "Assets/Art/Minebot/Generated/Selected/minebot_contour_family_sheet_004_selected.png"

    for index, path in enumerate(generated["wall"]):
        rows.append(
            f"| `wall-{index:02d}` | wall contour 4-bit index `{index}` | `{selected_sheet}` cell `W{index:02d}` | `{to_repo_path(path)}` | `Wall Contour Tilemap` 连续岩壁边界 |"
        )
    for index, path in enumerate(generated["danger"]):
        rows.append(
            f"| `danger-{index:02d}` | danger contour 4-bit index `{index}` | `{selected_sheet}` cell `D{index:02d}` | `{to_repo_path(path)}` | `Danger Contour Tilemap` 危险边界 |"
        )

    detail_semantics = (
        ("soil", "Soil 细颗粒 detail"),
        ("stone", "Stone 卵石 detail"),
        ("hard_rock", "HardRock 裂隙 detail"),
        ("ultra_hard", "UltraHard 深色晶裂 detail"),
    )
    for (suffix, semantics), path in zip(detail_semantics, generated["detail"]):
        rows.append(
            f"| `{suffix}` | {semantics} | `{selected_sheet}` detail slot `{suffix}` | `{to_repo_path(path)}` | world-grid hardness detail / overlay |"
        )

    rows.append(
        f"| `build-preview-valid` | 可建造预览 | `{selected_sheet}` build slot `valid` | `{to_repo_path(generated['build'][0])}` | `BuildPreview` 合法状态 |"
    )
    rows.append(
        f"| `build-preview-invalid` | 非法建造预览 | `{selected_sheet}` build slot `invalid` | `{to_repo_path(generated['build'][1])}` | `BuildPreview` 非法状态，避免与 danger contour 混淆 |"
    )

    body = "\n".join(
        [
            "# Minebot contour family asset manifest",
            "",
            "- 选中源图：`Assets/Art/Minebot/Generated/Selected/minebot_contour_family_sheet_004_selected.png`",
            "- 参考旧批次：`Assets/Art/Minebot/Generated/Selected/minebot_pixel_sheet_001_selected.png`",
            "",
            "| 资源 | 语义 | 源图路径 | 切片路径 | 层级职责 |",
            "| --- | --- | --- | --- | --- |",
            *rows,
            "",
        ]
    )
    with open(MANIFEST_PATH, "w", encoding="utf-8") as file:
        file.write(body)


def to_repo_path(path: str) -> str:
    return os.path.relpath(path, ROOT).replace(os.sep, "/")


def main() -> None:
    os.makedirs(SPRITE_DIR, exist_ok=True)
    os.makedirs(SOURCE_DIR, exist_ok=True)
    os.makedirs(SELECTED_DIR, exist_ok=True)

    for palette in PALETTES:
        sheet_path = os.path.join(SOURCE_DIR, f"minebot_contour_family_sheet_004_{palette.name.lower()}.png")
        write_png(sheet_path, generate_source_sheet(palette))

    selected_source = os.path.join(SOURCE_DIR, "minebot_contour_family_sheet_004_b.png")
    selected_target = os.path.join(SELECTED_DIR, "minebot_contour_family_sheet_004_selected.png")
    shutil.copyfile(selected_source, selected_target)

    generated = generate_final_assets(SELECTED_PALETTE)
    write_prompt_and_batch_notes()
    write_manifest(generated)


if __name__ == "__main__":
    main()
