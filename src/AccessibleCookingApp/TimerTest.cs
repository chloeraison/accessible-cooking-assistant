using System;
using System.Threading;

//A basic test harness to demonstrate and verify functionality of the SingleTimer class.
// This simulates a 10-second countdown timer, printing time remaining every second.
class TimerTest
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting 10-second timer test...");

        // Create and start the timer for approximately 10 seconds (0.166 minutes)
        SingleTimer timer = new SingleTimer();
        timer.Start(10.0 / 60.0); // 10 seconds

        // Loop until the timer finishes
        while (!timer.IsFinished())
        {
            Console.WriteLine($"Time left: {Math.Round(timer.TimeRemaining().TotalSeconds)} seconds");
            Thread.Sleep(1000); //pause for 1 second
        }

        Console.WriteLine("Timer finished!");
    }
}
