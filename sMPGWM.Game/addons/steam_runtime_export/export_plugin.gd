@tool
extends EditorExportPlugin

func _get_name() -> String:
	return "SteamRuntimeExport"

func _export_begin(features: PackedStringArray, _is_debug: bool, _path: String, _flags: int) -> void:
	if features.has("windows") and features.has("x86_64"):
		# An empty target places the DLL beside the executable, outside the PCK.
		add_shared_object("res://native/steam/windows/x86_64/steam_api64.dll", PackedStringArray(), "")
