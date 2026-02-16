using Godot;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus.code.editor;

[GlobalClass]
public partial class ItemContextMenu : PanelContainer
{
    [Export] 
    public Container ItemContainer { get; set; } = null!;

    private Item? _item;
    
    public Item? Item => _item;

    public void SetItem(Item item)
    {
        _item = item;

        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }
        
        foreach (var (name, def) in item.Type.PropNames)
        {
            var scene = ResourceLoader.Load<PackedScene>(def.Editor);
            if (scene == null)
            {
                continue;
            }

            var editor = scene.Instantiate();
            var handle = FindChildByType<PropEditorHandle>(editor);

            if (handle == null)
            {
                GD.PushError("No PropEditorHandle found in editor scene ", def.Editor);
                continue;
            }

            handle.PropName = name;
            handle.Item = item;
            
            ItemContainer.AddChild(handle);
        }
    }

    private static T? FindChildByType<T>(Node parent) where T : Node
    {
        if (parent is T node)
            return node;
        foreach (var child in parent.GetChildren())
        {
            var cNode = FindChildByType<T>(child);
            if (cNode != null)
                return cNode;
        }

        return null;
    }
}