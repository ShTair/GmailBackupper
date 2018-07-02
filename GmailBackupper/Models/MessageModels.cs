using Newtonsoft.Json;

namespace GmailBackupper.Models
{
    public class RawMessageResult
    {
        [JsonProperty("raw")]
        public string Raw { get; set; }
    }

    public class MinMessage
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("labelIds")]
        public string[] LabelIds { get; set; }

        [JsonProperty("internalDate")]
        public long InternalDate { get; set; }

        [JsonProperty("payload")]
        public Payload Payload { get; set; }
    }

    public class Payload
    {
        [JsonProperty("headers")]
        public Header[] Headers { get; set; }
    }

    public class Header
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
