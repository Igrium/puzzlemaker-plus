namespace VMFLib.Objects;

public record struct Vec2
{
    public int X;
    public int Y;

    public Vec2(string str)
    {
        var property = str.Trim('[', ']').Split(' ');
        X = int.Parse(property[0]);
        Y = int.Parse(property[1]);
    }

    public Vec2(int x, int y, int z)
    {
        X = x;
        Y = y;
    }

    public Vec2()
    {
        X = 0;
        Y = 0;
    }
}