using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using System.Diagnostics;
using Xabe.FFmpeg;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace azure_chat_bot
{
    public static class Config
    {
        public static string ffmpegPath = "ffmpeg.exe";
        public static string azureKey = "azureKey";
        public static string azureRegion = "westeurope";
        public static string token = "TELEGRAM TOKEN";
        public static string voiceFile = "voice.ogg";
        public static string voiceFileWAV = "voice.wav";
    }

    public static class FileHelper
    {
        /// <summary>
        /// Pobiera plik dźwiękowy i zapisuje na dysku
        /// </summary>
        /// <param name="download_url">link</param>
        /// <param name="fileName">imię pliku</param>
        /// <returns>filename</returns>
        public static string DownloadVoiceFile(string download_url, string fileName)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(download_url), fileName);
                }
                return fileName;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Konwertuje plik dźwiękowy do formatu .wav
        /// </summary>
        /// <param name="inputFileName">plik do konwertowania</param>
        /// <param name="outputFileName">plik do zapisywania</param>
        /// <returns>bool czy operacja udała się</returns>
        public static bool ConvertToWav(string inputFileName, string outputFileName)
        {
            try
            {
                //If file exists then delete
                try
                {
                    File.Delete(outputFileName);
                }
                catch { }


                //Convert using ffmpeg
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(Config.ffmpegPath);
                //psi.Arguments = " -i voice.ogg voice.wav";
                psi.Arguments = " -i " + inputFileName + " " + outputFileName;
                psi.RedirectStandardOutput = true;
                psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                psi.UseShellExecute = false;
                System.Diagnostics.Process ischk;
                ischk = System.Diagnostics.Process.Start(psi);
                ischk.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Zapisuje statystyki do pliku textowego
        /// </summary>
        public static void Save(List<Stats> stats)
        {
            string jsonFile = JsonConvert.SerializeObject(stats);
            File.WriteAllText("stats.json", jsonFile);
        }

        /// <summary>
        ///  Wczytuje statystyki z pliku
        /// </summary>
        /// <returns>zwraca zczytany obiekt statystyk</returns>
        public static List<Stats> ReadStats()
        {
            try
            {
                string dataString = File.ReadAllText("stats.json");
                var stats = JsonConvert.DeserializeObject<List<Stats>>(dataString);
                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new List<Stats>();
            }
        }
    }
    class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient(Config.token);
        public static List<Stats> Stats;

        static void Main(string[] args)
        {
            //Read stats from file
            Stats = FileHelper.ReadStats();

            var me = Bot.GetMeAsync().Result;
            Console.Title = me.Username;

            Bot.OnMessage += OnMessageReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            Bot.StopReceiving();
        }

        //Store stats
        private static void OnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            long userId = messageEventArgs.Message.From.Id;

            var statsToday = Stats.Where(s => s.Date == DateTime.Now.Date).FirstOrDefault();

            if (statsToday == null)
            {
                //Create new day
                Stats.Add(new Stats
                {
                    Date = DateTime.UtcNow.Date,
                    UserId = new List<long>() { userId }
                });
            }
            else
            {
                if (!statsToday.UserId.Contains(userId))
                {
                    statsToday.UserId.Add(userId);
                };
            }

            //Save to file
            FileHelper.Save(Stats);
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            if (message == null) return;
            switch (message.Type)
            {
                case MessageType.Text:
                    if (message.Text == "/statistics" && message.From.Id == 00000000000)
                    {
                        string statsString = Stats.Any() ? "*Statistics*:\n" : "Stats is empty";
                        foreach (var stat in Stats.OrderByDescending(s => s.Date))
                        {
                            statsString += $"{stat.Date.ToString("dd/MM/yy")} - `{stat.UserId.Count}` active users\n";
                        }

                        await Bot.SendTextMessageAsync(message.Chat.Id, statsString, ParseMode.Markdown);
                    }
                    else
                        await Bot.SendTextMessageAsync(message.Chat.Id, message.Text.ToUpper());
                    break;
                case MessageType.Voice:
                    //save file to ogg
                    var duration = message.Voice.Duration;
                    var test = await Bot.GetFileAsync(message.Voice.FileId);
                    var download_url = @"https://api.telegram.org/file/bot" + Config.token + "/" + test.FilePath;
                    var filePath = FileHelper.DownloadVoiceFile(download_url, Config.voiceFile);

                    //convert to WAV
                    FileHelper.ConvertToWav(Config.voiceFile, Config.voiceFileWAV);

                    //recognize speech and return recognized text
                    string recognizedText = await CognitiveFunctions.Recognize();

                    await Bot.SendTextMessageAsync(message.Chat.Id, recognizedText);
                    break;

                default:
                    await Bot.SendTextMessageAsync(message.Chat.Id, "Please, send voice file");
                    break;
            }
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message);
        }
    }

    /// <summary>
    /// Reprezentacja statystyk
    /// </summary>
    public class Stats
    {
        public DateTime Date { get; set; }
        public List<long> UserId { get; set; } = new List<long>();
    }
}

