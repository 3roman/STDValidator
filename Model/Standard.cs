using System.ComponentModel;

namespace StandardValidator.Model
{
    class MyStandard: INotifyPropertyChanged
    {
        public int ID { get; set; }
        public string CurrentStandardName { get; set; }
        public string CurrentStandardNumber { get; set; }
        public string LatestStandardNumber { get; set; }
        public string State { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
