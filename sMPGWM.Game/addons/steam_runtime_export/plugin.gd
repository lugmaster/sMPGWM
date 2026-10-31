@tool
extends EditorPlugin

const SteamRuntimeExport = preload("export_plugin.gd")
var export_plugin: EditorExportPlugin

func _enter_tree() -> void:
	export_plugin = SteamRuntimeExport.new()
	add_export_plugin(export_plugin)

func _exit_tree() -> void:
	remove_export_plugin(export_plugin)
	export_plugin = null
