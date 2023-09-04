using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace image_classifier
{
    class Program
    {
        static CustomVisionPredictionClient prediction_client;
        private static SpeechConfig speechConfig;

        static async Task Main(string[] args)
        {
            try
            {
                // Configuration settings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string prediction_endpoint = configuration["PredictionEndpoint"];
                string prediction_key = configuration["PredictionKey"];
                string cogSvcKey = configuration["CognitiveServiceKey"];
                string cogSvcRegion = configuration["CognitiveServiceRegion"];
                Guid project_id = Guid.Parse(configuration["ProjectID"]);
                string model_name = configuration["ModelName"];

                // Authenticate client
                prediction_client = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(prediction_key))
                {
                    Endpoint = prediction_endpoint
                };

                try 
                {
                    // Configure speech service
                    speechConfig = SpeechConfig.FromSubscription(cogSvcKey, cogSvcRegion);

                    // Configure voice
                    speechConfig.SpeechSynthesisVoiceName = "en-IE-EmilyNeural";
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }

                string keepGoing = "";

                do
                {
                    // Classifying images
                    await Speak("Please enter the name of the picture you wish to classify");
                    string imgName = Console.ReadLine();

                    var files = Directory.GetFiles("images", imgName + "?.jpg");
                    foreach(var image in files)
                    {
                        await Speak("Analyzing image.");
                        MemoryStream image_data = new MemoryStream(File.ReadAllBytes(image));
                        var result = prediction_client.ClassifyImage(project_id, model_name, image_data);

                        foreach (var prediction in result.Predictions)
                        {
                            if (prediction.Probability > 0.5)
                            {
                                await Speak($"I'm {prediction.Probability:P2} sure that is a {prediction.TagName}!");
                            }
                        }
                    }
                    System.Console.WriteLine("Press 1 to analyze a different image.");
                    System.Console.WriteLine("Press 2 to exit.");

                    string input = Console.ReadLine();
                    if(input == "2")
                    {
                        keepGoing = "no";
                    }
                }
                while (keepGoing != "no");

                await Speak("Goodbye!");

            }
            catch(Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
        static async Task Speak(string answer)
        {
            string response = answer;

            Console.WriteLine(response);
            using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);

            // Synthesize spoken output
            SpeechSynthesisResult speak = await speechSynthesizer.SpeakTextAsync(response);
        }
    }
}