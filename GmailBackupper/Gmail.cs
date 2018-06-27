using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace GmailBackupper
{
    class Gmail
    {
        private string _clientId;
        private string _clientSecret;
        private string _refreshToken;

        private string _accessToken;

        public Gmail(string clientId, string clientSecret, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
        }

        public async Task RefreshAccessToken()
        {
            var uri = "https://www.googleapis.com/oauth2/v4/token";
            var content = $"client_secret={_clientSecret}&grant_type=refresh_token&refresh_token={_refreshToken}&client_id={_clientId}";

            var json = await Post(uri, null, content);
            var model = JsonConvert.DeserializeObject<RefreshAccessTokenResultModel>(json);

            _accessToken = model.AccessToken;
        }

        private async Task<string> Post(string uri, string accessToken, string content = null)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = "POST";
            if (accessToken == null)
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }
            else
            {
                request.ContentType = "application/json";
                request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + accessToken);
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
