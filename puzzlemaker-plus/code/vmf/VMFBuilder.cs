using System;
using System.Collections.Generic;
using VMFLib.Objects;
using VMFLib.Parsers;
using VMFLib.VClass;

namespace PuzzlemakerPlus.VMF;

public class VMFBuilder
{
    private enum Axis { X, Y, Z }

    public IList<Solid> Solids { get; } = new List<Solid>();

    private int _faceCount = 0;
    private int _solidCount = 0;

    public void AddSolid(SolidBuilder solid)
    {
        Solids.Add(solid.Build());
    }

    public void WriteVMF(VClassWriter writer)
    {
        writer.WriteClass(new VersionInfo());

        World world = new World();
        world.Solids.AddRange(Solids);
        Godot.GD.Print(String.Join(",", Solids));
        writer.WriteClass(world);
    }

    public void WriteVMF(string path)
    {
        using (VClassWriter writer = new VClassWriter(new FileAccessStream(path, Godot.FileAccess.ModeFlags.Write)))
        {
            WriteVMF(writer);
        }
    }

    /// <summary>
    /// Contains utility methods for constructing solid BSP.
    /// Note: many of these methods aim to create a finished brush, so chaining them may result in invalid geometry.
    /// </summary>
    public class SolidBuilder
    {
        private readonly VMFBuilder _vmfBuilder;

        protected readonly List<Side> _sides = new();

        public string? Material { get; set; }

        public string? Material2 { get; set; }

        private int _lightmapScale = 8;

        public int LightmapScale
        {
            get => _lightmapScale;
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "Lightmap scale must be at least 1.");
                _lightmapScale = value;
            }
        }

        public SolidBuilder(VMFBuilder vmfBuilder)
        {
            _vmfBuilder = vmfBuilder;
        }

        public virtual void AddSide(Side side)
        {
            side.Id = ++_vmfBuilder._faceCount;
            _sides.Add(side);
        }

        /// <summary>
        /// Add a side defined by a plane. Auto-calculate UV, etc.
        /// </summary>
        /// <param name="plane">Plane to add.</param>
        /// <param name="material">Material to give it.</param>
        /// <param name="lightmapScale">Lightmap scale to use. 8 by default because it's not 2004 anymore.</param>
        public void AddSide(in Plane plane, string? material = null)
        {
            if (material == null)
                material = Material;

            Side side = new Side();
            side.Plane = plane;

            Axis facingAxis = GetFacingAxis(plane.ComputeNormal());

            switch(facingAxis)
            {
                case Axis.X:
                    side.UAxis = new UVAxis(new Vec3(0, 1, 0));
                    side.VAxis = new UVAxis(new Vec3(0, 0, -1));
                    break;
                case Axis.Y:
                    side.UAxis = new UVAxis(new Vec3(1, 0, 0));
                    side.VAxis = new UVAxis(new Vec3(0, 0, -1));
                    break;
                case Axis.Z:
                    side.UAxis = new UVAxis(new Vec3(1, 0, 0));
                    side.VAxis = new UVAxis(new Vec3(0, -1, 0));
                    break;
            }
            side.LightmapScale = LightmapScale;
            if (material != null)
                side.Material = material;
            AddSide(side);
        }

        public void AddBox(Vec3 min, Vec3 size, string? mat = null, int lightmapScale = 8)
        {
            Vec3 max = min + size;

            AddSide(new Plane(min.X, max.Y, max.Z, max.X, max.Y, max.Z, max.X, min.Y, max.Z));
            AddSide(new Plane(min.X, min.Y, min.Z, max.X, min.Y, min.Z, max.X, max.Y, min.Z));
            AddSide(new Plane(min.X, max.Y, max.Z, min.X, min.Y, max.Z, min.X, min.Y, min.Z));
            AddSide(new Plane(max.X, max.Y, min.Z, max.X, min.Y, min.Z, max.X, min.Y, max.Z));
            AddSide(new Plane(max.X, max.Y, max.Z, min.X, max.Y, max.Z, min.X, max.Y, min.Z));
            AddSide(new Plane(max.X, min.Y, min.Z, min.X, min.Y, min.Z, min.X, min.Y, max.Z));
        }

        public void AddThickenedQuad(Vec3 v1, Vec3 v2, Vec3 v3, Vec3 v4, double thickness, double inset = 0)
        {
            // PLEEASSE don't make me have to think about this math ever again
            string? frontMat = Material;
            string? backMat = Material2 != null ? Material2 : frontMat;

            // Compute the normal of the plane defined by the quad
            Vec3 normal = (v2 - v1).Cross(v3 - v1).Normalized();

            // Compute the offset for the thickness
            Vec3 offset = normal * thickness;

            // Define the vertices of the cuboid
            Vec3 v1_thick = v1 + offset;
            Vec3 v2_thick = v2 + offset;
            Vec3 v3_thick = v3 + offset;
            Vec3 v4_thick = v4 + offset;

            if (inset > 0)
            {
                Vec3 v1_inset = (v4 - v1).Normalized();
                v1_inset += (v2 - v1).Normalized();
                v1_thick += v1_inset * inset;

                Vec3 v2_inset = (v1 - v2).Normalized();
                v2_inset += (v3 - v2).Normalized();
                v2_thick += v2_inset * inset;

                Vec3 v3_inset = (v2 - v3).Normalized();
                v3_inset += (v4 - v3).Normalized();
                v3_thick += v3_inset * inset;

                Vec3 v4_inset = (v3 - v4).Normalized();
                v4_inset += (v1 - v4).Normalized();
                v4_thick += v4_inset * inset;
            }


            // Add the six sides of the cuboid
            AddSide(new Plane(v1, v2, v3), frontMat); // Front face

            // Only add back face if bevel didn't eliminate it.
            if (!AreAnyVecsEqual(v1_thick, v2_thick, v3_thick, .5))
                AddSide(new Plane(v1_thick, v3_thick, v2_thick), backMat); // Back face

            AddSide(new Plane(v1, v1_thick, v2_thick), backMat); // Side face 1
            AddSide(new Plane(v2, v2_thick, v3_thick), backMat); // Side face 2
            AddSide(new Plane(v3, v3_thick, v4_thick), backMat); // Side face 3
            AddSide(new Plane(v4, v4_thick, v1_thick), backMat); // Side face 4
        }

        public void AddThickenedQuad(in Quad quad, double thickness, double inset = 0)
        {
            AddThickenedQuad(quad.Vert1.ToVec3(), quad.Vert2.ToVec3(), quad.Vert3.ToVec3(), quad.Vert4.ToVec3(), thickness, inset);
        }

        private static Axis GetFacingAxis(Vec3 vec)
        {
            vec.Normalize();
            if (Math.Abs(vec.Dot(new Vec3(0, 0, 1))) >= 0.5)
                return Axis.Z;
            else if (Math.Abs(vec.Dot(new Vec3(0, 1, 0))) >= 0.5)
                return Axis.Y;
            else
                return Axis.X;
        }

        public Solid Build()
        {
            Solid solid = new Solid();
            solid.Id = _vmfBuilder._solidCount++;
            solid.Sides.AddRange(_sides);
            return solid;
        }

        private static bool AreAnyVecsEqual(Vec3 vec1, Vec3 vec2, Vec3 vec3, double epsilon)
        {
            double epsilonSquared = epsilon * epsilon;
            return (vec1.SquaredDistanceTo(vec2) < epsilonSquared)
                || (vec1.SquaredDistanceTo(vec3) < epsilonSquared)
                || (vec2.SquaredDistanceTo(vec3) < epsilonSquared);
        }
    }
}