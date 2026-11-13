# VFX atlas assembler

Combine numbered PNG animation frames into one transparent sprite sheet for Godot.
The tool preserves each frame's full canvas and RGBA pixels, sorts filenames
numerically, and verifies the encoded atlas before writing it. Source PNGs are
retained.

## Setup

Install Python 3 and Pillow:

```powershell
python -m pip install Pillow
```

## Usage

Run these commands from the repository root. Replace `path/to/effect` with a folder
containing the original animation frames, all with the same width and height.

```powershell
python tools/vfx_atlas/assemble_vfx_atlas.py path/to/effect --columns 6
```

By default, the tool reads `frame*.png` and writes `path/to/effect/effect.png`.
The output filename comes from the folder name and is reserved for the atlas.
Relative input paths are resolved from your current working directory.

Frames are placed left to right, then top to bottom. Numeric sorting puts
`frame2.png` before `frame10.png`; zero-padded filenames work too.

| Option | Behavior |
| --- | --- |
| `--columns N` | Set a positive column count. Without it, use the ceiling of the square root of the frame count. |
| `--pattern "*.png"` | Change the source filename pattern. The output atlas is excluded from inputs. |
| `--skip-transparent` | Omit fully transparent frames. This shortens playback if all frame durations remain equal. |
| `--force` | Replace an existing atlas. Otherwise, existing output is left untouched and the command stops. |
| `--help` | Show usage information. |

For differently named source frames:

```powershell
python tools/vfx_atlas/assemble_vfx_atlas.py path/to/metal_impact --pattern "metal_impact_*.png" --columns 3
```

To rebuild an existing atlas while omitting transparent frames:

```powershell
python tools/vfx_atlas/assemble_vfx_atlas.py path/to/effect --columns 6 --skip-transparent --force
```

Rebuilding requires the source frame PNGs. This tool does not extract frames from
an existing atlas, delete source files, or modify Godot scenes or project files.

## Use the atlas in Godot

1. Add an `AnimatedSprite2D` and create a `SpriteFrames` resource.
2. Choose **Add frames from a sprite sheet** and select the generated PNG.
3. Set the horizontal and vertical counts to the grid dimensions printed by the tool.
4. Select the populated cells in order, left to right and top to bottom.
5. Leave unused trailing cells unselected, then configure animation speed and looping.

For example, 17 frames of 96×96 pixels with `--columns 6` produce a 576×288 atlas
with a 6×3 grid. Select the first 17 cells; the last cell is empty. Without an
explicit column count, the same frames use a 5×4 grid.

For pixel-art effects, use nearest texture filtering in Godot. Keep a consistent
sprite offset across animation frames so the effect stays aligned.
