using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace ChiaMonitor.Notifications
{
    public class LineNotify : INotifier
    {
        private string Token { get; set; }

        public LineNotify(string token)
        {
            Token = token;
        }

        public bool IsEnable()
        {
            return !String.IsNullOrEmpty(Token);
        }

        public void Notify(string message)
        {
            Notify(LogLevel.Information, message);
        }

        public void Notify(LogLevel level, string message)
        {
            Console.WriteLine(level.ToString() + " : " + message);

            if (!IsEnable())
                return;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                var postData = string.Format("message={0}", message);
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
                Console.WriteLine(e.Message);
            }
        }
    }
}
