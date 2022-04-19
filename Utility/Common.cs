using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace STDValidator.Utility
{
    internal class Common
    {
        public static IEnumerable<string> BrowseFileName()
        {
            IEnumerable<string> files = null;
            var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                files = Directory.EnumerateFiles(fbd.SelectedPath, "*", SearchOption.AllDirectories);
            }
            else
            {
                return null;
            }

            return files?.Select(x => Path.GetFileNameWithoutExtension(x));
        }

        public static string GetData(string url)
        {
            var data = string.Empty;
            try
            {
                data = new WebClient().DownloadString(url);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message,"异常", MessageBoxButtons.OK,MessageBoxIcon.Error);
            }

            return data;
        }
    }
}
