using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace STDValidator.Utility
{
    public static class Common
    {
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

        public static string ParseString(string rawString, string pattern, int groupIndex)
        {
            return Regex.Match(rawString, pattern).Groups[groupIndex].Value;
        }
    }
}
