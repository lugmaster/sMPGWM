param(
    [string] $GameDirectory = (Join-Path $PSScriptRoot '../sMPGWM.Game')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

$gameRoot = (Resolve-Path -LiteralPath $GameDirectory).Path
$sourceDirectory = Join-Path $gameRoot 'assets/ui/icons/1-bit_Pixel_Icons'
$outputDirectory = Join-Path $gameRoot 'assets/ui/icons/atlases'
$accessorPath = Join-Path $gameRoot 'Scripts/Util/UiComponents/Icons.cs'
$atlasSize = 1024
$cellSize = 20
$padding = 1
$columns = [int][Math]::Floor($atlasSize / $cellSize)
$capacity = $columns * $columns

function ConvertTo-Identifier([string] $value) {
    $identifier = (($value -split '[^A-Za-z0-9]+' | Where-Object { $_ } | ForEach-Object {
        $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
    }) -join '')
    if ($identifier -match '^[0-9]') {
        $identifier = '_' + $identifier
    }
    if ($identifier -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "Invalid C# identifier: $value"
    }
    return $identifier
}

$domains = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter 'Icons_*.png' -File |
    ForEach-Object { $_.BaseName.Substring('Icons_'.Length) } |
    Sort-Object -Property @{ Expression = { $_.Length }; Descending = $true }, @{ Expression = { $_ } })
$sprites = @(Get-ChildItem -LiteralPath (Join-Path $sourceDirectory 'Sprites') -Filter '*.png' -File |
    Sort-Object -Property Name)
if ($sprites.Count -eq 0) {
    throw 'No icon sprites found.'
}

$entries = [System.Collections.Generic.List[object]]::new()
$identifiers = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($sprite in $sprites) {
    $domain = $domains | Where-Object { $sprite.BaseName.StartsWith($_ + '_', [System.StringComparison]::Ordinal) } |
        Select-Object -First 1
    if (-not $domain) {
        throw "No domain sheet found for $($sprite.Name)."
    }
    $domainName = ConvertTo-Identifier $domain
    $iconName = ConvertTo-Identifier $sprite.BaseName.Substring($domain.Length + 1)
    if ($domainName -eq $iconName -or -not $identifiers.Add("$domainName.$iconName")) {
        throw "Conflicting C# icon identifier: $domainName.$iconName"
    }
    $bitmap = [System.Drawing.Bitmap]::new($sprite.FullName)
    try {
        if ($bitmap.Width + 2 * $padding -gt $cellSize -or $bitmap.Height + 2 * $padding -gt $cellSize) {
            throw "Sprite $($sprite.Name) exceeds the $cellSize pixel cell size including padding."
        }
        $entries.Add([pscustomobject]@{
            Source = $sprite.FullName
            Domain = $domainName
            Name = $iconName
            Index = $entries.Count
            Atlas = [int][Math]::Floor($entries.Count / $capacity)
            X = ($entries.Count % $capacity % $columns) * $cellSize + $padding
            Y = [int][Math]::Floor(($entries.Count % $capacity) / $columns) * $cellSize + $padding
            Width = $bitmap.Width
            Height = $bitmap.Height
        })
    }
    finally {
        $bitmap.Dispose()
    }
}

[void][System.IO.Directory]::CreateDirectory($outputDirectory)
$atlasCount = [int][Math]::Ceiling($entries.Count / $capacity)
for ($atlasIndex = 0; $atlasIndex -lt $atlasCount; $atlasIndex++) {
    $atlas = [System.Drawing.Bitmap]::new($atlasSize, $atlasSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($atlas)
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        foreach ($entry in ($entries | Where-Object { $_.Atlas -eq $atlasIndex })) {
            $bitmap = [System.Drawing.Bitmap]::new($entry.Source)
            try {
                $graphics.DrawImageUnscaled($bitmap, $entry.X, $entry.Y)
                for ($offset = -1; $offset -le $entry.Width; $offset++) {
                    $sourceX = [Math]::Max(0, [Math]::Min($offset, $entry.Width - 1))
                    $atlas.SetPixel($entry.X + $offset, $entry.Y - 1, $bitmap.GetPixel($sourceX, 0))
                    $atlas.SetPixel($entry.X + $offset, $entry.Y + $entry.Height, $bitmap.GetPixel($sourceX, $entry.Height - 1))
                }
                for ($offset = 0; $offset -lt $entry.Height; $offset++) {
                    $atlas.SetPixel($entry.X - 1, $entry.Y + $offset, $bitmap.GetPixel(0, $offset))
                    $atlas.SetPixel($entry.X + $entry.Width, $entry.Y + $offset, $bitmap.GetPixel($entry.Width - 1, $offset))
                }
            }
            finally {
                $bitmap.Dispose()
            }
        }
        $atlas.Save((Join-Path $outputDirectory "icons_$atlasIndex.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $atlas.Dispose()
    }

    $resourcePath = "res://assets/ui/icons/atlases/icons_$atlasIndex.png"
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $pathHash = ([System.BitConverter]::ToString($md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($resourcePath)))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $md5.Dispose()
    }
    $importedPath = "res://.godot/imported/icons_$atlasIndex.png-$pathHash.ctex"
    $importSettings = @"
[remap]

importer="texture"
type="CompressedTexture2D"
path="$importedPath"
metadata={
"vram_texture": false
}

[deps]

source_file="$resourcePath"
dest_files=["$importedPath"]

[params]

compress/mode=0
mipmaps/generate=false
process/fix_alpha_border=false
"@
    $importPath = Join-Path $outputDirectory "icons_$atlasIndex.png.import"
    if (-not (Test-Path -LiteralPath $importPath)) {
        [System.IO.File]::WriteAllText($importPath, $importSettings.Replace("`r`n", "`n") + "`n")
    }
}

$code = [System.Text.StringBuilder]::new()
[void]$code.AppendLine('namespace sMPGWM.Scripts.Util.UiComponents;')
[void]$code.AppendLine()
[void]$code.AppendLine('public static class Icons')
[void]$code.AppendLine('{')
[void]$code.AppendLine('    internal static IconDefinition[] LoadDefinitions(out IconAtlasProvider atlasProvider)')
[void]$code.AppendLine('    {')
[void]$code.AppendLine('        atlasProvider = new IconAtlasProvider(')
for ($atlasIndex = 0; $atlasIndex -lt $atlasCount; $atlasIndex++) {
    $suffix = if ($atlasIndex -eq $atlasCount - 1) { ');' } else { ',' }
    [void]$code.AppendLine("            `"res://assets/ui/icons/atlases/icons_$atlasIndex.png`"$suffix")
}
[void]$code.AppendLine('        return')
[void]$code.AppendLine('        [')
foreach ($entry in $entries) {
    [void]$code.AppendLine("            atlasProvider.CreateIcon($($entry.Atlas), $($entry.X), $($entry.Y), $($entry.Width), $($entry.Height)),")
}
[void]$code.AppendLine('        ];')
[void]$code.AppendLine('    }')
foreach ($group in ($entries | Group-Object Domain | Sort-Object Name)) {
    [void]$code.AppendLine()
    [void]$code.AppendLine("    public static class $($group.Name)")
    [void]$code.AppendLine('    {')
    foreach ($entry in $group.Group) {
        [void]$code.AppendLine("        public static IconDefinition $($entry.Name) => IconDefinitions.GetIcon($($entry.Index));")
    }
    [void]$code.AppendLine('    }')
}
[void]$code.AppendLine('}')
[System.IO.File]::WriteAllText($accessorPath, $code.ToString().Replace("`r`n", "`n"))
Write-Output "Generated $($entries.Count) icon definitions in $atlasCount ${atlasSize}x${atlasSize} atlas(es)."
