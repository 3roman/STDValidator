using STDValidator.Models;
using STDValidator.Utility;
using Stylet;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace STDValidator.ViewModels
{
    public class MainViewModel : Stylet.Screen
    {
        public BindableCollection<CodeEx> Codes { get; set; } = new BindableCollection<CodeEx>();
        public CodeEx SelectedCode { get; set; }
        public string StateMessage { get; set; } = "未扫描";
        public string KeyWord { get; set; }
        private long Scanned;
        public bool CanOnValidate { get; set; }
        private readonly IWindowManager _windowsManager;

        public MainViewModel(IWindowManager windowManager)
        {
            _windowsManager = windowManager;
        }

        public void OnMenuCopyNumber()
        {
            if (SelectedCode != null && !string.IsNullOrEmpty(SelectedCode.LatestNumber))
            {
                System.Windows.Clipboard.SetText(SelectedCode.LatestNumber);
            }
            else if (SelectedCode != null && !string.IsNullOrEmpty(SelectedCode.Number))
            {
                System.Windows.Clipboard.SetText(SelectedCode.Number);
            }
        }

        public void OnMenuCopyName()
        {
            if (SelectedCode != null)
            {
                System.Windows.Clipboard.SetText(SelectedCode.Name);
            }
        }

        public void OnMenuImportFromExcel()
        {
            _windowsManager.ShowMessageBox("1、工作表名必须为Sheet1\r\n2、第1列为标准号，第2列为标准名（可选列）", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = @"Excel|*.xlsx;*.xls";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var dt = Common.ImportFromExcel(ofd.FileName);
                    foreach (var item in dt.AsEnumerable())
                    {
                        Codes.Add(new CodeEx()
                        {
                            Number = item[0].ToString(),
                            Name = item.ItemArray.Length == 2 ? item[1].ToString() : string.Empty
                        }
                       );
                    }
                }
            }
            Scanned = 0;
            StateMessage = $"待命中：{Scanned}/{Codes.Count}";
            CanOnValidate = Codes.Count > 0;
        }

        public void OnMenuExportToExcel()
        {
            var sfd = new SaveFileDialog
            {
                DefaultExt = @"xlsx",
                Filter = @"Excel|*.xlsx",
                FileName = "data",
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                Common.ExportToExcel(Codes, sfd.FileName);
            }
        }

        public void OnBrowse()
        {
            var files = Common.BrowseFileName();
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                var number = Regex.Match(file, @"([\w／]+\s*[\d.]+[-_]\d+\s*)").Groups[1].Value;
                if (string.IsNullOrEmpty(number.Trim()))
                {
                    continue;
                }
                Codes.Add(new CodeEx
                {
                    Name = file.Replace(number, string.Empty),
                    Number = number
                });
            }
            Scanned = 0;
            StateMessage = $"待命中：{Scanned}/{Codes.Count}";
            CanOnValidate = Codes.Count > 0;
        }

        public void OnAdd()
        {
            if (string.IsNullOrEmpty(KeyWord))
            {
                return;
            }
            if (Regex.IsMatch(KeyWord, "[\u4e00-\u9fa5]+"))
            {
                _windowsManager.ShowMessageBox("只支持标准号验证", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                KeyWord = string.Empty;
                return;
            }
            Codes.Add(new CodeEx
            {
                Number = KeyWord
            });
            KeyWord = string.Empty;
            Scanned = 0;
            StateMessage = $"待命中：{Scanned}/{Codes.Count}";
            CanOnValidate = Codes.Count > 0;
        }

        public void OnClear()
        {
            Codes.Clear();
            Scanned = 0;
            StateMessage = "未扫描";
            CanOnValidate = false;
        }

        public void OnValidate()
        {
            // 套一层Task，防止UI线程阻塞
            Task.Run(() =>
            {
                Scanned = 0;
                CanOnValidate = false;
                Parallel.ForEach(Codes, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, Code =>
                {
                    ValidateWorker(Code);
                    Interlocked.Increment(ref Scanned);
                    StateMessage = $"验证中：{Scanned}/{Codes.Count}";
                });
                StateMessage = $"验证结束：{Scanned}/{Codes.Count}";
                CanOnValidate = true;
            });

        }

        private void ValidateWorker(CodeEx code)
        {
            // 必须把T替换掉，否则有些查不到
            var number = code.Number.Replace("T", string.Empty);
            var content = Common.GetData($"http://www.csres.com/s.jsp?keyword={number}&pageNum=1");

            if (!number.Contains("-"))
            {
                DealWithIncompletedCodeNumber(code, content);

                return;
            }

            if (content.Contains("没有找到"))
            {
                code.State = CodeState.NotFound;
                code.TextColor = Brushes.Blue;

                return;
            }

            if (Regex.Matches(content, "现行").Count >= 3)
            {
                var matches = Regex.Matches(content, "<font color=\"#000000\">(.+)</font>");
                code.Number = matches[0].Groups[1].Value;
                code.Name = matches[1].Groups[1].Value;
                code.State = CodeState.Valid;
                code.TextColor = Brushes.ForestGreen;

                return;
            }

            if (Regex.Matches(content, "作废").Count >= 3)
            {
                code.State = CodeState.Expired;
                code.TextColor = Brushes.Red;
                var newLink = Regex.Match(content, @"/detail/.+html").Value;
                content = Common.GetData($"http://www.csres.com/{newLink}");
                var matches = Regex.Matches(content, "被.+(/detail/.+html).+blank>(.+)</a>[代替]{1}");
                if (matches.Count == 0)
                {
                    code.LatestNumber = "***自动挖掘失败***";
                }
                else
                {
                    code.LatestNumber = matches[0].Groups[2].Value;
                    newLink = matches[0].Groups[1].Value;
                    content = Common.GetData($"http://www.csres.com/{newLink}");
                    code.Name = Regex.Match(content, @"h3>(.+)<a").Groups[1].Value;
                }
            }

            return;

        }

        public void DealWithIncompletedCodeNumber(CodeEx code, string content)
        {
            var resuls = Regex.Matches(content, "color=\"#000000\">(.+)</font>");
            if (resuls.Count < 1)
            {
                code.State = CodeState.NotFound;
                code.TextColor = Brushes.Blue;
                return;
            }
            else if (resuls.Count >= 2)
            {
                code.Number = resuls[0].Groups[1].Value;
                code.Name = resuls[1].Groups[1].Value;
                code.State = CodeState.Valid;
                code.TextColor = Brushes.ForestGreen;
            }

            return;
        }
    }
}

