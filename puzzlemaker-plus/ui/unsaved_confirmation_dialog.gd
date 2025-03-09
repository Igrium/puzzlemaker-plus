extends ConfirmationDialog

var _next_func: Callable

func _ready() -> void:
	confirmed.connect(_on_confirmed)
	
func _on_confirmed():
	_next_func.call()

func show_dialog(next_func: Callable):
	_next_func = next_func
	visible = true

## Check if the current project has been saved before calling a function. If it hasn't, confirm with the user.
func confirm_saved(next_func: Callable):
	if (Editor.UnSaved):
		show_dialog(next_func)
	else:
		next_func.call()
