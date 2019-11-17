using System.ComponentModel;

namespace StandardValidator.Model
{
    class MyStandard: INotifyPropertyChanged
    {
        public int ID { get; set; }
        public string CurrentStandard { get; set; }
        public string CurrentStandardNumber { get; set; }
        public string LatestStandard { get; set; }
        public string Note { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
