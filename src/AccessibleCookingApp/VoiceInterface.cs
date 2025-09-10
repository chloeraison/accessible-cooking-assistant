using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio; // for AudioConfig
using DotNetEnv;
using System.Media; // for SoundPlayer

/*
 VoiceInterface class
 --------------------
 Handles microphone input (speech → text) and also speaks results back (text → speech)
 using Microsoft Azure Cognitive Services.

 **Outcome**
 -> Listen for voice commands and return them as text
 -> Speak app responses out loud for accessibility
 -> Load credentials safely from a .env file (no hard-coding)

 **Plan (top → bottom)**
 1. Load Azure Speech credentials (key + region) from .env
 2. Create a SpeechConfig for both recognition + synthesis
 3. Provide:
    - ListenOnceAsync() : waits for one spoken command and returns the recognised text
    - SpeakAsync(text)  : speaks a line of text back to the user
 4. Expose IsSpeaking so callers can wait until TTS is finished (half-duplex UX)
 */
public class VoiceInterface
{
    // Azure Speech Service credentials - loaded from .env
    private readonly string subscriptionKey;
    private readonly string serviceRegion;

    // Main configuration object for Azure Speech (used for both STT + TTS)
    private readonly SpeechConfig speechConfig;

    // Shared synthesiser for text-to-speech output
    private readonly SpeechSynthesizer synthesizer;

    // Indicates when TTS is playing so callers can wait before listening again
    public bool IsSpeaking { get; private set; } = false;

    /*
     Constructor:
     - Loads environment variables from .env (AZURE_SPEECH_KEY, AZURE_REGION)
     - Sets up SpeechConfig for recognition + synthesis
     - Chooses a clear UK English voice for TTS (can be changed later)
    */
    public VoiceInterface()
    {
        // Define a relative path to the .env file for portability
        var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
        Env.Load(envPath);

        // Retrieve the Azure Speech Service credentials from environment
        subscriptionKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY")
                          ?? throw new ArgumentException("Missing AZURE_SPEECH_KEY");
        serviceRegion = Environment.GetEnvironmentVariable("AZURE_REGION") ?? "uksouth";

        // Initialise the Azure speech configuration with loaded credentials
        speechConfig = SpeechConfig.FromSubscription(subscriptionKey, serviceRegion);
        // Optional: recognition language (helps accuracy)
        speechConfig.SpeechRecognitionLanguage = "en-GB";
        // Optional: pick a natural-sounding voice
        speechConfig.SpeechSynthesisVoiceName = "en-GB-RyanNeural";

        // Route TTS to default speakers explicitly (avoids clipping on some setups)
        var speaker = AudioConfig.FromDefaultSpeakerOutput();
        synthesizer = new SpeechSynthesizer(speechConfig, speaker);
    }

    /*
     ListenOnceAsync()
     -----------------
     Activates the microphone and waits for a single voice command.
     Returns the recognised speech as a string, or null if not recognised.
    */
    public async Task<string?> ListenOnceAsync()
    {
        // If TTS is still playing, wait before opening the mic (half-duplex safety)
        while (IsSpeaking) await Task.Delay(50);

        using var mic = AudioConfig.FromDefaultMicrophoneInput();
        using var recogniser = new SpeechRecognizer(speechConfig, mic);

        Console.WriteLine("Speak now...");

        // Start listening and wait for a single result
        var result = await recogniser.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            Console.WriteLine($"Recognised: {result.Text}");
            return result.Text;
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            Console.WriteLine("No speech recognised.");
        }
        else if (result.Reason == ResultReason.Canceled)
        {
            var cancellation = CancellationDetails.FromResult(result);
            Console.WriteLine($"Cancelled: {cancellation.Reason}");
            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"Error details: {cancellation.ErrorDetails}");
            }
        }

        return null;
    }

    /*
     SpeakAsync()
     ------------
     Speaks a line of text out loud using Azure TTS.
     (Printing to console is done by the caller so we don't double-print.)
    */
    public async Task SpeakAsync(string text)

    {
        if (string.IsNullOrWhiteSpace(text)) return;

        string? tmp = null;
        try
        {
            IsSpeaking = true;

            if (OperatingSystem.IsWindows())
            {
                // --- Buffered path on Windows: synth → WAV → PlaySync (prevents clipping) ---
                tmp = Path.Combine(Path.GetTempPath(), $"aca_tts_{Guid.NewGuid():N}.wav");

                using (var fileOut = AudioConfig.FromWavFileOutput(tmp))
                using (var synthToFile = new SpeechSynthesizer(speechConfig, fileOut))
                {
                    var res = await synthToFile.SpeakTextAsync(text);
                    if (res.Reason != ResultReason.SynthesizingAudioCompleted)
                        return; // fail gracefully
                }

                // SoundPlayer is Windows-only; guard removes CA1416 warnings.
                #pragma warning disable CA1416
                using (var player = new System.Media.SoundPlayer(tmp))
                {
                    player.Load();      // fully buffer
                    player.PlaySync();  // block until finished
                }
                #pragma warning restore CA1416
            }
            else
            {
                // --- Non-Windows: use the normal SDK speaker path ---
                await synthesizer.SpeakTextAsync(text);
            }

            // Small settle so we don't open the mic immediately after TTS
            await Task.Delay(200);
        }
        finally
        {
            IsSpeaking = false;
            if (tmp != null) { try { File.Delete(tmp); } catch { } }
        }
    }
}
