using AngleSharp.Html.Parser;
using MiniExcelLibs;
using STDValidator.Models;
using STDValidator.Utility;
using Stylet;
using System.Diagnostics;
using System.IO;
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
        public string KeyWord { get; set; } = "DL/T5054-1996";
        private long Scanned;
        public bool CanOnValidate { get; set; }
        private readonly IWindowManager _windowsManager;


        public MainViewModel(IWindowManager windowManager)
        {
            _windowsManager = windowManager;
        }

        public void OnMenuCopyLatestNumber()
        {
            if (SelectedCode == null)
            {
                return;
            }
            if (SelectedCode.State == CodeState.Valid)
            {
                System.Windows.Clipboard.SetText(SelectedCode.Number);
            }
            else
            {
                System.Windows.Clipboard.SetText(SelectedCode.LatestNumber);
            }
        }

        public void OnMenuCopyLatestName()
        {
            if (SelectedCode == null)
            {
                return;
            }
            if (SelectedCode.State == CodeState.Valid)
            {
                System.Windows.Clipboard.SetText(SelectedCode.Name);
            }
            else
            {
                System.Windows.Clipboard.SetText(SelectedCode.LatestName);
            }
        }

        public void OnMenuBrowse()
        {
            var files = Common.BrowseFileName();
            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                var number = Regex.Match(file, @"([\w／]+\s*[\d.]+[-_]\d+\s*)").Groups[1].Value;
                if (!string.IsNullOrEmpty(number.Trim()))
                {
                    Codes.Add(new CodeEx
                    {
                        Name = file.Replace(number, string.Empty),
                        Number = number
                    });
                }
            }
            Scanned = 0;
            StateMessage = $"待命中：{Scanned}/{Codes.Count}";
            CanOnValidate = Codes.Count > 0;
        }

        public void OnMenuImportFromExcel()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = @"Excel|*.xlsx;*.xls";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (var stream = File.OpenRead(ofd.FileName))
                    {
                        var rows = stream.Query(); // 返回动态类型
                        foreach (var row in rows)
                        {
                            Codes.Add(new CodeEx()
                            {
                                Number = row.A,
                            });
                        }
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
                using (var sr = File.Create(sfd.FileName))
                {
                    sr.SaveAs(Codes);
                }
                if (File.Exists(sfd.FileName))
                {
                    Process.Start(sfd.FileName);
                }
            }
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
            var currentTask = Task.Run(() =>
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
                CanOnValidate = Codes.Count > 0;
            });

        }

        private void ValidateWorker(CodeEx code)
        {
            // 查询必须把T/替换掉，否则有些查不到
            var number = code.Number.Replace("T", string.Empty).Replace("/", string.Empty).Replace(" ","");
            var content = Common.GetData($"http://www.csres.com/s.jsp?keyword={number}&pageNum=1");
            //Debugger.Log(1, "", $"{number}\n");

            // 每日次数限制检查
            if (content.Contains("已经超出我们允许的范围"))
            {
                KeyWord = "啊哦，超出了网站每天允许的查询次数，明天再试吧！！！";
            }

            var parser = new HtmlParser();
            var document = parser.ParseDocument(content);
            var results = document.QuerySelectorAll("thead.th1");
            // 未找到
            if (results.Length == 0)
            {
                code.State = CodeState.NotFound;
                code.TextColor = Brushes.Blue;

                return;
            }
            // 有效标准
            results = document.QuerySelectorAll("thead.th1 font[color=\"#000000\"]");
            if (results.Length >= 5)
            {
                code.State = CodeState.Valid;
                code.TextColor = Brushes.Green;
                code.Number = results[0].TextContent;
                code.Name = results[1].TextContent;

                return;
            }
            // 废止
            results = document.QuerySelectorAll("thead.th1 font[color=\"#FF0000\"]");
            if (results.Length == 5)
            {
                code.State = CodeState.Expired;
                code.TextColor = Brushes.Red;
                var result = document.QuerySelector("thead.th1 a").GetAttribute("href");
                content = Common.GetData($"http://www.csres.com/{result}");
                // 有新标准链接信息
                //var match = Regex.Match(content, "被.+(/detail/.+html).+blank>(.+)</a>(替代|代替){1}");
                //var match = Regex.Match(content, "(被.+blank>|被){1}(.+?)(?=</a>|替代|代替)");
                var match = Regex.Match(content, "(被.+blank>|被){1}(.+?)(?=</a>替代|</a>代替|替代|代替)");
                if (match != null && match.Groups.Count == 3)
                {
                    code.LatestNumber = match.Groups[2].Value;
                    code.LatestName = retrieveLatestName(code.LatestNumber);

                    return;
                }

                code.LatestNumber = "**自动检索无结果**";
            }
        }

        private string retrieveLatestName(string latestNumber)
        {
            var content = Common.GetData($"http://www.csres.com/s.jsp?keyword={latestNumber}&pageNum=1");
            var parser = new HtmlParser();
            var document = parser.ParseDocument(content);
            var results = document.QuerySelectorAll("thead.th1 font[color=\"#000000\"]");
            if (results.Length >= 5)
            {
                return results[1].TextContent;
            }

            return string.Empty;
        }
    }
}

