using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using BGMNALateNightAnimeBotLib.Model;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BGMNALateNightAnimeBotLib
{
    public class Bot
    {
        public Bot(string token)
        {
            BotClient = new TelegramBotClient(token);
        }

        private TelegramBotClient BotClient { get; }

        public async Task<(bool, List<ValidationResult>)> SendInfo(PublishInfo publishInfo)
        {
            var result = new List<ValidationResult>();
            if (!Validator.TryValidateObject(publishInfo, new ValidationContext(publishInfo), result))
                return (false, result);

            var content = $"{publishInfo.ChsName}\n" +
                          $"{publishInfo.JpnName}\n" +
                          $"BGM链接: [{publishInfo.BangumiId}](https://bgm.tv/subject/{publishInfo.BangumiId})\n" +
                          "\n" +
                          $"[下载地址]({publishInfo.DownloadUrl})";

            if (!string.IsNullOrEmpty(publishInfo.SubtitleUrl))
                content += $"\n[字幕地址]({publishInfo.SubtitleUrl})";
            else if (publishInfo.Subtitle.Count > 0)
                using (var ms = new MemoryStream())
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create))
                {
                    foreach (var fileInfo in publishInfo.Subtitle)
                        archive.CreateEntryFromFile(fileInfo.FullName, fileInfo.Name);

                    ms.Seek(0, SeekOrigin.Begin);
                    var fileMsg = await BotClient.SendDocumentAsync(new ChatId(-1001221005295),
                        new InputMedia(ms, "subtitle.zip"), $"{publishInfo.ChsName} 字幕");
                    content += $"\n[字幕地址](https://t.me/bgmna_latenight_anime/{fileMsg.MessageId})";
                }

            await BotClient.SendPhotoAsync(new ChatId(-1001221005295),
                new InputMedia(publishInfo.Cover.ToString()), content, ParseMode.Markdown);

            return (true, null);
        }
    }
}