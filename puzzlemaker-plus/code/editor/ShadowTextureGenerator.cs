using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus.Editor;

/// <summary>
/// Generates a voxel shadow map from a voxel world.
/// </summary>
[GlobalClass]
public partial class ShadowTextureGenerator : RefCounted
{
    [Signal]
    public delegate void GenerationCompleteEventHandler(int width, int height, byte[] data);

    public record struct ShadowMask
    {
        internal ShadowMask(int width, int height, byte[] data)
        {
            this.width = width;
            this.height = height;
            this.data = data;
        }

        public readonly int width;
        public readonly int height;
        public byte[] data;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"width: {width}, height: {height}");
            
            for (int v = 0; v < height; v++)
            {
                builder.AppendLine();
                builder.Append(Convert.ToHexString(data, v * width, width));
            }

            return builder.ToString();
        }
    }

    public static ShadowMask GenerateShadowMask(VoxelWorld<PuzzlemakerVoxel> world, int startY)
    {
        Vector3I worldMin = world.GetMinPos();
        Vector3I worldMax = world.GetMaxPos();

        int width = worldMax.X - worldMin.X;
        int height = worldMax.Z - worldMin.Z;
        byte[] mask = new byte[width * height];

        List<Task> tasks = new List<Task>(width * height);

        for (int v = 0; v < height; v++)
        {
            for (int u = 0; u < width; u++)
            {
                Vector3I startPos = new Vector3I(u - worldMin.X, startY, v - worldMin.Z);
                // Perform raycast and add result to mask.
                Vector3I? traceResult = world.Trace(new Vector3I(u, startY, v), Direction.Up, vox => vox.IsOpen);
                int index = v * width + u;
                if (traceResult.HasValue)
                {
                    // Trace should never return a value less than startY.
                    int deltaY = traceResult.Value.Y - startY;
                    mask[index] = (byte)Math.Min(deltaY, byte.MaxValue);
                }
                else
                {
                    mask[index] = byte.MaxValue;
                }
            }
        }
        return new ShadowMask(width, height, mask);
    }

    public async void GenerateShadowMaskAsync(int startY)
    {
        PuzzlemakerWorld world = EditorState.Instance.World;
        var mask = await Task.Run(() => GenerateShadowMask(world, startY));
        EmitSignalGenerationComplete(mask.width, mask.height, mask.data);
    }

    public static async Task<Image> GenerateShadowImage(VoxelWorld<PuzzlemakerVoxel> world, int startY)
    {
        ShadowMask mask = GenerateShadowMask(world, startY);
        Image result;
        //if (existing != null)
        //{
        //    result = existing;
        //    result.Resize(mask.width, mask.height, Image.Interpolation.Nearest);
        //    GD.Print(mask);
        //    GD.Print(result.IsCompressed());
        //    result.SetData(mask.width, mask.width, false, Image.Format.L8, mask.data);
        //}
        //else
        {

            result = Image.CreateFromData(mask.width, mask.height, false, Image.Format.L8, mask.data);
        }

        return result;
    }


}
