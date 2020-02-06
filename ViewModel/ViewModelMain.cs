using STDValidator.Model;
using STDValidator.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STDValidator.ViewModel
{
    class ViewModelMain : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<MyStandard> Standards { get; set; }
        public RelayCommand CommandBrowse { get; }
        public RelayCommand CommandValidate { get; }
        public RelayCommand CommandExport { get; }
        private List<Task> tasks = new List<Task>();

        public ViewModelMain()
        {
            CommandBrowse = new RelayCommand(Browse);
            CommandValidate = new RelayCommand(Validate, CanValidate);
            CommandExport = new RelayCommand(Export, CanExport);
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
                    CurrentStandardNumber = currentStandardNumber.ToUpper()
                });
            }
        }

        private bool CanValidate(object parameter)
        {
            if (tasks.Count > 0)
            {
                foreach (var item in tasks)
                {
                    if (!item.IsCompleted)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void Validate(object parameter)
        {
            foreach (var standard in Standards)
            {
                tasks.Add(Task.Factory.StartNew(() => ValidateMethod(standard)));
            }
        }

        public void ValidateMethod(MyStandard standard)
        {
            var content = HttpHelper.GetHttpContent($"http://www.csres.com/s.jsp?keyword={standard.CurrentStandardNumber}&pageNum=1");
            if (content.Contains("没有找到"))
            {
                standard.State = "未找到";
                return;
            }

            standard.State = HttpHelper.GetContentByXPath(content, "//td[5]/font").InnerHtml;
            if (!standard.State.Contains("现行"))
            {
                var link = HttpHelper.GetContentByXPath(content, "//table/thead/tr[2]/td[1]/a").Attributes["href"].Value;
                content = HttpHelper.GetHttpContent($"http://www.csres.com{link}");
                content = content.Replace("替代", "代替");
                var prefix = Common.ParseString(standard.CurrentStandardNumber, "([A-Z]+).*", 1).Replace("T", "");
                var pattern = @"[\u88ab].*("+ prefix + @"/?T?.+-\d{4}).*[\u4ee3][\u66ff]";
                standard.LatestStandardNumber = Common.ParseString(content, pattern, 1);
            }
        }

        private bool CanExport(object parameter)
        {
            if (Standards.Count > 0)
            {
                return true;
            }
            return false;
        }

        private void Export(object parameter)
        {
            var sfd = new SaveFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), // 设置初始目录
                Filter = @"Excel|*.xlsx|所有文件|*.*",
                FileName = "校验结果"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var filename = sfd.FileName;
                Common.DataTable2Excel(Common.List2DataTable(Standards.ToList()), filename);
            }
        }
    }
}

