using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace StandardValidator.Utility
{
    class HttpHelper
    {
        public static async Task<string> GetHtmlAsync(string url)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage response;
            var result = string.Empty;

            using (response = await httpClient.GetAsync(url))
            {
                var buffer = await response.Content.ReadAsByteArrayAsync();
                result = Encoding.GetEncoding("GBK").GetString(buffer);
            }      
            
            return result;
        }

        public static string GetContentByXPath(string html, string xPath)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var nodes = doc.DocumentNode.SelectNodes(xPath);
            if (null != nodes)
            {
                return nodes[1].InnerHtml;
            }

            return string.Empty;
        }
    }
}
