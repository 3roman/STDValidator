using StandardValidator.Model;
using StandardValidator.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Web;
using System.Text;

namespace StandardValidator.ViewModel
{
    class ViewModelMain : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<MyStandard> Standards { get; set; }
        public RelayCommand CommandBrowse { get; }
        public RelayCommand CommandValidate { get; }

        public ViewModelMain()
        {
            CommandBrowse = new RelayCommand(Browse);
            CommandValidate = new RelayCommand(ValidateAsync, CanValidate);
            Standards = new ObservableCollection<MyStandard>();
        }

        private void Browse(object parameter)
        {
            var DirectoryPath = Common.GetDirectory();
            List<string> files = Common.GetAllFiles(DirectoryPath).ToList<string>();
            var id = 0;
            files.ForEach(x =>
            {
                id++;
                Standards.Add(new MyStandard
                {
                    ID = id,
                    CurrentStandard = Path.GetFileNameWithoutExtension(x),
                    CurrentStandardNumber = Common.ParseString(Path.GetFileNameWithoutExtension(x)
                    , @"^(\w+[\u3000\u0020]+\d+-\d+)[\u3000\u0020]+", 1)
                });
            });

        }

        private bool CanValidate(object parameter)
        {
            if (Standards.Count >0)
            {
                return true;
            }
            return false;
        }

        private async void ValidateAsync(object parameter)
        {
            for (var i = 0; i < Standards.Count; i++)
            {
                var standardNumber = Standards[i].CurrentStandardNumber;
                var queryString = $"http://www.csres.com/s.jsp?keyword={standardNumber}&pageNum=1";
                var httpResponse =await HttpHelper.GetHtmlAsync(queryString).Result;
                var content = HttpHelper.GetContentByXPath(httpResponse, "//tr[2]/td[5]/font");
                ////MessageBox.Show(content);
            }
        }
    }
}

