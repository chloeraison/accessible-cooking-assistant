using System;
using System.Threading.Tasks;

/*
VoiceTest is a simple test harness for triggering a one-time voice recognition check.
It creates an instance of VoiceInterface, listens to the microphone, and prints what was heard.
 */
public static class VoiceTest
{
    public static async Task RunTestAsync()
    {
        // Create an instance of the voice interface - credentials handled internally
        var voiceInterface = new VoiceInterface();

        // Prompt user to speak
        Console.WriteLine("Say something to test!");

        // Await a single voice command from the user
        string? result = await voiceInterface.ListenOnceAsync();

        // Display the result if any speech was captured
        Console.WriteLine(!string.IsNullOrEmpty(result) ? $"You said: {result}" : "Speech could not be recognised.");
        Console.WriteLine("Test complete.");

        // Prevent console from closing immediately
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}
