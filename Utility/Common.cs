using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        /// <summary>
        /// <summary>
        /// 字符串转Unicode
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>Unicode编码后的字符串</returns>
        public static string String2Unicode(string source)
        {
            var bytes = Encoding.Unicode.GetBytes(source);
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < bytes.Length; i += 2)
            {
                stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Unicode转字符串
        /// </summary>
        /// <param name="source">经过Unicode编码的字符串</param>
        /// <returns>正常字符串</returns>
        public static string Unicode2String(string source)
        {
            return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(
                         source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
        }
    }
}
