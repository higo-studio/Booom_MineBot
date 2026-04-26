#!/usr/bin/env python3
"""Rebuild Minebot art assets from the 005 style-refresh source sheets."""

from __future__ import annotations

import collections
import os
import shutil
import struct
import zlib
from dataclasses import dataclass

from generate_minebot_contour_family_assets import Canvas, write_png


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART_ROOT = os.path.join(ROOT, "Assets", "Art", "Minebot")
SOURCE_DIR = os.path.join(ART_ROOT, "Generated", "SourceSheets")
SELECTED_DIR = os.path.join(ART_ROOT, "Generated", "Selected")
SPRITE_TILE_DIR = os.path.join(ART_ROOT, "Sprites", "Tiles")
SPRITE_ACTOR_DIR = os.path.join(ART_ROOT, "Sprites", "Actors")

GENERATED_ROOT = os.path.join(os.path.expanduser("~"), ".codex", "generated_images", "019dc972-70d6-7231-8b3e-8fe3fbac7f3d")

MAIN_SOURCE = os.path.join(GENERATED_ROOT, "ig_08bad29ffbbf4f0e0169ee0cad3df4819181d0f360eea75e7f.png")
CONTOUR_SOURCE = os.path.join(GENERATED_ROOT, "ig_08bad29ffbbf4f0e0169ee0c5cd8b48191a4659c9db806dbc0.png")
ACTOR_SOURCE = os.path.join(GENERATED_ROOT, "ig_08bad29ffbbf4f0e0169ee0d279ca08191abbc1f6b5ad6c894.png")

MAIN_TARGET_SOURCE = os.path.join(SOURCE_DIR, "minebot_pixel_sheet_005_a.png")
MAIN_TARGET_SELECTED = os.path.join(SELECTED_DIR, "minebot_pixel_sheet_005_selected.png")
CONTOUR_TARGET_SOURCE = os.path.join(SOURCE_DIR, "minebot_contour_family_sheet_005_a.png")
CONTOUR_TARGET_SELECTED = os.path.join(SELECTED_DIR, "minebot_contour_family_sheet_005_selected.png")
ACTOR_TARGET_SOURCE = os.path.join(SOURCE_DIR, "minebot_actor_optimized_sheet_005.png")

CONTOUR_GRID_X = (14, 190, 366, 543, 718, 893, 1067)
CONTOUR_GRID_Y = (12, 195, 376, 557, 736, 915, 1094)
CONTOUR_CELL_WIDTH = 169
CONTOUR_CELL_HEIGHT = 177


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


@dataclass(frozen=True)
class Component:
    x0: int
    y0: int
    x1: int
    y1: int
    area: int

    @property
    def width(self) -> int:
        return self.x1 - self.x0 + 1

    @property
    def height(self) -> int:
        return self.y1 - self.y0 + 1

    @property
    def center_y(self) -> float:
        return (self.y0 + self.y1) / 2.0


def ensure_dirs() -> None:
    for path in (SOURCE_DIR, SELECTED_DIR, SPRITE_TILE_DIR, SPRITE_ACTOR_DIR):
        os.makedirs(path, exist_ok=True)


def read_png(path: str) -> Canvas:
    with open(path, "rb") as file:
        payload = file.read()

    if not payload.startswith(PNG_SIGNATURE):
        raise ValueError(f"{path} is not a PNG file")

    width = height = bit_depth = color_type = None
    idat = bytearray()
    cursor = len(PNG_SIGNATURE)
    while cursor < len(payload):
        length = struct.unpack(">I", payload[cursor:cursor + 4])[0]
        chunk_type = payload[cursor + 4:cursor + 8]
        data = payload[cursor + 8:cursor + 8 + length]
        cursor += 12 + length

        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, compression, filter_method, interlace = struct.unpack(">IIBBBBB", data)
            if compression != 0 or filter_method != 0 or interlace != 0:
                raise ValueError(f"{path} uses unsupported PNG settings")
        elif chunk_type == b"IDAT":
            idat.extend(data)
        elif chunk_type == b"IEND":
            break

    if width is None or height is None or bit_depth != 8 or color_type not in (2, 6):
        raise ValueError(f"{path} uses unsupported PNG format")

    channels = 4 if color_type == 6 else 3
    stride = width * channels
    decoded = zlib.decompress(bytes(idat))
    canvas = Canvas(width, height)

    prev = bytearray(stride)
    offset = 0
    for y in range(height):
        filter_type = decoded[offset]
        row = bytearray(decoded[offset + 1:offset + 1 + stride])
        offset += 1 + stride
        recon = bytearray(stride)
        for i in range(stride):
            left = recon[i - channels] if i >= channels else 0
            up = prev[i]
            up_left = prev[i - channels] if i >= channels else 0
            value = row[i]
            if filter_type == 0:
                recon[i] = value
            elif filter_type == 1:
                recon[i] = (value + left) & 0xFF
            elif filter_type == 2:
                recon[i] = (value + up) & 0xFF
            elif filter_type == 3:
                recon[i] = (value + ((left + up) // 2)) & 0xFF
            elif filter_type == 4:
                recon[i] = (value + paeth(left, up, up_left)) & 0xFF
            else:
                raise ValueError(f"{path} uses unsupported PNG filter {filter_type}")

        for x in range(width):
            base = x * channels
            red = recon[base]
            green = recon[base + 1]
            blue = recon[base + 2]
            alpha = recon[base + 3] if channels == 4 else 255
            canvas.set(x, y, (red, green, blue, alpha))
        prev = recon

    return canvas


def paeth(left: int, up: int, up_left: int) -> int:
    predictor = left + up - up_left
    dist_left = abs(predictor - left)
    dist_up = abs(predictor - up)
    dist_up_left = abs(predictor - up_left)
    if dist_left <= dist_up and dist_left <= dist_up_left:
        return left
    if dist_up <= dist_up_left:
        return up
    return up_left


def copy_sources() -> None:
    shutil.copyfile(MAIN_SOURCE, MAIN_TARGET_SOURCE)
    shutil.copyfile(MAIN_SOURCE, MAIN_TARGET_SELECTED)
    shutil.copyfile(CONTOUR_SOURCE, CONTOUR_TARGET_SOURCE)
    shutil.copyfile(CONTOUR_SOURCE, CONTOUR_TARGET_SELECTED)
    shutil.copyfile(ACTOR_SOURCE, ACTOR_TARGET_SOURCE)


def color_distance(a: tuple[int, int, int, int], b: tuple[int, int, int, int]) -> int:
    return abs(a[0] - b[0]) + abs(a[1] - b[1]) + abs(a[2] - b[2]) + abs(a[3] - b[3])


def find_components(canvas: Canvas, *, background: tuple[int, int, int, int], tolerance: int, min_area: int) -> list[Component]:
    width = canvas.width
    height = canvas.height
    visited = bytearray(width * height)
    components: list[Component] = []

    def is_foreground(index_x: int, index_y: int) -> bool:
        color = canvas.get(index_x, index_y)
        return color[3] > 0 and color_distance(color, background) > tolerance

    for y in range(height):
        for x in range(width):
            index = y * width + x
            if visited[index] or not is_foreground(x, y):
                continue

            queue = collections.deque([(x, y)])
            visited[index] = 1
            x0 = x1 = x
            y0 = y1 = y
            area = 0
            while queue:
                px, py = queue.popleft()
                area += 1
                if px < x0:
                    x0 = px
                if px > x1:
                    x1 = px
                if py < y0:
                    y0 = py
                if py > y1:
                    y1 = py

                for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    if nx < 0 or ny < 0 or nx >= width or ny >= height:
                        continue
                    neighbor_index = ny * width + nx
                    if visited[neighbor_index] or not is_foreground(nx, ny):
                        continue
                    visited[neighbor_index] = 1
                    queue.append((nx, ny))

            if area >= min_area:
                components.append(Component(x0, y0, x1, y1, area))

    return components


def sort_row_major(components: list[Component], tolerance: int) -> list[Component]:
    rows: list[list[Component]] = []
    for component in sorted(components, key=lambda item: (item.y0, item.x0)):
        for row in rows:
            if abs(component.center_y - row[0].center_y) <= tolerance:
                row.append(component)
                break
        else:
            rows.append([component])

    ordered: list[Component] = []
    for row in sorted(rows, key=lambda items: min(item.y0 for item in items)):
        ordered.extend(sorted(row, key=lambda item: item.x0))
    return ordered


def crop(canvas: Canvas, x0: int, y0: int, x1: int, y1: int) -> Canvas:
    cropped = Canvas(x1 - x0, y1 - y0)
    for y in range(cropped.height):
        for x in range(cropped.width):
            cropped.set(x, y, canvas.get(x0 + x, y0 + y))
    return cropped


def key_out(canvas: Canvas, key_color: tuple[int, int, int, int], tolerance: int) -> Canvas:
    keyed = Canvas(canvas.width, canvas.height)
    for y in range(canvas.height):
        for x in range(canvas.width):
            color = canvas.get(x, y)
            if color_distance(color, key_color) <= tolerance:
                keyed.set(x, y, (0, 0, 0, 0))
            else:
                keyed.set(x, y, color)
    return keyed


def key_out_green(canvas: Canvas) -> Canvas:
    keyed = Canvas(canvas.width, canvas.height)
    for y in range(canvas.height):
        for x in range(canvas.width):
            color = canvas.get(x, y)
            if color[1] >= 220 and color[0] <= 50 and color[2] <= 50:
                keyed.set(x, y, (0, 0, 0, 0))
            else:
                keyed.set(x, y, color)
    return keyed


def crop_component(canvas: Canvas, component: Component, *, pad: int, fill_mode: str) -> Canvas:
    x0 = max(0, component.x0 - pad)
    y0 = max(0, component.y0 - pad)
    x1 = min(canvas.width, component.x1 + pad + 1)
    y1 = min(canvas.height, component.y1 + pad + 1)
    region = crop(canvas, x0, y0, x1, y1)
    size = max(region.width, region.height)
    if fill_mode == "transparent":
        fill = (0, 0, 0, 0)
    else:
        fill = region.get(0, 0)
    square = Canvas(size, size, fill)
    offset_x = (size - region.width) // 2
    offset_y = (size - region.height) // 2
    square.blit(region, offset_x, offset_y)
    return square


def crop_cell_interior(canvas: Canvas, component: Component, inset: int) -> Canvas:
    x0 = component.x0 + inset
    y0 = component.y0 + inset
    x1 = component.x1 - inset + 1
    y1 = component.y1 - inset + 1
    return crop(canvas, x0, y0, x1, y1)


def scale_nearest(source: Canvas, width: int, height: int) -> Canvas:
    scaled = Canvas(width, height)
    for y in range(height):
        source_y = min(source.height - 1, int(y * source.height / height))
        for x in range(width):
            source_x = min(source.width - 1, int(x * source.width / width))
            scaled.set(x, y, source.get(source_x, source_y))
    return scaled


def export_main_tiles() -> None:
    canvas = read_png(MAIN_TARGET_SELECTED)
    background = canvas.get(0, 0)
    components = sort_row_major(find_components(canvas, background=background, tolerance=12, min_area=4000), tolerance=80)
    if len(components) != 13:
        raise ValueError(f"Expected 13 components in main sheet, found {len(components)}")

    mapping = [
        ("tile_floor_cave.png", 0),
        ("tile_wall_soil.png", 1),
        ("tile_wall_stone.png", 2),
        ("tile_wall_hard_rock.png", 3),
        ("tile_wall_ultra_hard.png", 4),
        ("tile_boundary.png", 5),
        ("tile_overlay_danger.png", 6),
        ("tile_overlay_marker.png", 7),
        ("tile_hint_scan.png", 8),
        ("tile_facility_repair_station.png", 9),
        ("tile_facility_robot_factory.png", 10),
    ]

    for filename, index in mapping:
        square = crop_component(canvas, components[index], pad=10, fill_mode="background")
        output = scale_nearest(square, 16, 16)
        write_png(os.path.join(SPRITE_TILE_DIR, filename), output)


def export_detail_and_build_tiles() -> None:
    canvas = read_png(CONTOUR_TARGET_SELECTED)
    detail_mapping = [
        ("tile_detail_soil.png", 4, 0),
        ("tile_detail_stone.png", 4, 1),
        ("tile_detail_hard_rock.png", 4, 2),
        ("tile_detail_ultra_hard.png", 4, 3),
        ("tile_build_preview_valid.png", 5, 0),
        ("tile_build_preview_invalid.png", 5, 1),
    ]

    for filename, row, column in detail_mapping:
        x0 = CONTOUR_GRID_X[column]
        y0 = CONTOUR_GRID_Y[row]
        cell = crop(canvas, x0 + 12, y0 + 12, x0 + CONTOUR_CELL_WIDTH - 12, y0 + CONTOUR_CELL_HEIGHT - 12)
        output = scale_nearest(cell, 16, 16)
        write_png(os.path.join(SPRITE_TILE_DIR, filename), output)


def export_actor_sprites() -> None:
    source = read_png(ACTOR_SOURCE)
    canvas = key_out_green(source)
    components = sort_row_major(find_components(canvas, background=(0, 0, 0, 0), tolerance=0, min_area=500), tolerance=100)
    if len(components) != 2:
        raise ValueError(f"Expected 2 actor sprites, found {len(components)}")

    mapping = [
        ("actor_player_minebot.png", 0),
        ("actor_helper_robot.png", 1),
    ]
    for filename, index in mapping:
        square = crop_component(canvas, components[index], pad=8, fill_mode="transparent")
        output = scale_nearest(square, 32, 32)
        write_png(os.path.join(SPRITE_ACTOR_DIR, filename), output)


def main() -> None:
    ensure_dirs()
    copy_sources()
    export_main_tiles()
    export_detail_and_build_tiles()
    export_actor_sprites()


if __name__ == "__main__":
    main()
