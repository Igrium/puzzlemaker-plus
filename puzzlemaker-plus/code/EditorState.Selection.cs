using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PuzzlemakerPlus.Items;

namespace PuzzlemakerPlus;

public partial class EditorState
{

    [Signal]
    public delegate void OnUpdatedGridScaleEventHandler(int newScale);

    [Signal]
    public delegate void OnUpdatedSelectionEventHandler(Aabb selection);

    [Signal]
    public delegate void UpdatedSelectedItemsEventHandler(Array<Item> selectedItems);

    private int _gridScale = 4;


    /// <summary>
    /// The scale of the editor grid in voxel units. Selection is always a multiple of this scale. If the grid scale changes, selection will re-calculate to accomidate.
    /// </summary>
    public int GridScale => _gridScale;

    public void SetGridScale(int gridScale)
    {
        if (gridScale <= 0)
            throw new ArgumentException("Grid scale must be greater than zero.", nameof(gridScale));
        else if (gridScale == _gridScale) 
            return;

        bool updateSelection = gridScale > _gridScale;

        _gridScale = gridScale;
        EmitSignal(SignalName.OnUpdatedGridScale, gridScale);

        if (updateSelection)
        {
            SetSelection(Selection);
        }
    }

    public void IncreaseGridScale()
    {
        SetGridScale(GridScale * 2);
    }

    public bool DecreaseGridScale()
    {
        if (GridScale <= 1) return false;
        SetGridScale(GridScale / 2);
        return true;
    }

    private Aabb _selection;

    /// <summary>
    /// <p>The current selection of the editor.Selections work based on vertices: 
    /// a face is considered selected if all 4 of its verts are selected, and a voxel is considered selected if all 6 of its verts are.</p>
    /// <p>Alternatively, one could assert selection is based on voxels, being start-inclusive and end-exclusive.</p>
    /// </summary>
    public Aabb Selection => _selection;
    
    /// <summary>
    /// The hit normal of the last selection raycast.
    /// </summary>
    public Vector3 SelectionNormal { get; private set; } = Vector3.Zero;

    private HashSet<Item> _selectedItems = new();

    public IReadOnlySet<Item> SelectedItems => _selectedItems;

    private void EmitUpdatedSelectedItems()
    {
        Array<Item> selected = [.. SelectedItems];
        EmitSignal(SignalName.UpdatedSelectedItems, selected);
    }

    public void SetSelection(Aabb selection, bool includeItems = false)
    {
        selection = SnapSelectionToGrid(selection, GridScale);
        if (selection == _selection)
            return;

        _selection = selection;

        EmitSignal(SignalName.OnUpdatedSelection, selection);
        if (includeItems)
            SetSelectedItems(selection);
    }

    public void ExpandSelection(Vector3 newPos, bool includeItems = false)
    {
        newPos = SnapVectorToGrid(newPos, GridScale);
        SetSelection(Selection.Expand(newPos), includeItems);
    }

    public void SelectFace(VoxelFace face, bool expand = false, bool includeItems = false)
    {
        var selection = ExpandToGrid(face, GridScale, face.Direction.GetAxis());
        SetSelection(expand ? Selection.Merge(selection) : selection, includeItems);
    }

    public void SelectFace(Vector3I pos, int direction, bool expand = false, bool includeItems = false)
    {
        SelectFace(new VoxelFace(pos, (Direction)direction), expand, includeItems);
    }

    public Array<Item> GetSelectedItems()
    {
        Array<Item> array = [.. SelectedItems];
        return array;
    }


    /// <summary>
    /// Select items based on a voxel selection. Items that are in or are attached to a selected voxel become selected.
    /// </summary>
    /// <param name="selection">The voxel selection.</param>
    public void SetSelectedItems(Aabb selection)
    {
        _selectedItems.Clear();
        foreach (var item in Project.Items.Values)
        {
            if (selection.HasPoint(item.Position))
                _selectedItems.Add(item);
        }
        EmitUpdatedSelectedItems();
    }

    public void SetSelectedItems(Godot.Collections.Array<Item> items)
    {
        SetSelectedItems((IEnumerable<Item>)items);
    }

    public void SetSelectedItems(IEnumerable<Item> items)
    {
        _selectedItems.Clear();
        foreach (var item in items)
        {
            _selectedItems.Add(item);
        }
        EmitUpdatedSelectedItems();
    }

    public bool IsItemSelected(Item item)
    {
        return _selectedItems.Contains(item);
    }

    /// <summary>
    /// Set an item's selection state.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="selected"></param>
    /// <returns></returns>
    public bool SetItemSelected(Item item, bool selected)
    {
        bool result;
        if (selected)
            result = _selectedItems.Add(item);
        else
            result = _selectedItems.Remove(item);

        if (result)
            EmitUpdatedSelectedItems();

        return result;
    }

    /// <summary>
    /// Set the selection based on the result of a raycast.
    /// </summary>
    /// <param name="pos">Raycast hit position.</param>
    /// <param name="normal">Raycast hit normal.</param>
    /// <param name="expand">Whether to expand the current selection.</param>
    public void SelectRaycast(Vector3 pos, Vector3 normal, bool expand = false)
    {
        Direction direction = Directions.GetClosestDirection(normal);

        // Subtract normal to avoid possible floating point errors.
        pos -= normal * .25f;
        Vector3I voxelPos = new Vector3I((int)MathF.Floor(pos.X), (int)MathF.Floor(pos.Y), (int)MathF.Floor(pos.Z));
        SelectFace(new VoxelFace(voxelPos, direction), expand);
        SelectionNormal = normal;
    }

    private static Aabb ExpandToGrid(VoxelFace face, int gridScale = 1, int ignoreAxis = -1)
    {
        var (vert1, vert2, vert3, vert4) = face.GetVertices();

        // This could definately be optimized, but I don't really care tbh.
        
        int vertXMin = MathUtils.Min(vert1.X, vert2.X, vert3.X, vert4.X);
        int vertXMax = MathUtils.Max(vert1.X, vert2.X, vert3.X, vert4.X);

        int vertYMin = MathUtils.Min(vert1.Y, vert2.Y, vert3.Y, vert4.Y);
        int vertYMax = MathUtils.Max(vert1.Y, vert2.Y, vert3.Y, vert4.Y);

        int vertZMin = MathUtils.Min(vert1.Z, vert2.Z, vert3.Z, vert4.Z);
        int vertZMax = MathUtils.Max(vert1.Z, vert2.Z, vert3.Z, vert4.Z);

        int xMin = ignoreAxis != 0 ? MathUtils.RoundDown(vertXMin, gridScale) : vertXMin;
        int xMax = ignoreAxis != 0 ? MathUtils.RoundUp(vertXMax, gridScale) : vertXMax;

        int yMin = ignoreAxis != 1 ? MathUtils.RoundDown(vertYMin, gridScale) : vertYMin;
        int yMax = ignoreAxis != 1 ? MathUtils.RoundUp(vertYMax, gridScale) : vertYMax;

        int zMin = ignoreAxis != 2 ? MathUtils.RoundDown(vertZMin, gridScale) : vertZMin;
        int zMax = ignoreAxis != 2 ? MathUtils.RoundUp(vertZMax, gridScale) : vertZMax;

        Vector3 min = new Vector3(xMin, yMin, zMin);
        Vector3 max = new Vector3(xMax, yMax, zMax);

        return new Aabb(min, max - min);
    }

    public void ClearSelection()
    {
        SetSelection(default);
    }

    /// <summary>
    /// Check if a given face is selected.
    /// </summary>
    public bool IsFaceSelected(VoxelFace face)
    {
        var (vert1, vert2, vert3, vert4) = face.GetVertices();
        var selection = Selection;
        return selection.HasPoint(vert1)
            && selection.HasPoint(vert2)
            && selection.HasPoint(vert3)
            && selection.HasPoint(vert4);
    }

    public bool IsFaceSelected(Vector3I face, int direction)
    {
        return IsFaceSelected(new VoxelFace(face, (Direction)direction));
    }

    /// <summary>
    /// Check if a given voxel is selected in its entirety.
    /// </summary>
    public bool IsVoxelSelected(Vector3 voxel)
    {
        var selection = Selection;
        Vector3I min = selection.Position.RoundInt();
        Vector3I max = (selection.Position + selection.Size).RoundInt();
        Vector3I v = voxel.RoundInt();

        return (min.X <= v.X && v.X < max.X
            && min.Y <= v.Y && v.Y < max.Y
            && min.Z <= v.Z && v.Z < max.Z);

        //return selection.HasPoint(voxel) && selection.HasPoint(voxel + new Vector3(1, 1, 1));
    }

    public Vector3I[] GetSelectedVoxels()
    {
        var selection = Selection;
        Vector3I min = (selection.Position).RoundInt();
        Vector3I max = (selection.Position + selection.Size).RoundInt();

        Vector3I[] array = new Vector3I[(max.X - min.X) * (max.Y - min.Y) * (max.Z - min.Z)];

        int index = 0;
        for (int z = min.Z; z < max.Z; z++)
        {
            for (int y = min.Y; y < max.Y; y++)
            {
                for (int x = min.X; y < max.X; x++)
                {
                    array[index++] = new Vector3I(x, y, z);
                }
            }
        }
        return array;
    }

    /// <summary>
    /// The fact that GDScript doesn't have a PackedVector3IArray is dumb.
    /// </summary>
    public Vector3[] GetSelectedVoxelsFloat()
    {
        var selection = Selection;
        Vector3I min = (selection.Position).RoundInt();
        Vector3I max = (selection.Position + selection.Size).RoundInt();

        Vector3[] array = new Vector3[(max.X - min.X) * (max.Y - min.Y) * (max.Z - min.Z)];

        int index = 0;
        for (int z = min.Z; z < max.Z; z++)
        {
            for (int y = min.Y; y < max.Y; y++)
            {
                for (int x = min.X; y < max.X; x++)
                {
                    array[index++] = new Vector3(x, y, z);
                }
            }
        }
        return array;
    }

    private static Aabb SnapSelectionToGrid(in Aabb selection, int gridScale)
    {
        Vector3 start = selection.Position;
        Vector3 end = start + selection.Size;

        start.X = MathF.Round(start.X / gridScale) * gridScale;
        start.Y = MathF.Round(start.Y / gridScale) * gridScale;
        start.Z = MathF.Round(start.Z / gridScale) * gridScale;

        end.X = MathF.Round(end.X / gridScale) * gridScale;
        end.Y = MathF.Round(end.Y / gridScale) * gridScale;
        end.Z = MathF.Round(end.Z / gridScale) * gridScale;

        return new Aabb(start, end - start);
    }

    private static Vector3 SnapVectorToGrid(Vector3 vec, int gridScale)
    {
        vec.X = MathF.Round(vec.X / gridScale) * gridScale;
        vec.Y = MathF.Round(vec.Y / gridScale) * gridScale;
        vec.Z = MathF.Round(vec.Z / gridScale) * gridScale;
        return vec;
    }
}
