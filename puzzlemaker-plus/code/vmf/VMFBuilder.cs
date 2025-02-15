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

    public class SolidBuilder
    {
        private readonly VMFBuilder _vmfBuilder;

        protected readonly List<Side> _sides = new();

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
        /// <param name="plane"></param>
        public void AddSide(in Plane plane, string? material = null)
        {
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
            side.LightmapScale = 8;
            if (material != null)
                side.Material = material;
            AddSide(side);
        }

        public void AddBox(Vec3 min, Vec3 size)
        {
            Vec3 max = min + size;
            string mat = "DEV/DEV_MEASUREWALL01A";

            AddSide(new Plane(min.X, max.Y, max.Z, max.X, max.Y, max.Z, max.X, min.Y, max.Z), mat);
            AddSide(new Plane(min.X, min.Y, min.Z, max.X, min.Y, min.Z, max.X, max.Y, min.Z), mat);
            AddSide(new Plane(min.X, max.Y, max.Z, min.X, min.Y, max.Z, min.X, min.Y, min.Z), mat);
            AddSide(new Plane(max.X, max.Y, min.Z, max.X, min.Y, min.Z, max.X, min.Y, max.Z), mat);
            AddSide(new Plane(max.X, max.Y, max.Z, min.X, max.Y, max.Z, min.X, max.Y, min.Z), mat);
            AddSide(new Plane(max.X, min.Y, min.Z, min.X, min.Y, min.Z, min.X, min.Y, max.Z), mat);

            // Create the 6 sides of the box using a single plane for each face
            //AddSide(new Plane(new Vec3(pos.X, pos.Y, pos.Z), new Vec3(max.X, max.Y, pos.Z), new Vec3(max.X, pos.Y, pos.Z))); // Bottom
            //AddSide(new Plane(new Vec3(pos.X, pos.Y, max.Z), new Vec3(max.X, max.Y, max.Z), new Vec3(max.X, pos.Y, max.Z))); // Top
            //AddSide(new Plane(new Vec3(pos.X, pos.Y, pos.Z), new Vec3(pos.X, max.Y, max.Z), new Vec3(pos.X, pos.Y, max.Z))); // Left
            //AddSide(new Plane(new Vec3(max.X, pos.Y, pos.Z), new Vec3(max.X, max.Y, max.Z), new Vec3(max.X, pos.Y, max.Z))); // Right
            //AddSide(new Plane(new Vec3(pos.X, pos.Y, pos.Z), new Vec3(max.X, pos.Y, max.Z), new Vec3(max.X, pos.Y, pos.Z))); // Front
            //AddSide(new Plane(new Vec3(pos.X, max.Y, pos.Z), new Vec3(max.X, max.Y, max.Z), new Vec3(max.X, max.Y, pos.Z))); // Back
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
    }
}