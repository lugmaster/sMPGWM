"""Bake a tight circle or ellipse around a ship PNG's nontransparent pixels.

Install: python -m pip install -r tools/shield_geometry/requirements.txt
Check:   python tools/shield_geometry/bake_shield_geometry.py --check
Write:   python tools/shield_geometry/bake_shield_geometry.py --write
Select:  add --ships path/to/Ships.yml --ship fighter --alpha-threshold 1

The default is read-only checking. Pixel cell corners use centered Sprite2D
coordinates. A normalized Khachiyan fit stops at a 1e-5 relative dual residual.
The tighter ellipse/circle candidate is rounded to one decimal place. Radii grow
in 0.1-pixel steps only when needed for containment, including float32 conversion.
This is a numerical minimum, not an exact symbolic optimum. See Todd/Yildirim:
https://people.orie.cornell.edu/miketodd/TYKhach.pdf

Only shield_geometry YAML blocks are replaced. PNGs, hull polygons and other
ship fields remain unchanged. All selected fits succeed before any write.
"""

from __future__ import annotations

import argparse
import math
import os
from pathlib import Path
import random
import sys
import tempfile

try:
    import numpy as np
    from PIL import Image
    import yaml
except ModuleNotFoundError as error:
    if __name__ == "__main__":
        print(f"Missing dependency: {error.name}. Install with: python -m pip install -r "
              "tools/shield_geometry/requirements.txt", file=sys.stderr)
        raise SystemExit(2) from error
    raise


def convex_hull(points):
    """Return a deterministic counterclockwise convex hull without duplicates."""
    ordered = sorted({(float(point[0]), float(point[1])) for point in points})
    if not ordered or any(not math.isfinite(value) for point in ordered for value in point):
        raise ValueError("Geometry needs finite, nonempty points.")

    def cross(a, b, c):
        return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])

    lower = []
    for point in ordered:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], point) <= 0:
            lower.pop()
        lower.append(point)
    upper = []
    for point in reversed(ordered):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], point) <= 0:
            upper.pop()
        upper.append(point)
    return lower[:-1] + upper[:-1] if len(ordered) > 1 else ordered


def silhouette_hull(image, alpha_threshold=1):
    """Enclose retained pixel cells, including their corners, not just centers."""
    if not isinstance(alpha_threshold, int) or not 1 <= alpha_threshold <= 255:
        raise ValueError("alpha_threshold must be an integer from 1 through 255.")
    alpha = np.asarray(image.convert("RGBA"))[:, :, 3]
    height, width = alpha.shape
    points = []
    for y in range(height):
        occupied = np.flatnonzero(alpha[y] >= alpha_threshold)
        if occupied.size:
            left = float(occupied[0]) - width / 2
            right = float(occupied[-1] + 1) - width / 2
            top = y - height / 2
            points.extend(((left, top), (right, top), (left, top + 1), (right, top + 1)))
    if not points:
        raise ValueError("PNG has no pixels at or above the alpha threshold.")
    return convex_hull(points)


def _quadratic_values(geometry, points, float32=False):
    dtype = np.float32 if float32 else np.float64
    center = np.asarray(geometry["center"], dtype=dtype).astype(np.float64)
    offset = np.asarray(points, dtype=np.float64) - center
    if geometry["shape"] == "circle":
        radius = float(dtype(geometry["radius"]))
        if not math.isfinite(radius) or radius <= 0:
            raise ValueError("Circle radius must be finite and positive.")
        return np.sum(offset * offset, axis=1) / (radius * radius)
    if geometry["shape"] != "ellipse":
        raise ValueError("Unknown shield shape.")
    radii = np.asarray(geometry["radii"], dtype=dtype).astype(np.float64)
    if not np.all(np.isfinite(radii)) or np.any(radii <= 0):
        raise ValueError("Ellipse radii must be finite and positive.")
    angle = float(dtype(dtype(geometry["rotation_degrees"]) * dtype(math.pi / 180)))
    cosine, sine = math.cos(angle), math.sin(angle)
    x = offset[:, 0] * cosine + offset[:, 1] * sine
    y = -offset[:, 0] * sine + offset[:, 1] * cosine
    return (x / radii[0]) ** 2 + (y / radii[1]) ** 2


def contains_points(geometry, points, tolerance=0.0):
    """Check containment using both serialized and runtime float32 parameters."""
    if not math.isfinite(tolerance) or tolerance < 0:
        raise ValueError("Containment tolerance must be finite and nonnegative.")
    values = np.asarray(points, dtype=np.float64)
    if values.size == 0:
        return True
    if values.ndim != 2 or values.shape[1] != 2 or not np.all(np.isfinite(values)):
        raise ValueError("Containment needs finite [x,y] points.")
    return bool(np.all(_quadratic_values(geometry, values) <= 1 + tolerance) and
                np.all(_quadratic_values(geometry, values, True) <= 1 + tolerance))


def _safe_geometry(shape, center, radii, angle, points):
    def rounded(value):
        value = round(float(value), 1)
        return value if value != 0 else 0.0

    geometry = {"shape": shape, "center": [rounded(value) for value in center]}
    if shape == "circle":
        geometry["radius"] = max(0.1, rounded(radii[0]))
    else:
        geometry["radii"] = [max(0.1, rounded(value)) for value in radii]
        geometry["rotation_degrees"] = rounded(float(angle) % 180) % 180
        minimum_axes = [int(round(value * 10)) for value in geometry["radii"]]

    def boundary_value(candidate):
        return max(float(np.max(_quadratic_values(candidate, points))),
                   float(np.max(_quadratic_values(candidate, points, True))))

    for _ in range(10000):
        if contains_points(geometry, points):
            break
        if shape == "circle":
            geometry["radius"] = rounded(geometry["radius"] + 0.1)
            continue
        candidates = []
        for axis in range(2):
            axes = geometry["radii"].copy()
            axes[axis] = rounded(axes[axis] + 0.1)
            if axes[0] < axes[1]:
                continue
            candidate = {**geometry, "radii": axes}
            violation = boundary_value(candidate)
            candidates.append((max(1.0, violation), math.prod(axes), axis, candidate))
        geometry = min(candidates, key=lambda candidate: candidate[:3])[3]
    else:
        raise ValueError("Rounded shield geometry could not enclose the silhouette within 10000 radius increments.")
    if shape == "circle":
        return geometry

    # Search the tenth-pixel grid for the least added area at the rounded center/angle.
    best_area = math.prod(int(round(value * 10)) for value in geometry["radii"])
    for major in range(minimum_axes[0], best_area // minimum_axes[1] + 1):
        if major * minimum_axes[1] >= best_area:
            break
        low, high = minimum_axes[1], min(major, (best_area - 1) // major)
        candidate = {**geometry, "radii": [major / 10, high / 10]}
        if not contains_points(candidate, points):
            continue
        while low < high:
            middle = (low + high) // 2
            candidate["radii"][1] = middle / 10
            if contains_points(candidate, points):
                high = middle
            else:
                low = middle + 1
        candidate["radii"][1] = low / 10
        best_area = major * low
        geometry = candidate
    return geometry


def _minimum_circle(points):
    """Incremental enclosing-circle construction with a reproducible order."""
    ordered = [np.array(point, dtype=np.float64) for point in points]
    random.Random(0).shuffle(ordered)
    center = ordered[0].copy()
    radius_squared = 0.0

    def outside(point):
        return float(np.dot(point - center, point - center)) > radius_squared * (1 + 1e-13) + 1e-15

    for i, first in enumerate(ordered):
        if not outside(first):
            continue
        center, radius_squared = first.copy(), 0.0
        for j in range(i):
            second = ordered[j]
            if not outside(second):
                continue
            center = (first + second) / 2
            radius_squared = float(np.dot(first - center, first - center))
            for k in range(j):
                third = ordered[k]
                if not outside(third):
                    continue
                a, b = second - first, third - first
                determinant = float(a[0] * b[1] - a[1] * b[0])
                if abs(determinant) <= 1e-15:
                    pairs = ((first, second), (first, third), (second, third))
                    left, right = max(pairs, key=lambda pair: float(np.dot(pair[1] - pair[0], pair[1] - pair[0])))
                    center = (left + right) / 2
                else:
                    aa, bb = float(np.dot(a, a)), float(np.dot(b, b))
                    center = first + np.array(((aa * b[1] - bb * a[1]) / (2 * determinant),
                                               (a[0] * bb - b[0] * aa) / (2 * determinant)))
                radius_squared = float(np.dot(first - center, first - center))
    radius_squared = max(float(np.dot(point - center, point - center)) for point in ordered)
    return center, math.sqrt(radius_squared)


def fit_shield_geometry(points, tolerance=1e-5, max_iterations=1000000):
    """Return a validated serialized circle/ellipse for finite 2D points."""
    if not math.isfinite(tolerance) or not 0 < tolerance < 1 or max_iterations < 1:
        raise ValueError("Fit tolerance and iteration limit must be positive and bounded.")
    hull = np.asarray(convex_hull(points), dtype=np.float64)
    if len(hull) < 3:
        raise ValueError("Shield fitting needs a nondegenerate two-dimensional silhouette.")
    origin = (np.min(hull, axis=0) + np.max(hull, axis=0)) / 2
    scale = float(np.max(np.ptp(hull, axis=0)))
    normalized = (hull - origin) / scale
    q = np.vstack((normalized.T, np.ones(len(hull))))
    weights = np.full(len(hull), 1.0 / len(hull))
    converged = False
    try:
        for _ in range(max_iterations):
            moment = (q * weights) @ q.T
            distances = np.sum(q * np.linalg.solve(moment, q), axis=0)
            index = int(np.argmax(distances))
            maximum = float(distances[index])
            if maximum <= 3 * (1 + tolerance):
                converged = True
                break
            step = (maximum - 3) / (3 * (maximum - 1))
            weights *= 1 - step
            weights[index] += step
        if not converged:
            raise ValueError(f"Minimum ellipse did not converge within {max_iterations} iterations.")
        center = normalized.T @ weights
        covariance = (normalized.T * weights) @ normalized - np.outer(center, center)
        eigenvalues, eigenvectors = np.linalg.eigh(covariance)
        if not np.all(np.isfinite(eigenvalues)) or eigenvalues[0] <= 0:
            raise ValueError("Shield silhouette has degenerate covariance.")
    except np.linalg.LinAlgError as error:
        raise ValueError("Shield silhouette cannot produce a stable ellipse.") from error
    radii = np.sqrt(2 * eigenvalues[::-1]) * scale
    axis = eigenvectors[:, -1]
    angle = math.degrees(math.atan2(float(axis[1]), float(axis[0]))) % 180
    circle_center, circle_radius = _minimum_circle(normalized)
    ellipse = {"shape": "ellipse", "center": center * scale + origin,
               "radii": radii, "rotation_degrees": angle}
    ellipse_area = math.prod(radii) * max(1.0, float(np.max(_quadratic_values(ellipse, hull))))
    if (circle_radius * scale) ** 2 <= ellipse_area * (1 + 1e-12):
        return _safe_geometry("circle", circle_center * scale + origin, [circle_radius * scale], 0, hull)
    return _safe_geometry("ellipse", center * scale + origin, radii, angle, hull)


class _UniqueLoader(yaml.SafeLoader):
    pass


def _mapping(loader, node, deep=False):
    result = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in result:
            raise ValueError(f"Duplicate YAML key: {key}")
        result[key] = loader.construct_object(value_node, deep=deep)
    return result


_UniqueLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, _mapping)


def _field(node, key):
    if not isinstance(node, yaml.MappingNode):
        raise ValueError("Expected a YAML mapping.")
    for key_node, value_node in node.value:
        if key_node.value == key:
            return key_node, value_node
    return None


def _project_root(ships_path, explicit):
    if explicit:
        return explicit.resolve()
    for directory in ships_path.parents:
        if (directory / "project.godot").is_file():
            return directory
    return ships_path.parent


def _png_path(texture_path, project_root):
    if not isinstance(texture_path, str) or not texture_path.startswith("res://"):
        raise ValueError("texture_path must use res:// and point to a PNG.")
    path = (project_root / texture_path[6:]).resolve()
    if not path.is_relative_to(project_root) or path.suffix.lower() != ".png":
        raise ValueError("texture_path must identify a PNG inside the project.")
    return path


def _format_block(geometry, indent, newline):
    lines = [" " * indent + "shield_geometry:"]
    for key, value in geometry.items():
        if isinstance(value, list):
            text = "[" + ", ".join(str(number) for number in value) + "]"
        else:
            text = str(value)
        lines.append(" " * (indent + 2) + key + ": " + text)
    return newline.join(lines) + newline


def _last_value_end(node):
    if isinstance(node, yaml.MappingNode) and node.value:
        return max(_last_value_end(value) for _, value in node.value)
    if isinstance(node, yaml.SequenceNode) and node.value:
        return max(_last_value_end(value) for value in node.value)
    return node.end_mark.index


def _updated_yaml(source, root_node, updates):
    ships_field = _field(root_node, "ships")
    if ships_field is None or not isinstance(ships_field[1], yaml.SequenceNode):
        raise ValueError("Ships.yml must contain a ships sequence.")
    newline = "\r\n" if "\r\n" in source else "\n"
    edits = []
    for node in ships_field[1].value:
        name_field = _field(node, "name")
        if name_field is None or name_field[1].value not in updates:
            continue
        if node.flow_style:
            raise ValueError("Ship mappings must use block YAML for a targeted update.")
        geometry = updates[name_field[1].value]
        existing = _field(node, "shield_geometry")
        indent = node.value[0][0].start_mark.column
        block = _format_block(geometry, indent, newline)
        if existing:
            key_node, value_node = existing
            start = source.rfind("\n", 0, key_node.start_mark.index) + 1
            if source[start:key_node.start_mark.index].strip():
                raise ValueError("shield_geometry must start on its own indented line.")
            end = _last_value_end(value_node)
            next_newline = source.find("\n", end)
            end = len(source) if next_newline < 0 else next_newline + 1
            edits.append((start, end, block))
        else:
            first_value = node.value[0][1]
            if not isinstance(first_value, yaml.ScalarNode):
                raise ValueError("A ship's first YAML field must be scalar for insertion.")
            end = source.find("\n", first_value.end_mark.index)
            if end < 0:
                edits.append((len(source), len(source), newline + block))
            else:
                edits.append((end + 1, end + 1, block))
    for start, end, replacement in sorted(edits, reverse=True):
        source = source[:start] + replacement + source[end:]
    return source


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    default_ships = Path(__file__).resolve().parents[2] / "sMPGWM.Game/assets/game_data/Ships.yml"
    parser.add_argument("--ships", type=Path, default=default_ships)
    parser.add_argument("--ship", help="Exact ship name; omitted means every ship.")
    parser.add_argument("--project-root", type=Path, help="Directory corresponding to res://; normally inferred.")
    parser.add_argument("--alpha-threshold", type=int, default=1)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--check", action="store_true", help="Check for missing/stale geometry without writing (default).")
    mode.add_argument("--write", action="store_true", help="Atomically update selected shield_geometry blocks.")
    args = parser.parse_args(argv)
    if not 1 <= args.alpha_threshold <= 255:
        parser.error("--alpha-threshold must be between 1 and 255.")
    try:
        ships_path = args.ships.resolve()
        with ships_path.open("r", encoding="utf-8", newline="") as stream:
            source = stream.read()
        document = yaml.load(source, Loader=_UniqueLoader)
        if not isinstance(document, dict) or not isinstance(document.get("ships"), list):
            raise ValueError("Ships.yml must contain a ships sequence.")
        root = yaml.compose(source)
        project_root = _project_root(ships_path, args.project_root)
        updates = {}
        selected = 0
        names = set()
        for ship in document["ships"]:
            if not isinstance(ship, dict) or not isinstance(ship.get("name"), str) or not ship["name"].strip():
                raise ValueError("Every ship must have a nonempty name.")
            name = ship["name"]
            if name in names:
                raise ValueError(f"Duplicate ship name: {name}")
            names.add(name)
            if args.ship is not None and name != args.ship:
                continue
            selected += 1
            path = _png_path(ship.get("texture_path"), project_root)
            with Image.open(path) as image:
                if image.format != "PNG":
                    raise ValueError(f"Ship '{name}' texture is not a PNG.")
                hull = silhouette_hull(image, args.alpha_threshold)
            geometry = fit_shield_geometry(hull)
            if ship.get("collider_points") and not contains_points(geometry, ship["collider_points"]):
                print(f"Warning: '{name}' authored hull extends outside PNG-derived shield geometry; hull was not included in the fit.", file=sys.stderr)
            if ship.get("shield_geometry") != geometry:
                updates[name] = geometry
            area = math.pi * (geometry["radius"] ** 2 if geometry["shape"] == "circle" else math.prod(geometry["radii"]))
            print(f"{name}: {geometry['shape']}, area={area:.6f}, {'stale' if name in updates else 'current'}")
        if not selected:
            raise ValueError("No ships matched the selection.")
        if not updates:
            print("Shield geometry is current.")
            return 0
        if not args.write:
            print("Shield geometry is missing or stale. Run with --write to update it.")
            return 1
        updated = _updated_yaml(source, root, updates)
        expected = document
        for ship in expected["ships"]:
            if ship["name"] in updates:
                ship["shield_geometry"] = updates[ship["name"]]
        if yaml.load(updated, Loader=_UniqueLoader) != expected:
            raise ValueError("Targeted YAML update changed unrelated data; nothing was written.")
        temporary = None
        try:
            with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", newline="", dir=ships_path.parent,
                                             prefix=ships_path.name + ".", suffix=".tmp", delete=False) as stream:
                temporary = Path(stream.name)
                stream.write(updated)
                stream.flush()
                os.fsync(stream.fileno())
            os.replace(temporary, ships_path)
            temporary = None
        finally:
            if temporary is not None:
                temporary.unlink(missing_ok=True)
        print(f"Updated {len(updates)} ship(s) in {ships_path}.")
        return 0
    except (OSError, ValueError, KeyError, TypeError, yaml.YAMLError) as error:
        print(f"Shield geometry bake failed: {error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
