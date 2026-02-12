using Godot;

namespace PuzzlemakerPlus.Items;

public partial class ItemProp<[MustBeVariant] T> : RefCounted
{
    public T Value { get; set; } = default!;

    /// <summary>
    /// Get a UI editor that will edit this value. Value should be directly modified.
    /// </summary>
    /// <returns></returns>
    public Control? GetEditor()
    {
        return null;
    }

    /// <summary>
    /// Called when this prop is being compiled.
    /// </summary>
    /// <param name="item">Item being compiled</param>
    public void Compile(Item item)
    {
        
    }
}