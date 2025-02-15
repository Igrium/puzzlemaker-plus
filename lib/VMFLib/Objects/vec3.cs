namespace VMFLib.Objects
{
    public record struct RGB
    {
        public int Red;
        public int Green;
        public int Blue;

        public RGB(string str)
        {
            var property = str.Split(' ');
            Red = int.Parse(property[0]);
            Green = int.Parse(property[1]);
            Blue = int.Parse(property[2]);
        }

        public RGB()
        {
            Red = 255;
            Green = 255;
            Blue = 255;
        }

        public RGB(int red, int green, int blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        public readonly override string ToString()
        {
            return $"{Red} {Green} {Blue}";
        }
    }
    
    public record struct Vec3
    {
        public double X;
        public double Y;
        public double Z;
        
        public Vec3(string str)
        {
            var property = str.Trim('[', ']').Split(' ');
            X = double.Parse(property[0]);
            Y = double.Parse(property[1]);
            Z = double.Parse(property[2]);
        }

        public Vec3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public readonly override string ToString()
        {
            return $"{X} {Y} {Z}";
        }

        /// <summary>
        /// Same as ToString() except is capable of returning special formats(e.g "({x} {y} {z})")
        /// </summary>
        /// <param name="flags">0 = ToString() 1 = surrounded () 2 = surrounded []</param>
        /// <returns></returns>
        public readonly string ToSpecialString(int flags)
        {
            switch (flags)
            {
                case 2:
                {
                    return $"[{ToString()}]";
                }
                case 1:
                {
                    return $"({ToString()})";
                }
                default:
                {
                    return ToString();
                }
            }
        }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, double b) => new Vec3(a.X * b, a.Y * b, a.Z * b);

        public readonly double Dot(Vec3 other)
        {
            return this.X * other.X + this.Y * other.Y + this.Z * other.Z;
        }

        public readonly Vec3 Cross(Vec3 other)
        {
            return new Vec3(
                this.Y * other.Z - this.Z * other.Y,
                this.Z * other.X - this.X * other.Z,
                this.X * other.Y - this.Y * other.X
            );
        }

        public readonly double LengthSquared()
        {
            return X * X + Y * Y + Z * Z;
        }

        public readonly double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        public void Normalize()
        {
            double len = Length();
            X = X / len;
            Y = Y / len;
            Z = Z / len;
        }

        public readonly Vec3 Normalized()
        {
            Vec3 result = this;
            result.Normalize();
            return result;
        }
    }
}