using System;
using System.Threading.Tasks;

/*
VoiceTest is a simple test harness for triggering a one-time voice recognition check.
It creates an instance of VoiceInterface, listens to the microphone, and prints what was heard.
 */
class VoiceTest
{
    static async Task Main(string[] args)
    {
        // Create an instance of the voice interface - credentials handled internally
        var voiceInterface = new VoiceInterface();

        // Prompt user to speak
        Console.WriteLine("Say something you'd like to test!");

        // Await a single voice command from the user
        string? result = await voiceInterface.ListenOnceAsync();

        // Display the result if any speech was captured
        if (!string.IsNullOrEmpty(result))
        {
            Console.WriteLine($"You said: {result}");
        }
        else
        {
            Console.WriteLine("Speech could not be recoginised.");
        }

        // Prevent console from closing immediately
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }
}
