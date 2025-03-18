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
        public Vector2 MouseStartPos;
        public bool HasInitializedDrag;
        public Vector2 Offset;
        public Quaternion StartRotation;
        public Vector3 StartMountNormal;
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

    [Export]
    public float DragStartThreshold { get; set; } = 5f;

    /// <summary>
    /// Physics layers to consider for traceResult.
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint RaycastMask { get; set; } = 0xFFFFFFFF;

    private Vector3 _baseMountNormal = Vector3.Down;


    /// <summary>
    /// The normal of the item's mount direction in local space.
    /// </summary>
    [ExportCategory("Mount")]
    [Export]
    public Vector3 BaseMountNormal
    {
        get => _baseMountNormal;
        set => _baseMountNormal = value.Normalized();
    }

    /// <summary>
    /// When snapping to the wall, if the wall's global orientation matches one of these, rotate the item so the mount direction matches its normal.
    /// </summary>
    [Export]
    public DirectionFlags MountSnapOrientations { get; set; }

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

        Quaternion quat = node.GlobalBasis.GetRotationQuaternion();

        _dragState = new DragState
        {
            MouseStartPos = mousePos,
            Offset = nodePos - mousePos,
            StartRotation = quat,
            StartMountNormal = quat * BaseMountNormal,
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
            var didInit = _dragState.Value.HasInitializedDrag;
            _dragState = null;
            if (didInit)
                EmitSignalDragDropped(node, node.GlobalPosition, node.GlobalRotation);
        }
    }

    private void TickDrag()
    {
        if (!_dragState.HasValue)
            return;

        Vector2 mousePos = GetViewport().GetMousePosition();

        if (!_dragState.Value.HasInitializedDrag)
        {
            if (mousePos.DistanceSquaredTo(_dragState.Value.MouseStartPos) >= DragStartThreshold * DragStartThreshold)
            {
                _dragState = _dragState.Value with { HasInitializedDrag = true };
            }
            else return;
        }

        RaycastResult traceResult;
        Raycast(mousePos + _dragState.Value.Offset, out traceResult);
        if (!traceResult.Hit)
            return;

        traceResult.Normal *= -1;
        Node3D node = _dragState.Value.Node;
        node.GlobalPosition = traceResult.Position.Round(SnapIncrement);
        
        if (MountSnapOrientations != 0)
        {
            Direction wallDirection = Directions.GetClosestDirection(traceResult.Normal);
            if (MountSnapOrientations.HasDirection(wallDirection) && !_dragState.Value.StartMountNormal.IsEqualApprox(traceResult.Normal))
            {
                
                Quaternion deltaRot = ComputeRotationBetween(_dragState.Value.StartMountNormal, traceResult.Normal);

                SetNodeGlobalRotation(node, deltaRot * _dragState.Value.StartRotation);
            }
            else
            {
                SetNodeGlobalRotation(node, _dragState.Value.StartRotation);
            }
        }
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

    private static Quaternion ComputeRotationBetween(Vector3 from, Vector3 to)
    {
        // Ensure the vectors are normalized
        from = from.Normalized();
        to = to.Normalized();

        // Calculate dot product
        float dot = from.Dot(to);

        // If vectors are already aligned, return the identity quaternion
        if (Mathf.IsEqualApprox(dot, 1.0f))
        {
            return Quaternion.Identity;
        }

        // If vectors are opposite, return a 180 degree rotation quaternion
        if (Mathf.IsEqualApprox(dot, -1.0f))
        {
            // Find an orthogonal vector to use as the rotation axis
            Vector3 orthogonal = from.Cross(Vector3.Right).Length() < 0.1 ? from.Cross(Vector3.Up) : from.Cross(Vector3.Right);
            orthogonal = orthogonal.Normalized();

            return new Quaternion(orthogonal, Mathf.Pi);
        }

        // Calculate the rotation axis
        Vector3 axis = from.Cross(to).Normalized();

        // Calculate the angle of rotation
        float angle = Mathf.Acos(dot);

        // Create and return the quaternion representing the rotation
        return new Quaternion(axis, angle).Normalized();
    }

    // I don't know why this isn't vanilla lol.
    private static void SetNodeGlobalRotation(Node3D node, Quaternion rotation)
    {
        Transform3D transform = node.GlobalTransform;
        transform.Basis = new Basis(rotation) * Basis.FromScale(transform.Basis.Scale);
        node.GlobalTransform = transform;
    }
}
