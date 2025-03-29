using System;
using System.Collections.Generic;
using Godot;

namespace PuzzlemakerPlus;

public class EdgeModelGenerator
{
    public enum CornerType
    {
        None, Corner, MeshEdge
    }

    public static void FindCorners(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, IDictionary<Vector3I, Vector3> corners)
    {
        foreach (var vertex in mesh.VertexRefCache.Keys)
        {
            Vector3I vertInt = vertex.RoundInt();
            Vector3 normal = VoxelCornerNormals.ComputeCornerNormal(world, vertInt);

            if (!normal.IsCardinal())
            {
                corners.Add(vertInt, normal);
            }
        }
    }

    public static void CreateEdgeModel(SimpleQuadMesh mesh, IVoxelView<PuzzlemakerVoxel> world, Action<Quad> quadConsumer, float length = 1, bool drawDebug = false)
    {
        Dictionary<Vector3I, Vector3> corners = new();
        FindCorners(mesh, world, corners);

        if (drawDebug)
        {
            foreach (var (vertex, normal) in corners)
            {
                DrawDebugVector(vertex, vertex + normal, 8);
            }
        }


    }

    private static void DrawDebugVector(Vector3 start, Vector3 end, float duration = 0)
    {
        Callable.From(() => DebugDraw3D.DrawArrow(start, end, duration: duration)).CallDeferred();
    }
}
