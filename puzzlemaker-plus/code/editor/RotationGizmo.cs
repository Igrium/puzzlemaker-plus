using System;
using Godot;

namespace PuzzlemakerPlus.Editor;

[GlobalClass]
public partial class RotationGizmo : Node3D
{
    [Signal]
    public delegate void DragStartedEventHandler(Node3D node);

    [Signal]
    public delegate void DragUpdatedEventHandler(Node3D node, Basis rotation);

    [Signal]
    public delegate void DragDroppedEventHandler(Node3D node, Basis rotation);

    [Export]
    public Node3D? Target { get; set; }

    [Export]
    public bool AutoDrop { get; set; }

    public float SnapIncrementRadians { get; set; }

    /// <summary>
    /// The rotation increments to use, in degrees.
    /// </summary>
    [Export]
    public float SnapIncrement
    {
        get => Mathf.RadToDeg(SnapIncrementRadians);
        set => SnapIncrementRadians = Mathf.DegToRad(value);
    }

    public float DragStartTherholdRadians { get; set; } = Mathf.DegToRad(1);

    [Export]
    public float DragStartThreshold
    {
        get => Mathf.RadToDeg(DragStartTherholdRadians);
        set => DragStartTherholdRadians = Mathf.DegToRad(DragStartTherholdRadians);
    }

    public bool IsDragging { get; private set; }

    private Vector3 _axis;
    private Plane _plane;

    private Vector3 _nodePos;
    private Basis _startBasis;
    private Vector3 _startNormal;

    private bool _hasInitializedDrag;

    public Node3D GetTarget()
    {
        if (Target != null)
            return Target;
        Node parent = GetParent();
        if (parent is Node3D threedee)
            return threedee;
        else
            throw new InvalidOperationException("RotationGizmo must have Target set or be the direct child of a Node3D.");
    }

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
        IsDragging = true;
        Node3D node = GetTarget();
        _nodePos = node.GlobalPosition;
        _startBasis = node.GlobalBasis;

        _axis = this.GlobalTransform.Basis.Y.Normalized(); // Rotating this node allows axis change
        _plane = new Plane(_axis, node.GlobalPosition);

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3 intersection = Raycast(mousePos, _plane).GetValueOrDefault();

        _startNormal = (intersection - _nodePos).Normalized();
        _hasInitializedDrag = false;

        EmitSignalDragStarted(node);
    }

    public void StopDragging()
    {
        if (!IsDragging) return;
        var node = GetTarget();
        IsDragging = false;
        if (_hasInitializedDrag)
        {
            EmitSignalDragDropped(node, node.GlobalBasis);
        }
    }

    private void TickDrag()
    {
        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3? opt = Raycast(mousePos, _plane);
        if (!opt.HasValue)
            return;

        Vector3 intersection = opt.Value;
        Vector3 normal = (intersection - _nodePos).Normalized();

        float angle = _startNormal.SignedAngleTo(normal, _axis);

        if (!_hasInitializedDrag)
        {
            if (Mathf.Abs(angle) >= DragStartTherholdRadians)
                _hasInitializedDrag = true;
            else return;
        }

        //Basis basis = _startRotation.Rotated(_axis, angle);
        Node3D node = GetTarget();
        node.GlobalBasis = _startBasis.Rotated(_axis, angle);
    }

    private Vector3? Raycast(Vector2 screenPos, Plane plane)
    {
        Camera3D cam = GetViewport().GetCamera3D();

        Vector3 start = cam.ProjectRayOrigin(screenPos);
        Vector3 dir = cam.ProjectRayNormal(screenPos);

        return _plane.IntersectsRay(start, dir);
    }
}
