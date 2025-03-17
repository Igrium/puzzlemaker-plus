using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PuzzlemakerPlus.Editor;

internal struct RaycastResult
{
    public bool Hit { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Normal { get; set; }
    public GodotObject? Collider { get; set; }
    public ulong ColliderID { get; set; }
    public Rid RID { get; set; }
    public int Shape { get; set; }

    public static RaycastResult FromDict(Dictionary dict)
    {
        return dict.Count() != 0 ? new RaycastResult
        {
            Hit = true,
            Position = dict["position"].AsVector3(),
            Normal = dict["normal"].AsVector3(),
            Collider = dict["collider"].AsGodotObject(),
            ColliderID = dict["collider_id"].AsUInt64(),
            RID = dict["rid"].AsRid(),
            Shape = dict["shape"].AsInt32()
        } : default;
    }
}



/// <summary>
/// Makes the parent node able to be dragged around in the editor.
/// </summary>
[GlobalClass]
public partial class Draggable : Node
{

    private struct DragState
    {
        public Vector2 Offset;
        public PhysicsDirectSpaceState3D PhysicsSpace;
        public Node3D Node;
    }

    public const float RAY_LENGTH = 4096;

    [Signal]
    public delegate void DragStartedEventHandler(Node3D node);

    [Signal]
    public delegate void DragDroppedEventHandler(Node3D node, Vector3 position, Vector3 rotation);

    /// <summary>
    /// Automatically check for when a player releases M1 and drop the draggable.
    /// </summary>
    [Export]
    public bool AutoDrop { get; set; }

    /// <summary>
    /// Grid Snap to this nearest grid increment.
    /// </summary>
    [Export]
    public float SnapIncrement { get; set; } = 0;

    /// <summary>
    /// Physics layers to consider for traceResult.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint RaycastMask { get; set; } = 0xFFFFFFFF;

    /// <summary>
    /// The sides on which the item is allowed to mount to a surface.
    /// </summary>
    [Export]  
    public DirectionFlags MountDirections { get; set; } = DirectionFlags.All;

    /// <summary>
    /// If we try to mount on a wall with an unsupported mount direction, rotate so the base mount direction equals the wall direction.
    /// </summary>
    [ExportCategory("Mount Rotation")]
    [Export]
    public bool AllowMountRotate { get; set; } = false;

    [Export]
    public Direction BaseMountDirection { get; set; } = Direction.Down;

    public Node3D Parent
    {
        get
        {
            var parent = GetParent();
            if (parent is Node3D node3d)
                return node3d;
            else
                throw new InvalidOperationException("Draggable must be a child of a Node3D.");
        }
    }

    private DragState? _dragState;

    public bool IsDragging => _dragState.HasValue;

    public override void _Input(InputEvent e)
    {
        base._Input(e);
        if (IsDragging && AutoDrop)
        {
            if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
                StopDragging();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (IsDragging)
            TickDrag();
    }

    public void StartDragging()
    {
        if (IsDragging)
            StopDragging();

        Node3D node = Parent;
        Vector2 nodePos = GetScreenPos(node.GlobalPosition);
        Vector2 mousePos = GetViewport().GetMousePosition();

        _dragState = new DragState
        {
            Offset = nodePos - mousePos,
            PhysicsSpace = node.GetWorld3D().DirectSpaceState,
            Node = node
        };
        EmitSignalDragStarted(node);
    }

    public void StopDragging()
    {
        if (_dragState.HasValue)
        {
            var node = _dragState.Value.Node;
            _dragState = null;
            EmitSignalDragDropped(node, node.GlobalPosition, node.GlobalRotation);
        }
    }

    private void TickDrag()
    {
        if (!_dragState.HasValue)
            return;

        RaycastResult traceResult;
        Raycast(GetViewport().GetMousePosition() + _dragState.Value.Offset, out traceResult);
        if (!traceResult.Hit)
            return;

        Node3D node = _dragState.Value.Node;
        Vector3 localNormal = node.Quaternion.Inverse() * -traceResult.Normal;
        Direction localMountDir = Directions.GetClosestDirection(localNormal);

        node.GlobalPosition = traceResult.Position.Round(SnapIncrement);
        //DebugDraw3D.DrawArrow(traceResult.Position, node.ToGlobal(localNormal * -3));

    }

    private void Raycast(Vector2 screenPos, out RaycastResult result)
    {
        if (!_dragState.HasValue)
            throw new InvalidOperationException("Can only raycast while dragging.");

        var space = _dragState.Value.PhysicsSpace;
        Camera3D cam = GetViewport().GetCamera3D();

        Vector3 start = cam.ProjectRayOrigin(screenPos);
        Vector3 end = start + cam.ProjectRayNormal(screenPos) * RAY_LENGTH;

        var query = PhysicsRayQueryParameters3D.Create(start, end);
        query.CollideWithAreas = true;
        query.CollisionMask = RaycastMask;

        query.Exclude = GetChildRIDs(_dragState.Value.Node);
        result = RaycastResult.FromDict(space.IntersectRay(query));
    }
    
    private Vector2 GetScreenPos(Vector3 worldPoint)
    {
        return GetViewport().GetCamera3D().UnprojectPosition(worldPoint);
    }

    private Array<Rid> GetChildRIDs(Node node)
    {
        Array<Rid> rids = new();
        GetChildRIDs(node, rids);
        return rids;
    }

    private void GetChildRIDs(Node node, Array<Rid> array)
    {
        if (node is CollisionObject3D collider)
        {
            array.Add(collider.GetRid());
        }
        foreach (var child in node.GetChildren())
        {
            GetChildRIDs(child, array);
        }
    }
}
