using NIkita;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.RightsManagement;

namespace NIkita
{

    public class AppViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        public AppViewModel(NikitaDBContext context)
        {
            context.messages.Add(new Message { Date = DateTime.Now, Text = "Hello, world!", User = "Semen" });
            context.SaveChanges();
            foreach (var message in context.messages)
            {
                Messages.Add(message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
