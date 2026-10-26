# UI icons

Run `powershell -NoProfile -File tools/Generate-IconAtlases.ps1` from the repository root once when
the source icons change. This Windows script uses System.Drawing and generates
the PNG atlases and `Icons.cs`; commit these outputs. Icon groups stay together
as nested classes in this generated file. It is not run
during game startup. Open the project in Godot to import regenerated textures
before running or exporting the game.

The individual PNGs under `assets/ui/icons/1-bit_Pixel_Icons/Sprites` are the
source of truth. Category names come from the `Icons_*.png` sheet filenames,
including multiword categories such as `MapMarkers` and `ToolsCrafting`.
Underscores are removed from item names to create C# properties. Names starting
with a digit receive a leading underscore, for example `Software._3DSMax`.

```csharp
var definition = Icons.Alchemy.CancelDecline;
var button = BaseFactory.CreateClickableIcon(definition);
var texture = Icons.Travel.AirBalloon.IconTexture;
```

`Icons` is the public static API for named icons. `IconDefinition` is the sealed
value holder returned by its properties. The sealed
`IconDefinitions` autoload and `IconAtlasProvider` are internal implementation
details; there is no public provider, raw atlas accessor or indexed icon lookup.
`IconDefinitions` is the first autoload and derives from `AbstractSingleton`.
It loads all atlases through `IconAtlasProvider` and creates every `IconDefinition`
and `AtlasTexture` during tree entry, before other autoloads and the main scene.
Properties return the same cached definitions through array indexing with no
resource loading or allocation. The singleton retains the resources across scene
changes for the lifetime of the game. Definitions use the existing medium size
default; create a new `IconDefinition` sharing `IconTexture` for a different size.

The current 1,476 sprites fit on one 1024×1024 RGBA atlas (4 MiB uncompressed).
Further icons automatically spill into additional atlases. Twenty-pixel cells
preserve the two sprites with a 17-pixel dimension, with one pixel of extruded
edge padding. Imports are lossless, without mipmaps or alpha-border correction.
Use nearest texture filtering on pixel-art UI controls. Shared atlas textures
allow Godot to batch compatible consecutive UI draws; materials, clipping and
draw order can still split batches.

Do not edit the generated C# files. Regeneration sorts filenames and checks for
identifier collisions and oversized sprites before writing output. Original
sprites and category sheets are retained for editing and are not loaded by the
runtime provider.
