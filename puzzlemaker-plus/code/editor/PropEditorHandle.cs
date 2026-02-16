using System.Collections.Generic;
using Godot;
using PuzzlemakerPlus.Commands;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.code.editor;

/// <summary>
/// Allows scenes for property editors to access the property they control.
/// </summary>
[GlobalClass]
public partial class PropEditorHandle : Node
{
    [Signal]
    public delegate void PropertyChangedEventHandler(string oldVal, string newVal);

    private Item? _item;

    public Item? Item
    {
        get =>  _item;
        set
        {
            if (_item == value)
                return;

            string oldVal = PropValue;
            if (_item != null)
            {
                _item.PropertyChanged -= _onPropChanged;
            }
            _item = value;
            if (_item != null)
            {
                _item.PropertyChanged += _onPropChanged;
            }
            EmitSignalPropertyChanged(oldVal, PropValue);
        }
    }

    private string _propName = "";

    public string PropName
    {
        get => _propName;
        set
        {
            if (_propName == value)
                return;
            var oldVal = PropValue;
            _propName = value;
            
            if (Item != null)
                EmitSignalPropertyChanged(oldVal, PropValue);
            
        }
    }
    
    public string PropValue => Item?.Properties.GetValueOrDefault(PropName) ?? "";

    public void SetPropValue(string newValue)
    {
        if (Item == null)
            return;
        EditorState.Instance.CommandStack.Execute(new ChangeItemPropCommand(Item, PropName, newValue));
    }

    public void Reset()
    {
        Item?.ResetProperty(PropName);
    }

    private void _onPropChanged(string propName, string oldVal, string newVal)
    {
        if (propName == PropName)
        {
            EmitSignalPropertyChanged(oldVal, newVal);
        }
    }

    public override void _ExitTree()
    {
        if (Item != null)
        {
            Item.PropertyChanged -= _onPropChanged;
        }
        base._ExitTree();
    }
}