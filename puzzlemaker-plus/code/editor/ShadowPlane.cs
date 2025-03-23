using System;
using Godot;

namespace PuzzlemakerPlus.Editor;

[GlobalClass]
public partial class ShadowPlane : MeshInstance3D
{
    [Export]
    public ShaderMaterial? Material { get; set; }
    private ShaderMaterial mat => Material ?? throw new NullReferenceException("Please add a material.");
    private PlaneMesh _planeMesh = new();

    private PuzzlemakerWorld World => EditorState.Instance.World;

    private ImageTexture? _texture;
    private Vector2 _prevSize;

    public ShadowPlane()
    {
        Mesh = _planeMesh;
    }

    public async void Update()
    {
        if (World.IsEmpty())
            return;

        var (minBounds, maxBounds) = World.GetFilledBounds();

        float avgX = (minBounds.X + maxBounds.X) / 2;
        float avgY = (minBounds.Y + maxBounds.Y) / 2;
        int y = minBounds.Y - 4;

        Position = new Vector3(avgX, y, avgY);
        _planeMesh.Size = new Vector2(maxBounds.X - minBounds.X, maxBounds.Z - minBounds.Z);
        Mesh = _planeMesh;
        _planeMesh.Material = mat;

        using var image = await ShadowTextureGenerator.GenerateShadowImage(World, y);

        Vector2 imageSize = image.GetSize();
        if (_texture != null)
        {
            if (_prevSize == imageSize)
            {
                _texture.Update(image);
            }
            else
            {
                _texture.SetImage(image);
            }
        }
        else
        {
            _texture = ImageTexture.CreateFromImage(image);
        }
        _prevSize = imageSize;

        mat.SetShaderParameter("shadow_mask", _texture);
    }

}
