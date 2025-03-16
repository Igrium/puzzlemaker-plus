@tool
extends Node

@export_tool_button("Run Test Script", "Callable") var test_action = test_script

func test_script():
	print($"../Area3D/Television_01_2k/Television_01".get_surface_override_material(0))
	print("Hello World")
