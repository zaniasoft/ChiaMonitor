using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ChiaMonitor.Notifications
{
    public class LineNotify : INotifier
    {
        private const string LINE_NOTIFY_API = "https://notify-api.line.me/api/notify";
        private string Token { get; set; }
        public string Title { get; set; }

        public LineNotify(string token)
        {
            Token = token;
        }

        public bool IsEnable()
        {
            return !String.IsNullOrEmpty(Token);
        }

        public void Notify(LogLevel level, string message)
        {
            if (!String.IsNullOrEmpty(Title))
            {
                message = Title + "\n" + message;
            }

            if (!IsEnable())
                return;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(LINE_NOTIFY_API);
                var postData = string.Format("message={0}", System.Web.HttpUtility.UrlEncode(message));
                var data = Encoding.UTF8.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Headers.Add("Authorization", "Bearer " + Token);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }

            catch (Exception e)
            {
                Log.Error(e.Message);
                if (e.Message.Contains("401"))
                {
                    Log.Error("Wrong Line Token, Please check your LineToken in config.json");
                }
            }
        }

    }
}
