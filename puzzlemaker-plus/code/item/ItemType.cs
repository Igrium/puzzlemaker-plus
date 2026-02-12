using System.Collections.Generic;
using System.Linq;

namespace PuzzlemakerPlus.Items;

/// <summary>
/// The rotation mode of the item. Rotations are always done on the axis of the mount direction.
/// </summary>
public enum RotationMode
{
    /// <summary>
    /// This item can't rotate.
    /// </summary>
    Fixed,

    /// <summary>
    /// This item can rotate in 90-degree increments.
    /// </summary>
    Quarter,

    /// <summary>
    /// This item can rotate any amount.
    /// </summary>
    Full
}

/// <summary>
/// A single item type such as a cube or a button.
/// One instance of this class will exist per item json in the package.
/// </summary>
public sealed class ItemType
{
    public string ItemClassName { get; set; } = "Item";

    /// <summary>
    /// All the properties this item will have, along with their types
    /// </summary>
    public Dictionary<string, PropDef> PropNames { get; } = new();

    public RotationMode RotationMode { get; set; } = RotationMode.Fixed;
    
    public List<InstanceType> Instances { get; } = new();

    /// <summary>
    /// Find all the instances that this item may use for the given condition, in order of priority.
    /// Priority is first determined by matching theme, then by matching condition, and finally by definition order.
    /// </summary>
    /// <param name="props">The item's current properties.</param>
    /// <param name="theme">The current level's theme.</param>
    /// <returns>An enumerable of all the legal instances in order.</returns>
    public IEnumerable<InstanceType> GetLegalInstances(IReadOnlyDictionary<string, string> props, string theme)
    {
        List<InstanceType> themed = new(Instances.Count);
        List<InstanceType> conditioned = new(Instances.Count);
        List<InstanceType> generic = new(Instances.Count);

        foreach (var instance in Instances)
        {
            if (!instance.IsLegal(props, theme))
                continue;
            
            if (instance.Themes.Any())
                themed.Add(instance);
            else if (instance.Conditions.Any())
                conditioned.Add(instance);
            else
                generic.Add(instance);
        }
        
        return themed.Concat(conditioned).Concat(generic);
    }
    
    public InstanceType? GetInstance(IReadOnlyDictionary<string, string> props, string theme)
    {
        return GetLegalInstances(props, theme).FirstOrDefault();
    }

    public string? GetEditorModel(IReadOnlyDictionary<string, string> props, string theme)
    {
        return GetLegalInstances(props, theme)
            .FirstOrDefault(i => string.IsNullOrWhiteSpace(i.EditorModel))
            ?.EditorModel;
        
    }
    
    public sealed class PropDef
    {
        /// <summary>
        /// A reference to the PackedScene used for this instance's GUI editor.
        /// </summary>
        public string Editor { get; set; } = string.Empty;

        public string? DefaultValue { get; set; }
        public string? DisplayName { get; set; }
        
        /// <summary>
        /// If set, apply this property as an instance param  
        /// </summary>
        public bool InstanceParam { get; set; } = false;
        
        /// <summary>
        /// If this is an enum, the available options to choose from.
        /// </summary>
        public List<object> Options { get; } = new();
    }

    /// <summary>
    /// The required data to compile an instance into the level
    /// </summary>
    public sealed class InstanceType
    {
        /// <summary>
        /// The VMF path to use.
        /// </summary>
        public string VMFPath { get; set; } = string.Empty;

        /// <summary>
        /// This instance can be used when these themes are active. If empty, valid for all themes.
        /// </summary>
        /// <remarks>Instance types with the current theme explicitly specified are prioritized over generic instances.</remarks>
        public List<string> Themes { get; } = new();

        /// <summary>
        /// The conditions for this instance to be triggered.
        /// </summary>
        public List<InstanceCondition> Conditions { get; } = new();

        /// <summary>
        /// The model to use in the editor when this instance is used.
        /// If unset, use a lower-priority instance.
        /// </summary>
        public string? EditorModel { get; set; }

        public bool IsLegal(IReadOnlyDictionary<string, string> props, string theme)
        {
            if (Themes.Any() && !Themes.Contains(theme))
                return false;
            foreach (var cond in Conditions)
            {
                if (!cond.Test(props))
                    return false;
            }

            return true;
        }
    }

    public sealed class InstanceCondition
    {
        public string PropName { get; set; } = "";
        public string? ExpectedValue { get; set; }
        public bool Invert { get; set; } = false;

        public bool Test(IReadOnlyDictionary<string, string> props)
        {
            bool result = false;
            if (props.TryGetValue(PropName, out var prop) && prop == ExpectedValue)
                result = true;

            return Invert ? !result : result;
        }
    }
}

