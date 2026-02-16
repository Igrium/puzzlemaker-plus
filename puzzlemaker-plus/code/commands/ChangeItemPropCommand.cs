using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.Commands;

public class ChangeItemPropCommand(Item item, string propName, string newVal) : ICommand
{

    private string? _oldVal;
    
    public bool Execute()
    {
        _oldVal = item.GetProperty(propName);
        item.SetProperty(propName, newVal);
        return true;
    }

    public void Undo()
    {
        item.SetProperty(propName, _oldVal ?? "");
    }

    public void Redo()
    {
        item.SetProperty(propName, newVal);
    }
}