using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace StandardValidator.Utility
{
    public static class Common
    {
        public static IEnumerable<string> GetAllFiles(string path)
        {

            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        public static string GetDirectory()
        {
            var path = string.Empty;
            var fbd = new FolderBrowserDialog
            {
                Description = "请选择标准规范所在目录"
            };
            if (DialogResult.OK == fbd.ShowDialog())
            {
                path = fbd.SelectedPath;
            }

            return path;
        }

        public static string ParseString(string rawString, string pattern, int groupIndex)
        {
            return Regex.Match(rawString, pattern).Groups[groupIndex].Value;
        }
    }


}
