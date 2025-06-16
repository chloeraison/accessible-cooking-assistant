using System;
using System.Threading;

class Program {
    static void MAin(string[] args)
    {
        var timer = new SingeTimer();
        timer.Start(1); // Start a 1 minute timer

        while (!timer.IsFinished())
        {
            Console.WriteLine($"Time left: {timer.TimeRemaining():mm\\:ss}");
            Thread.Sleep(1000); // Wait for 1 second
        }
        
        Console.WriteLine("Timer complete!");

    }
}