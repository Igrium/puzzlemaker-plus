using System;
using Godot;

namespace PuzzlemakerPlus.Editor;

public partial class RotationGizmo : Node3D
{
    [Signal]
    public delegate void DragStartedEventHandler(Node3D node);

    [Signal]
    public delegate void DragUpdatedEventHandler(Node3D node, float angle, Basis rotation);

    [Signal]
    public delegate void DragDroppedEventHandler(Node3D node, float angle, Basis rotation);

    private struct DragState
    {
        public Node3D? Node;
        public bool IsDragging;
        /// <summary>
        /// True if we've dragged past the update threshold at least once.
        /// </summary>
        public bool IsInitialized;

        public Vector3 CenterPos;
        public Basis StartBasis;

        public Vector3 Axis;
        public Plane Plane;
        public Vector3 StartMouseNormal;

        public float LastUpdateAngle;
        public float Angle; // In radians
    }

    [Export]
    public Node3D? Target { get; set; }

    [Export]
    public bool AutoDrop { get; set; } = true;

    public float SnapIncrementRadians { get; set; }

    /// <summary>
    /// The snap increment in degrees.
    /// </summary>
    [Export]
    public float SnapIncrement
    {
        get => Mathf.RadToDeg(SnapIncrementRadians);
        set => SnapIncrementRadians = Mathf.DegToRad(value);
    }

    public float UpdateThresholdRadians { get; set; } = .001f;

    /// <summary>
    /// DragUpdated will be called only after we've been dragged this many degrees.
    /// </summary>
    [Export]
    public float UpdateThreshold
    {
        get => Mathf.RadToDeg(UpdateThresholdRadians);
        set => UpdateThresholdRadians = Mathf.DegToRad(value);
    }

    private DragState _dragState;

    public bool IsDragging => _dragState.IsDragging;

    public Node3D GetTarget()
    {
        return Target ?? GetParentNode3D() ?? throw new InvalidOperationException("No valid target");
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
        if (IsDragging)
            return;

        _dragState = new DragState();

        Node3D node = GetTarget();
        _dragState.Node = node;
        _dragState.CenterPos = node.GlobalPosition;
        _dragState.StartBasis = node.GlobalBasis;

        _dragState.Axis = this.GlobalBasis.Y;
        _dragState.Plane = new Plane(_dragState.Axis, _dragState.CenterPos);

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3? hitResult = Raycast(mousePos, _dragState.Plane);

        if (hitResult.HasValue)
        {
            _dragState.StartMouseNormal = (hitResult.Value - _dragState.CenterPos).Normalized();
        }
        else
        {
            _dragState.StartMouseNormal = Vector3.Forward;
        }

        _dragState.IsDragging = true;
        EmitSignalDragStarted(node);
    }

    public void StopDragging()
    {
        if (!IsDragging) return;
        Node3D node = _dragState.Node ?? throw new NullReferenceException(); // Shouldn't happen

        DragState state = _dragState;
        _dragState = default;

        if (state.IsInitialized)
            EmitSignalDragDropped(node, state.Angle, node.GlobalBasis);
    }

    private void TickDrag()
    {
        if (!IsDragging) return;
        Node3D node = _dragState.Node ?? throw new NullReferenceException();

        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector3? hitResult = Raycast(mousePos, _dragState.Plane);
        if (!hitResult.HasValue)
            return;

        Vector3 normal = (hitResult.Value - _dragState.CenterPos).Normalized();

        float angle = _dragState.StartMouseNormal.SignedAngleTo(normal, _dragState.Axis);
        if (SnapIncrementRadians != 0)
        {
            angle = Mathf.Round(angle / SnapIncrementRadians) * SnapIncrementRadians;
        }

        if (!_dragState.IsInitialized)
        {
            if (Mathf.Abs(angle) > UpdateThresholdRadians)
                _dragState.IsInitialized = true;
            else return;
        }

        node.GlobalBasis = _dragState.StartBasis.Rotated(_dragState.Axis, angle);
        _dragState.Angle = angle;
        
        if (Mathf.Abs(angle - _dragState.LastUpdateAngle) > UpdateThresholdRadians)
        {
            _dragState.LastUpdateAngle = angle;
            EmitSignalDragUpdated(node, angle, node.GlobalBasis);
        }
    }

    private Vector3? Raycast(Vector2 screenPos, Plane plane)
    {
        Camera3D cam = GetViewport().GetCamera3D();

        Vector3 start = cam.ProjectRayOrigin(screenPos);
        Vector3 dir = cam.ProjectRayNormal(screenPos);

        return plane.IntersectsRay(start, dir);
    }
}
