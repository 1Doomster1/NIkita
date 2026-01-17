using System;
using System.Text.Json.Serialization;

namespace NikitaMessenger
{
    public class NetworkMessage
    {
        public string Type { get; set; }
        public string SenderId { get; set; }
        public string SenderName { get; set; }
        public string Content { get; set; }
        public string ChatId { get; set; }
        public string Data { get; set; }

        [JsonIgnore]
        public DateTime Timestamp { get; set; }
    }
}