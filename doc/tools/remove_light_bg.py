#!/usr/bin/env python3
"""Remove flat light-gray / off-white background → transparent PNG.

Usage:
  python remove_light_bg.py input.png
  python remove_light_bg.py input.png -o output.png
  python remove_light_bg.py input.png -o out.png --size 512
  python remove_light_bg.py a.png b.png --outdir ../out --size 512

Only keys flat light backgrounds (flood-fill from borders). Does not eat
dark hull pixels. Optionally tight-crops and fits into a square canvas.
"""

from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def is_light_bg(r: int, g: int, b: int, a: int) -> bool:
    if a < 8:
        return True
    mx, mn = max(r, g, b), min(r, g, b)
    # flat light gray / off-white
    return mn >= 175 and (mx - mn) <= 25


def is_grayish_fringe(r: int, g: int, b: int, a: int) -> bool:
    if a < 8:
        return False
    mx, mn = max(r, g, b), min(r, g, b)
    return mn >= 165 and (mx - mn) <= 30


def remove_light_background(img: Image.Image) -> Image.Image:
    """Flood-fill light gray from borders to alpha=0. Keeps dark content intact."""
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()

    visited = [[False] * w for _ in range(h)]
    q: deque[tuple[int, int]] = deque()

    for x in range(w):
        for y in (0, h - 1):
            r, g, b, a = px[x, y]
            if is_light_bg(r, g, b, a) and not visited[y][x]:
                q.append((x, y))
                visited[y][x] = True
    for y in range(h):
        for x in (0, w - 1):
            if visited[y][x]:
                continue
            r, g, b, a = px[x, y]
            if is_light_bg(r, g, b, a):
                q.append((x, y))
                visited[y][x] = True

    while q:
        x, y = q.popleft()
        px[x, y] = (0, 0, 0, 0)
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and not visited[ny][nx]:
                r, g, b, a = px[nx, ny]
                if is_light_bg(r, g, b, a):
                    visited[ny][nx] = True
                    q.append((nx, ny))

    # light-gray fringe touching already-transparent pixels
    q = deque()
    seen: set[tuple[int, int]] = set()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if not is_grayish_fringe(r, g, b, a):
                continue
            for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if 0 <= nx < w and 0 <= ny < h and px[nx, ny][3] <= 8:
                    q.append((x, y))
                    seen.add((x, y))
                    break
    while q:
        x, y = q.popleft()
        px[x, y] = (0, 0, 0, 0)
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen:
                r, g, b, a = px[nx, ny]
                if is_grayish_fringe(r, g, b, a):
                    seen.add((nx, ny))
                    q.append((nx, ny))

    return img


def content_bbox(img: Image.Image, pad: int = 8) -> tuple[int, int, int, int]:
    w, h = img.size
    px = img.load()
    min_x, min_y, max_x, max_y = w, h, 0, 0
    found = False
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 8:
                found = True
                if x < min_x:
                    min_x = x
                if y < min_y:
                    min_y = y
                if x > max_x:
                    max_x = x
                if y > max_y:
                    max_y = y
    if not found:
        return 0, 0, w, h
    min_x = max(0, min_x - pad)
    min_y = max(0, min_y - pad)
    max_x = min(w - 1, max_x + pad)
    max_y = min(h - 1, max_y + pad)
    return min_x, min_y, max_x + 1, max_y + 1


def fit_square(img: Image.Image, size: int) -> Image.Image:
    """Contain-fit into size×size transparent canvas (no cover-crop)."""
    box = content_bbox(img)
    cropped = img.crop(box)
    cw, ch = cropped.size
    scale = min(size / cw, size / ch)
    nw = max(1, int(round(cw * scale)))
    nh = max(1, int(round(ch * scale)))
    scaled = cropped.resize((nw, nh), Image.Resampling.LANCZOS)
    sp = scaled.load()
    for y in range(nh):
        for x in range(nw):
            if sp[x, y][3] < 12:
                sp[x, y] = (0, 0, 0, 0)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(scaled, ((size - nw) // 2, (size - nh) // 2), scaled)
    return out


def process_file(src: Path, dst: Path, size: int | None) -> None:
    img = Image.open(src)
    img = remove_light_background(img)
    if size is not None and size > 0:
        img = fit_square(img, size)
    dst.parent.mkdir(parents=True, exist_ok=True)
    img.save(dst, "PNG")
    print(f"{src.name} → {dst} ({img.size[0]}x{img.size[1]})")


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Remove light-gray background to transparent PNG."
    )
    parser.add_argument("inputs", nargs="+", type=Path, help="Input image path(s)")
    parser.add_argument("-o", "--output", type=Path, help="Output path (single input only)")
    parser.add_argument(
        "--outdir",
        type=Path,
        help="Output directory (batch). Default: same folder as each input.",
    )
    parser.add_argument(
        "--size",
        type=int,
        default=512,
        help="Square canvas size after contain-fit (default 512). Use 0 to skip resize.",
    )
    parser.add_argument(
        "--suffix",
        default="_transparent",
        help="Filename suffix when writing beside input (batch / no -o). Default: _transparent",
    )
    args = parser.parse_args()

    size = None if args.size == 0 else args.size

    if args.output is not None:
        if len(args.inputs) != 1:
            parser.error("-o/--output only works with a single input")
        process_file(args.inputs[0], args.output, size)
        return

    for src in args.inputs:
        if args.outdir is not None:
            dst = args.outdir / (src.stem + ".png")
        else:
            dst = src.with_name(src.stem + args.suffix + ".png")
        process_file(src, dst, size)


if __name__ == "__main__":
    main()
