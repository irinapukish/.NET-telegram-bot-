using System;
using Xunit;
using azure_chat_bot;

namespace azure_chat_bot.Tests
{
    public class azure_chat_bot_Tests
    {
        [Fact]
        public void DownloadFile()
        {
            // Arrange
            string imageUrl = "https://wsiz.rzeszow.pl/wp-content/uploads/2018/12/Rekrutacja1.jpg";
            string imageFile = "testimg.jpg";

            // Act
            var result = FileHelper.DownloadVoiceFile(imageUrl, imageFile);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void DownloadVoiceFileNotNull()
        {
            // Arrange
            string voiceFile = "voice.ogg";
            string voiceFileWAV = "voice.wav";

            // Act
            var result = FileHelper.ConvertToWav(voiceFile, voiceFileWAV);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void recognizerTest()
        {
            // Arrange
            

            // Act
            var result = CognitiveFunctions.Recognize().GetAwaiter().GetResult();

            // Assert
            Assert.NotEqual("", result);
        }
    }
}
