using STDValidator.Models;
using STDValidator.Utility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stylet;

namespace STDValidator.ViewModels
{
    public class MainViewModel : Screen
    {
        public BindableCollection<Code> Codes { get; set; } = new BindableCollection<Code>();
        private readonly List<Task> _tasks = new List<Task>();

        public void Browse()
        {
            
            // 浏览文件
            var files = Common.BrowseFiles().Select(x => Path.GetFileNameWithoutExtension(x));

            // 提取文件名
            Codes.Clear();
            foreach (var file in files)
            {
                var codeNumber = Common.ParseString(file,
                     @"^(\w+[\u3000\u0020]+\d+\.?\d+-\d+)[\u3000\u0020]+", 1);
                if (string.Empty == codeNumber)
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
        }

        public bool CanOnlineValidate => 0 == _tasks.Count;

        public void OnlineValidate()
        {
            foreach (var code in Codes)
            {
                _tasks.Add(Task.Factory.StartNew(() => ValidateMethod(code)));
            }
        }

        private void ValidateMethod(Code code)
        {
            var number = code.CodeNumber.ToUpper().Replace("T", "");
            var content = HttpHelper.GetHttpContent($"http://www.csres.com/s.jsp?keyword={number}&pageNum=1");
            if (content.Contains("没有找到"))
            {
                code.Effectiveness = "未找到";
                
                return;
            }
            code.Effectiveness = HttpHelper.GetContentByXPath(content, "//td[5]/font").InnerHtml;
            if (!code.Effectiveness.Contains("现行"))
            {
                var link = HttpHelper.GetContentByXPath(content, "//table/thead/tr[2]/td[1]/a").Attributes["href"].Value;
                content = HttpHelper.GetHttpContent($"http://www.csres.com{link}");
                content = content.Replace("替代", "代替");
                var prefix = Common.ParseString(code.CodeNumber, "([A-Z]+).*", 1).Replace("T", "");
                var pattern = @"[\u88ab].*(" + prefix + @"/?T?.+-\d{4}).*[\u4ee3][\u66ff]";
                code.LatestCodeNumber = Common.ParseString(content, pattern, 1);
            }
            //Debug.WriteLine(code.Effectiveness);
        }
    }
}

