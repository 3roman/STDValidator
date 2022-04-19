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
        public string KeyWord { get; set; }
        private long Scanned;
        private bool isReachedDailyCount;
        public bool CanOnValidate { get; set; }
        private readonly IWindowManager _windowsManager;


        public MainViewModel(IWindowManager windowManager)
        {
            _windowsManager = windowManager;
        }

        public void OnMenuCopyNumber()
        {
            if (SelectedCode == null)
            {
                return;
            }

            var content = string.IsNullOrEmpty(SelectedCode.LatestNumber) ? SelectedCode.Number : SelectedCode.LatestNumber;
            System.Windows.Clipboard.SetText(content);
        }

        public void OnMenuCopyName()
        {
            if (SelectedCode == null)
            {
                return;
            }

            System.Windows.Clipboard.SetText(SelectedCode.Name);
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
                using(var sr = File.Create(sfd.FileName))
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
                    if (isReachedDailyCount)
                    {
                        return;
                    }
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
            if (isReachedDailyCount)
            {
                return;
            }

            // 查询必须把T替换掉，否则有些查不到
            var number = code.Number;
            if (number.Contains("T"))
            {
                number = number.Replace("T", string.Empty);
            }

            // 每日次数限制检查
            var content = Common.GetData($"http://www.csres.com/s.jsp?keyword={number}&pageNum=1");
            Debugger.Log(1, "", $"{number}\n");
            if (content.Contains("已经超出我们允许的范围"))
            {
                KeyWord = "啊哦，超出了网站每天允许的查询次数，明天再试吧！！！";
                isReachedDailyCount = true;
            }

            // 标准号不带年号
            if (!number.Contains("-"))
            {
                DealWithIncompletedCodeNumber(code, content);

                return;
            }

            // 没有找到
            if (content.Contains("没有找到"))
            {
                code.State = CodeState.NotFound;
                code.TextColor = Brushes.Blue;

                return;
            }

            if (Regex.Matches(content, "废止").Count >= 3)
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
                // 替代标准有链接
                var matches = Regex.Matches(content, "被.+(/detail/.+html).+blank>(.+)</a>[代替]{2}");
                if (matches.Count == 0)
                {
                    code.LatestNumber = "***查找失败，请手动验证！***";
                }
                else
                {
                    code.LatestNumber = matches[0].Groups[2].Value;
                    newLink = matches[0].Groups[1].Value;
                    content = Common.GetData($"http://www.csres.com/{newLink}");
                    // 链接到的还是过期标准
                    if (Regex.Matches(content, "现行").Count >= 1)
                    {
                        code.Name = Regex.Match(content, @"h3>(.+)<a").Groups[1].Value;
                    }
                    else
                    {
                        code.LatestNumber = "***查找失败，请手动验证！***";
                    }

                }
                // 替代标准无链接（极少情况）
                matches = Regex.Matches(content, ";被(.+)[替代]{2}");
                if (matches.Count == 1)
                {
                    code.LatestNumber = matches[0].Groups[1].Value;
                    content = Common.GetData($"http://www.csres.com/s.jsp?keyword={code.LatestNumber}&pageNum=1");
                    if (Regex.Matches(content, "现行").Count >= 1)
                    {

                        matches = Regex.Matches(content, "<font color=\"#000000\">(.+)</font>");
                        code.Name = matches[1].Groups[1].Value;
                        code.State = CodeState.Expired;
                        code.TextColor = Brushes.Red;

                        return;
                    }
                    else
                    {
                        code.LatestNumber = "***查找失败，请手动验证！***";
                    }
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

