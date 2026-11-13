"""Pack numbered PNG frames into a Godot-ready sprite sheet.

Install Pillow with: python -m pip install Pillow
Example: python tools/vfx_atlas/assemble_vfx_atlas.py path/to/laser_impact_burst --columns 6
Frames are ordered numerically, left to right and then top to bottom.
Source images are retained. Transparent frames are kept unless explicitly skipped.
"""

import argparse
from io import BytesIO
from math import isqrt
from pathlib import Path
import re


def positive_integer(value: str) -> int:
    number = int(value)
    if number < 1:
        raise argparse.ArgumentTypeError('Must be a positive integer.')
    return number


def frame_order(path: Path) -> tuple:
    parts = re.split(r'(\d+)', path.name.casefold())
    return tuple(int(part) if part.isdigit() else part for part in parts), path.name


def main() -> None:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('folder', type=Path, help='Folder containing the source frames.')
    parser.add_argument('--pattern', default='frame*.png',
                        help='Source filename pattern (default: frame*.png).')
    parser.add_argument('--columns', type=positive_integer,
                        help='Grid columns; defaults to the ceiling of sqrt(frame count).')
    parser.add_argument('--skip-transparent', action='store_true',
                        help='Omit fully transparent frames, shortening the animation.')
    parser.add_argument('--force', action='store_true',
                        help='Overwrite an existing atlas named after the folder.')
    args = parser.parse_args()

    try:
        from PIL import Image
    except ModuleNotFoundError:
        parser.error('Pillow is required. Install it with: python -m pip install Pillow')

    folder = args.folder.resolve()
    if not folder.is_dir():
        parser.error(f'Source folder does not exist: {folder}')
    output = folder / f'{folder.name}.png'
    if output.exists() and not args.force:
        parser.error(f'Atlas already exists: {output}. Use --force to replace it.')

    try:
        paths = []
        for path in folder.glob(args.pattern):
            if path.is_file() and path.suffix.lower() == '.png' and path.resolve() != output:
                paths.append(path)
        paths.sort(key=frame_order)
        if not paths:
            raise ValueError(f'No source PNGs match {args.pattern!r} in {folder}.')

        frames = []
        skipped = []
        frame_size = None
        for path in paths:
            with Image.open(path) as source:
                frame = source.convert('RGBA')
            if frame_size is None:
                frame_size = frame.size
            elif frame.size != frame_size:
                raise ValueError(f'{path.name} is {frame.size}; expected {frame_size}.')
            if args.skip_transparent and frame.getchannel('A').getbbox() is None:
                skipped.append(path.name)
                continue
            frames.append((path.name, frame))
        if not frames:
            raise ValueError('No visible frames remain after skipping transparent frames.')

        columns = args.columns or isqrt(len(frames) - 1) + 1
        rows = (len(frames) + columns - 1) // columns
        width, height = frame_size
        atlas = Image.new('RGBA', (columns * width, rows * height), (0, 0, 0, 0))
        for index, (_, frame) in enumerate(frames):
            atlas.paste(frame, ((index % columns) * width, (index // columns) * height))

        encoded = BytesIO()
        atlas.save(encoded, format='PNG', optimize=True)
        encoded.seek(0)
        with Image.open(encoded) as saved:
            for index, (name, frame) in enumerate(frames):
                x, y = (index % columns) * width, (index // columns) * height
                if saved.crop((x, y, x + width, y + height)).tobytes() != frame.tobytes():
                    raise ValueError(f'Atlas pixel verification failed for {name}.')
        with output.open('wb' if args.force else 'xb') as destination:
            destination.write(encoded.getvalue())
    except (OSError, ValueError) as error:
        parser.error(str(error))

    print(f'Created {output}')
    print(f'Atlas: {atlas.width}x{atlas.height}; grid: {columns}x{rows}; frame: {width}x{height}')
    print(f'Frames: {len(frames)}; unused trailing cells: {columns * rows - len(frames)}')
    print('Select only the populated cells in Godot, left to right and top to bottom.')
    print('Frame order: ' + ', '.join(name for name, _ in frames))
    if skipped:
        print('Skipped transparent frames: ' + ', '.join(skipped))
    print('Pixel verification passed. Source PNGs were retained.')


if __name__ == '__main__':
    main()
