using System.ComponentModel;

namespace CitizenRequestsAdmin.Models
{
    public class ChatMessage : INotifyPropertyChanged
    {
        public int ResponseId { get; set; }
        public bool IsFromCitizen { get; set; }
        public string SenderName { get; set; }
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }

        public string SenderInfo => IsFromCitizen
            ? $"[Гражданин] {SenderName}"
            : $"[Администратор] {SenderName}";

        public string FormattedTimestamp => Timestamp.ToString("HH:mm dd.MMM");

        public event PropertyChangedEventHandler PropertyChanged;
    }
}