using Newtonsoft.Json;

namespace GmailBackupper
{
    public class RefreshAccessTokenResultModel
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public class MessagesResultModel
    {
        [JsonProperty("messages")]
        public MessagesResultMessageModel[] Messages { get; set; }

        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; set; }

        [JsonProperty("resultSizeEstimate")]
        public int ResultSizeEstimate { get; set; }
    }

    public class MessagesResultMessageModel
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("threadId")]
        public string ThreadId { get; set; }
    }
}
