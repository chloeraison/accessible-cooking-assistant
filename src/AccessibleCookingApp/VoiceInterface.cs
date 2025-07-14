using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using DotNetEnv;

/*
 VoiceInterface class handles microphone input and converts spoken 
 words to text using Microsoft Azure Cognitive Services.
 */
public class VoiceInterface
{
    // Azure Speech Service credentials - loaded from .env
    private readonly string subscriptionKey;
    private readonly string serviceRegion;

    // Main configuration object for the Azure speech recogniser
    private readonly SpeechConfig speechConfig;

    /*
    Constructor: Sets up the speech configuration
    by loading environment variables from a .env file
    using DotNetEnv. If variables are missing, it will throw.
    */
    public VoiceInterface()
    {
        // Define a relative path to the .env file for portability
        var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");

        // Load the .env file (must contain AZURE_SPEECH_KEY and AZURE_REGION)
        Env.Load(envPath);

        // Retrieve the Azure Speech Service credentials from environment
        subscriptionKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY")
                          ?? throw new ArgumentException("Missing AZURE_SPEECH_KEY");

        // Fallback region if not specified in .env
        serviceRegion = Environment.GetEnvironmentVariable("AZURE_REGION") ?? "uksouth";

        // Initialise the Azure speech configuration with loaded credentials
        speechConfig = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
    }

    /*
    ListenOnceAsync() activates the microphone
    and waits for a single voice command.
    It returns the recognised speech as a string,
    or null if no valid speech is detected.
    */
    public async Task<string?> ListenOnceAsync()
    {
        // Set up the recogniser with the current speech config
        using var recogniser = new SpeechRecognizer(speechConfig);

        Console.WriteLine("Speak now...");

        // Start listening and wait for a singe result
        var result = await recogniser.RecognizeOnceAsync();

        // Handle successful recognition
        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"Recognised: {result.Text}");
            return result.Text;
        }
        // Handle unrecognisable speech (e.g. silence or noise)
        else if (result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("No speech recognised.");
        }
        // Handle recognition cancellation (e.g. due to error)
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            Console.WriteLine($"Cancelled: {cancellation.Reason}");

            // If the cancellation was caused by an error, print the details
            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"Error details: {cancellation.ErrorDetails}");
            }
        }

        // Return null if speech was not recognised
        return null;
    }
}
