using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
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

        public async Task GetMessages()
        {
            await RefreshAccessToken();

            var uri = "https://www.googleapis.com/gmail/v1/users/me/messages?includeSpamTrash=true";
            var json = await Get(uri);
            var model = JsonConvert.DeserializeObject<MessagesResultModel>(json);
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
