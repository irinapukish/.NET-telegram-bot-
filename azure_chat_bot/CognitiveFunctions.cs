using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace azure_chat_bot
{
    public static class CognitiveFunctions
    {
        /// <summary>
        /// Rozpoznaje text z pliku dźwiękowego
        /// </summary>
        /// <returns>Zwraca text rozpoznany w Azure voice recognition</returns>
        public static async Task<string> Recognize()
        {
            string returnString = "";
            // Creates an instance of a speech config with specified subscription key and service region.
            var config = SpeechConfig.FromSubscription(Config.azureKey, Config.azureRegion);
            var transcriptionStringBuilder = new StringBuilder();

            using (var audioInput = AudioConfig.FromWavFileInput(Config.voiceFileWAV))
            {
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    // Subscribes to events.  
                    recognizer.Recognizing += (sender, eventargs) =>
                    {
                        //TODO: Handle recognized intermediate result  
                    };

                    recognizer.Recognized += (sender, eventargs) =>
                    {
                        if (eventargs.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            transcriptionStringBuilder.Append(eventargs.Result.Text);
                            returnString = eventargs.Result.Text;
                        }
                        else if (eventargs.Result.Reason == ResultReason.NoMatch)
                        {
                            returnString = "cant recognize";
                        }
                    };

                    recognizer.Canceled += (sender, eventargs) =>
                    {
                        if (eventargs.Reason == CancellationReason.Error)
                        {
                            //TODO: Handle error  
                        }

                        if (eventargs.Reason == CancellationReason.EndOfStream)
                        {
                            Console.WriteLine(transcriptionStringBuilder.ToString());
                        }

                        //taskCompleteionSource.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (sender, eventargs) =>
                    {
                        //Started recognition session  
                    };

                    recognizer.SessionStopped += (sender, eventargs) =>
                    {
                        //Ended recognition session  
                        //taskCompleteionSource.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.  
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for end.  
                    while (returnString == "") { }

                    // Stops recognition.  
                    await recognizer.StopContinuousRecognitionAsync();

                    return returnString;
                }
            }
        }
    }
}
