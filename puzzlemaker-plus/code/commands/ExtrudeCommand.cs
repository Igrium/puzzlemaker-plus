using Godot;

namespace PuzzlemakerPlus.Commands;

public class ExtrudeCommand : AbstractWorldCommand
{
    private readonly Aabb _selection;
    private readonly Direction _direction;
    private readonly int _amount;
    /// <summary>
    /// If true, pull out of the wall, otherwise, push into the wall.
    /// </summary>
    private readonly bool _pulls;

    public ExtrudeCommand(Aabb selection, Direction direction, int amount, bool pulls)
    {
        _selection = selection;
        _direction = direction;
        _amount = amount;
        _pulls = pulls;
    }

    protected override void Execute(IVoxelView<PuzzlemakerVoxel> world)
    {
        Vector3I normal = _direction.GetNormal();


        if (_selection.HasVolume()) 
        {
            GD.PushWarning("Trying to perform an extrude with a selection that has volume. Results may be unexpected.");
        }
        
        Vector3I sampleMin = _selection.Position.RoundInt();
        Vector3I sampleMax = _selection.End.RoundInt();

        // Sample should be inclusive.
        for (int i = 0; i < 3; i++) 
        {
            if (_direction.GetAxis() != i)
                sampleMax[i] -= 1;
        }

        // If we're pushing into the wall, select the voxel just outside of it.
        if (!_pulls && _direction.IsPositive()) 
        {
            sampleMin -= normal;
            sampleMax -= normal;
        }
        // If we're pulling out of the wall, select the voxel just inside of it.
        else if (_pulls && !_direction.IsPositive())
        {
            sampleMin += normal;
            sampleMax += normal;
        }

        for (int x = sampleMin.X; x <= sampleMax.X; x++)
        {
            for (int y = sampleMin.Y; y <= sampleMax.Y; y++)
            {
                for (int z = sampleMin.Z; z <= sampleMax.Z; z++)
                {
                    Vector3I startPos = new Vector3I(x, y, z);
                    PuzzlemakerVoxel voxel = world.GetVoxel(startPos);
                    world.Fill(startPos, startPos + normal * _amount, voxel);
                }
            }
        }

        EditorState editor = EditorState.Instance;
        editor.SetSelection(editor.Selection.Move(normal * _amount));
    }
}