using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Gmail
    {
        private string _clientId;
        private string _clientSecret;
        private string _refreshToken;

        private string _accessToken;
        private DateTime _nextRefresh;
        private SemaphoreSlim _refreshSem = new SemaphoreSlim(1);

        public Gmail(string clientId, string clientSecret, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
        }

        private async Task MoveToTrash(string id)
        {
            var uri = $"https://www.googleapis.com/gmail/v1/users/me/messages/{id}/trash";
            var json = await Post(uri);
        }

        private async Task<MessageResultModel> GetMessage(string id, string format = MessageFormat.Full)
        {
            var uri = "https://www.googleapis.com/gmail/v1/users/me/messages/" + id;
            if (format != MessageFormat.Full) uri = uri + "?format=" + format;
            var json = await Get(uri);
            return JsonConvert.DeserializeObject<MessageResultModel>(json);
        }

        private async Task GetMessageStr(string id, Func<Stream, Task> func)
        {
            var uri = "https://www.googleapis.com/gmail/v1/users/me/messages/" + id;
            await GetBytes(uri, func);
        }

        private static class MessageFormat
        {
            public const string Full = "full";
            public const string Metadata = "metadata";
            public const string Minimal = "minimal";
            public const string Raw = "raw";
        }

        public MessageEnamerator GetMessageEnamerator()
        {
            return new MessageEnamerator(this);
        }

        private async Task<MessagesResultModel> GetMessages(string pageToken = null)
        {
            await RefreshAccessToken();

            var uri = "https://www.googleapis.com/gmail/v1/users/me/messages?includeSpamTrash=true";
            if (pageToken != null) uri = uri + "&pageToken=" + pageToken;
            var json = await Get(uri);
            return JsonConvert.DeserializeObject<MessagesResultModel>(json);
        }

        public class MessageEnamerator
        {
            private static readonly Regex _base64UrlRegex = new Regex(@"[-_]");

            private Gmail _gmail;
            private MessagesResultModel _current;
            private int _index;

            public string CurrentMessageId => _current.Messages[_index].Id;

            public MessageEnamerator(Gmail gmail) => _gmail = gmail;

            public async Task<bool> Next()
            {
                _index++;
                if (_current == null || _current.Messages.Length <= _index)
                {
                    if (_current != null && _current.NextPageToken == null) return false;

                    _current = await _gmail.GetMessages(_current?.NextPageToken);
                    _index = 0;
                }

                return true;
            }

            public async Task StoreFullMessage(string dstPath)
            {
                using (var stream = File.Create(dstPath))
                {
                    await _gmail.GetMessageStr(CurrentMessageId, async src =>
                    {
                        await src.CopyToAsync(stream);
                    });
                }
            }

            public async Task<byte[]> GetRawMessage()
            {
                var model = await _gmail.GetMessage(CurrentMessageId, MessageFormat.Raw);
                var str = _base64UrlRegex.Replace(model.raw, m => m.Value == "-" ? "+" : "/");
                return Convert.FromBase64String(str);
            }

            public async Task<MessageResultModel> GetMinimalMessage()
            {
                return await _gmail.GetMessage(CurrentMessageId, MessageFormat.Minimal);
            }

            public async Task MoveToTrash()
            {
                await _gmail.MoveToTrash(CurrentMessageId);
            }
        }

        private async Task RefreshAccessToken()
        {
            if (_nextRefresh > DateTime.Now) return;

            try
            {
                await _refreshSem.WaitAsync();
                if (_nextRefresh > DateTime.Now) return;

                _accessToken = null;

                var uri = "https://www.googleapis.com/oauth2/v4/token";
                var content = $"client_secret={_clientSecret}&grant_type=refresh_token&refresh_token={_refreshToken}&client_id={_clientId}";

                var json = await Post(uri, content, false);
                var model = JsonConvert.DeserializeObject<RefreshAccessTokenResultModel>(json);

                _accessToken = model.AccessToken;
                _nextRefresh = DateTime.Now.AddMinutes(30);

                Console.WriteLine("Refreshed AccessToken");
            }
            finally
            {
                _refreshSem.Release();
            }
        }

        private async Task GetBytes(string uri, Func<Stream, Task> func)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = "Get";
            request.ContentType = "application/json";
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _accessToken);

            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                await func(stream);
            }
        }

        private async Task<string> Get(string uri)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = "Get";
            request.ContentType = "application/json";
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _accessToken);

            using (var response = await request.GetResponseAsync())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task<string> Post(string uri, string content = null, bool useAccessToken = true)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = "POST";
            if (useAccessToken)
            {
                request.ContentType = "application/json";
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + _accessToken);
            }
            else
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }

            if (content != null)
            {
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync()))
                {
                    await writer.WriteAsync(content);
                }
            }

            using (var response = await request.GetResponseAsync())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
