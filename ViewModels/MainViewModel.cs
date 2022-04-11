using STDValidator.Models;
using System.IO;
using System.Linq;
using Stylet;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;


namespace STDValidator.ViewModels
{
    public class MainViewModel : Stylet.Screen
    {
        public BindableCollection<Code> Codes { get; set; } = new BindableCollection<Code>();
        public string ScanState { get; set; } = "未扫描";
        public bool CanOnlineValidate { get; set; } = true;
        public long Scanned  = 0;

        public void Browse()
        {
            Codes.Clear();
            var files = BrowseFiles().Select(x => Path.GetFileNameWithoutExtension(x));
            foreach (var file in files)
            {
                var codeNumber = Regex.Match(file, @"^(\w+[\u3000\u0020]+\d+\.?\d+-\d+)[\u3000\u0020]+").Groups[1].Value;
                if (string.IsNullOrEmpty(codeNumber))
                {
                    continue;
                }
                Codes.Add(new Code
                {
                    Index = Codes.Count + 1,
                    CodeName = file.Replace(codeNumber, ""),
                    CodeNumber = codeNumber.ToUpper()
                });
            }

            ScanState = $"待命中：{Scanned}/{Codes.Count}";
        }


        public void OnlineValidate()
        {
            // 套一层Task，防止UI线程阻塞
            Task.Run(() =>
            {
                CanOnlineValidate = false;
                Scanned = 0;
                Parallel.ForEach(Codes, new ParallelOptions() { MaxDegreeOfParallelism = 10 }, Code =>
                {
                    Validate(Code);
                    Interlocked.Increment(ref Scanned);
                    ScanState = $"扫描中：{Scanned}/{Codes.Count}";
                });
                ScanState = $"扫描结束：{Scanned}/{Codes.Count}";
                CanOnlineValidate = true;
            });
            
        }

        private void Validate(Code code)
        {
            var number = Regex.Replace(code.CodeNumber, @"[tT]", "");
            string content;
            using (var client = new WebClient())
            {
                content = client.DownloadString($"http://www.csres.com/s.jsp?keyword={number}&pageNum=1");
            }
            if (content.Contains("没有找到"))
            {
                code.Effectiveness = "未找到";

                return;
            }
            if (!content.Contains(@"现行</font></td>"))
            {
                code.Effectiveness = "作废";
                var link = Regex.Match(content, @"/detail/.+html").Value;
                using (var client = new WebClient())
                {
                    content = client.DownloadString($"http://www.csres.com{link}");
                }
                code.LatestCodeNumber = Regex.Match(content, @"被.+blank>(.+)</a>代替").Groups[1].Value;
            }
            else
            {
                code.Effectiveness = "现行";
            }
        }

        public static IEnumerable<string> BrowseFiles()
        {
            IEnumerable<string> files = null;
            var fbd = new FolderBrowserDialog();
            if (DialogResult.OK == fbd.ShowDialog())
            {
                files = Directory.EnumerateFiles(fbd.SelectedPath, "*", SearchOption.AllDirectories);
            }

            return files;
        }
    }
}

