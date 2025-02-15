using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuzzlemakerPlus;

public interface IMeshBuilder
{
    public void AddQuad(in Quad quad);

    public void AddQuads(IEnumerable<Quad> quads)
    {
        foreach (var quad in quads)
        {
            AddQuad(in quad);
        }
    }
}
