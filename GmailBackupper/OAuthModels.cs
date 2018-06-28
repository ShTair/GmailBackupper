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

    public class MessageResultModel
    {
        public string id { get; set; }
        public string threadId { get; set; }
        public string[] labelIds { get; set; }
        public string snippet { get; set; }
        public string historyId { get; set; }
        public long internalDate { get; set; }
        public Payload payload { get; set; }
        public int sizeEstimate { get; set; }
        public string raw { get; set; }
    }

    public class Payload
    {
        public string partId { get; set; }
        public string mimeType { get; set; }
        public string filename { get; set; }
        public Header[] headers { get; set; }
        public Body body { get; set; }
        public Part[] parts { get; set; }
    }

    public class Body
    {
        public string attachmentId { get; set; }
        public int size { get; set; }
        public string data { get; set; }
    }

    public class Header
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Part
    {
        public string partId { get; set; }
        public string mimeType { get; set; }
        public string filename { get; set; }
        public Header1[] headers { get; set; }
        public Body1 body { get; set; }
    }

    public class Body1
    {
        public int size { get; set; }
        public string data { get; set; }
        public string attachmentId { get; set; }
    }

    public class Header1
    {
        public string name { get; set; }
        public string value { get; set; }
    }
}
