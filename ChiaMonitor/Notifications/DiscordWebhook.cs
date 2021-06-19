using Microsoft.Extensions.Logging;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;

namespace ChiaMonitor.Notifications
{
    public class DiscordWebhook : INotifier, IDisposable
    {
        public string Title { get; set; }
        private string WebHookUrl { get; set; }
        private readonly WebClient dWebClient;
        private static NameValueCollection discord = new NameValueCollection();
        public string ProfilePicture { get; set; }

        public DiscordWebhook(string webHookUrl)
        {
            dWebClient = new WebClient();
            WebHookUrl = webHookUrl;
        }

        public bool IsEnable()
        {
            return !String.IsNullOrEmpty(WebHookUrl);
        }

        public void Notify(LogLevel level, string message)
        {
            if (!IsEnable())
                return;

            message = Regex.Replace(message, @"[^\u0000-\u007F]+", string.Empty);

            if (!String.IsNullOrEmpty(Title))
            {
                discord.Add("username", Title);
            }
            discord.Add("avatar_url", ProfilePicture);
            discord.Add("content", message);

            dWebClient.UploadValues(WebHookUrl, discord);

        }

        public void Dispose()
        {
            dWebClient.Dispose();
        }
    }
}
