using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace PuzzlemakerPlus;

[GlobalClass]
public partial class ThreadingTest : Node
{
    public void DoThreadingTest()
    {
        GD.Print("Starting thread: " + System.Environment.CurrentManagedThreadId);

        //var task = Count();
        var task = Task.Run(Count);
        //task.Start();
        GD.Print("The other thread is now counting.");
    }

    private async Task<object?> Count()
    {
        for (int i = 0; i < 10; i++)
        {
            GD.Print($"Thread {System.Environment.CurrentManagedThreadId} says {i}");
            await Task.Delay(100);
        }
        return null;
    }
}
