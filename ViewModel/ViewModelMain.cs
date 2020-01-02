using StandardValidator.Model;
using StandardValidator.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StandardValidator.ViewModel
{
    class ViewModelMain : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<MyStandard> Standards { get; set; }
        public RelayCommand CommandBrowse { get; }
        public RelayCommand CommandValidate { get; }
        private bool isValidating = false;

        public ViewModelMain()
        {
            CommandBrowse = new RelayCommand(Browse);
            CommandValidate = new RelayCommand(Validate, CanValidate);
            Standards = new ObservableCollection<MyStandard>();
        }

        private void Browse(object parameter)
        {
            var DirectoryPath = Common.GetDirectory();
            var files = Common.GetAllFiles(DirectoryPath).ToList<string>();
            Standards.Clear();
            foreach (var file in files)
            {
                var currentStandard = Path.GetFileNameWithoutExtension(file);
                var currentStandardNumber = Common.ParseString(currentStandard,
                     @"^(\w+[\u3000\u0020]+\d+\.?\d+-\d+)[\u3000\u0020]+", 1);
                if (string.Empty == currentStandardNumber)
                {
                    continue;
                }
                var currentStandardName = currentStandard.Replace(currentStandardNumber, "").Trim();
                Standards.Add(new MyStandard
                {
                    ID = Standards.Count + 1,
                    CurrentStandardName = currentStandardName,
                    CurrentStandardNumber = currentStandardNumber
                });
            }
        }

        private bool CanValidate(object parameter)
        {
            if (Standards.Count > 0 && !isValidating)
            {
                return true;
            }

            return false;
        }

        private void Validate(object parameter)
        {
            foreach (var standard in Standards)
            {
                Task.Factory.StartNew(() => ValidateMethod(standard));
            }
        }

        public void ValidateMethod(MyStandard standard)
        {
            var httpResponse = HttpHelper.GetHttpContent($"http://www.csres.com/s.jsp?keyword={standard.CurrentStandardNumber}&pageNum=1");
            if (httpResponse.Contains("没有找到"))
            {
                standard.State = "未找到";
                return;
            }
            standard.State = HttpHelper.GetContentByXPath(httpResponse, "//td[5]/font").InnerHtml;
            if (!standard.State.Contains("现行"))
            {
                var link = HttpHelper.GetContentByXPath(httpResponse, "//table/thead/tr[2]/td[1]/a").Attributes["href"].Value;
                httpResponse = HttpHelper.GetHttpContent($"http://www.csres.com{link}");
                var pattern = string.Format("[{0}](\\w{{2,}}.+-.+)[{1}][{2}]",
                    Common.String2Unicode("被"),
                    Common.String2Unicode("代"),
                    Common.String2Unicode("替"));
                standard.LatestStandardNumber = Common.ParseString(httpResponse, pattern, 1);
            }
        }
    }
}

