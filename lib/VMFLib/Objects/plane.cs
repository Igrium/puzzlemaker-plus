namespace VMFLib.Objects;

public record struct Plane
{
    //public Vec3[] Vertices;
    public Vec3 Vert1;
    public Vec3 Vert2;
    public Vec3 Vert3;

    public Plane(string plane)
    {
        string[] planeVerts = plane.Split(new []{'(', ')'}, StringSplitOptions.RemoveEmptyEntries);

        Vert1 = new Vec3(planeVerts[0]);
        Vert2 = new Vec3(planeVerts[1]);
        Vert3 = new Vec3(planeVerts[2]);

    }

    public Plane(Vec3 vert1, Vec3 vert2, Vec3 vert3)
    {
        Vert1 = vert1;
        Vert2 = vert2;
        Vert3 = vert3;
    }

    public Plane(double x1, double y1, double z1, double x2, double y2, double z2, double x3, double y3, double z3)
    {
        Vert1 = new Vec3(x1, y1, z2);
        Vert2 = new Vec3(x2, y2, z2);
        Vert3 = new Vec3(x3, y3, z3);
    }

    public Plane()
    {
        Vert1 = default;
        Vert2 = default;
        Vert3 = default;
    }

    public Vec3 ComputeNormal()
    {
        Vec3 edge2 = Vert2 - Vert1;
        Vec3 edge1 = Vert3 - Vert1;
        return edge1.Cross(edge2).Normalized();
    }

    public override string ToString()
    {
        return $"{Vert1.ToSpecialString(1)} {Vert2.ToSpecialString(1)} {Vert3.ToSpecialString(1)} ";
    }
}

public record struct UVAxis
{
    public Vec3 XYZ;

    public double Translation;
    public double Scaling;

    public UVAxis(string UVAxis)
    {
        var points = UVAxis.Replace("[", "").Replace("]", "").Split(' ');
        XYZ = new Vec3(double.Parse(points[0]), double.Parse(points[1]), double.Parse(points[2]));
        Translation = 0;
        Scaling = 0;
    }

    public UVAxis(Vec3 xyz, double translation = 0.0, double scaling = 0.25)
    {
        XYZ = xyz;
        Translation = translation;
        Scaling = scaling;
    }

    public UVAxis()
    {
        XYZ = new Vec3();
        Translation = 0;
        Scaling = 0;
    }

    public override string ToString()
    {
        return $"[{XYZ.ToString()} {Translation}] {Scaling}";
    }
}

public class DispRows
{
    public Dictionary<int, List<Vec3>> RowNormals = new Dictionary<int, List<Vec3>>();
    public Dictionary<int, List<Vec3>> RowOffsetNormals = new Dictionary<int, List<Vec3>>();
    
    public Dictionary<int, List<double>> RowDistances = new Dictionary<int, List<double>>();
    public Dictionary<int, List<Vec3>> RowOffsets = new Dictionary<int, List<Vec3>>();
    public Dictionary<int, List<double>> RowAlphas = new Dictionary<int, List<double>>();
    public Dictionary<int, List<int>> RowTriangleTags = new Dictionary<int, List<int>>();
    
    public Dictionary<int, List<int>> AllowedVerts = new Dictionary<int, List<int>>();
    
    public DispRows()
    {
    }

    public DispRows(string normals, string distances, string offsets, string alphas, string tritags, string allowedVerts)
    {
        ParseNormals(normals.Trim());
        ParseDistances(distances.Trim());
        ParseOffsets(offsets.Trim());
        ParseAlpha(alphas.Trim());
        ParseTriangleTag(tritags.Trim());
        ParseAllowedVerts(allowedVerts.Trim());
    }

    public void ParseNormals(string normals)
    {
        string[] rows = normals.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int row = 0; row != rows.Length; row++)
        {
            string currentNorm = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentNorm = currentNorm.Trim('"');

            //Please forgive me for what I am about to do
            List<Vec3> normalVerts = new List<Vec3>();
            var splitNorm = currentNorm.Split(' ');

            for (int i = 0; i < splitNorm.Length;)
            {
                double x = double.Parse(splitNorm[0 + i]);
                double y = double.Parse(splitNorm[1 + i]);
                double z = double.Parse(splitNorm[2 + i]);
                Vec3 vert = new Vec3(x, y, z);
                normalVerts.Add(vert);
                i += 3;
            }

            RowNormals.Add(row, normalVerts);
        }
    }
    
    public void ParseOffsetNormals(string normals)
    {
        string[] rows = normals.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int row = 0; row != rows.Length; row++)
        {
            string currentNorm = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentNorm = currentNorm.Trim('"');
            
            //Please forgive me for what I am about to do
            List<Vec3> normalVerts = new List<Vec3>();
            var splitNorm = currentNorm.Split(' ');

            for (int i = 0; i < splitNorm.Length;)
            {
                double x = double.Parse(splitNorm[0 + i]);
                double y = double.Parse(splitNorm[1 + i]);
                double z = double.Parse(splitNorm[2 + i]);
                Vec3 vert = new Vec3(x, y, z);
                normalVerts.Add(vert);
                i += 3;
            }
            RowOffsetNormals.Add(row, normalVerts);
        }
    }
    
    public void ParseDistances(string distances)
    {
        string[] rows = distances.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int row = 0; row != rows.Length; row++)
        {
            string currentDist = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentDist = currentDist.Trim('"');

            //TODO: This is an awful way to do this!
            List<double> distanceRows = new List<double>();
            foreach (string dist in currentDist.Split(' '))
            {
                distanceRows.Add(double.Parse(dist));
            }

            RowDistances.Add(row, distanceRows);
        }
    }

    public void ParseOffsets(string offsets)
    {
        string[] rows = offsets.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int row = 0; row != rows.Length; row++)
        {
            string currentOffset = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentOffset = currentOffset.Trim('"');
            
            //Please forgive me for what I am about to do
            List<Vec3> offsetRows = new List<Vec3>();
            var splitOffset = currentOffset.Split(' ');

            for (int i = 0; i < splitOffset.Length;)
            {
                double x = double.Parse(splitOffset[0 + i]);
                double y = double.Parse(splitOffset[1 + i]);
                double z = double.Parse(splitOffset[2 + i]);
                Vec3 vert = new Vec3(x, y, z);
                offsetRows.Add(vert);
                i += 3;
            }
            
            RowOffsets.Add(row, offsetRows);
        }
    }
    
    public void ParseAlpha(string alpha)
    {
        string[] rows = alpha.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int row = 0; row != rows.Length; row++)
        {
            string currentAlpha = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentAlpha = currentAlpha.Trim('"');
            
            //TODO: This is an awful way to do this!
            List<double> alphaRows = new List<double>();
            foreach (string dist in currentAlpha.Split(' '))
            {
                alphaRows.Add(double.Parse(dist));
            }
            
            RowAlphas.Add(row, alphaRows);
        }
    }
    
    public void ParseTriangleTag(string alpha)
    {
        string[] rows = alpha.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);;
        for (int row = 0; row != rows.Length; row++)
        {
            string currentAlpha = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentAlpha = currentAlpha.Trim('"');
            
            //TODO: This is an awful way to do this!
            List<int> distanceRows = new List<int>();
            foreach (string dist in currentAlpha.Split(' '))
            {
                distanceRows.Add(int.Parse(dist));
            }
            
            RowTriangleTags.Add(row, distanceRows);
        }
    }
    
    public void ParseAllowedVerts(string av)
    {
        string[] rows = av.Split(new []{'\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int row = 0; row != rows.Length; row++)
        {
            int idx = int.Parse(rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[0].Trim('"')); //TODO: Figure out what this actually is?
            string currentAv = rows[row].Split(new []{"\" \""}, StringSplitOptions.RemoveEmptyEntries)[1];
            currentAv = currentAv.Trim('"');
            
            //TODO: This is an awful way to do this!
            List<int> distanceRows = new List<int>();
            foreach (string dist in currentAv.Split(' '))
            {
                distanceRows.Add(int.Parse(dist));
            }
            
            AllowedVerts.Add(idx, distanceRows);
        }
    }
}