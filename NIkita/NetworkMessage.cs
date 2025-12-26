using System;
using System.Text.Json.Serialization;

namespace NikitaMicrosoft
{
    public class NetworkMessage
    {
        public string Type { get; set; }          // Тип сообщения
        public string SenderId { get; set; }      // ID отправителя
        public string SenderName { get; set; }    // Имя отправителя
        public string Content { get; set; }       // Содержимое
        public string ChatId { get; set; }        // ID чата/комнаты
        public string Data { get; set; }          // Дополнительные данные

        [JsonIgnore]
        public DateTime Timestamp { get; set; }   // Время отправки
    }
}