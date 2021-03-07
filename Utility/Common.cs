using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace STDValidator.Utility
{
    public static class Common
    {
        public static IEnumerable<string> GetAllFiles(string path)
        {

            return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }

        public static string GetDirectory(string initialDirectory="")
        {
            var path = string.Empty;
            var fbd = new FolderBrowserDialog
            {
                Description = @"请选择标准规范所在目录",
                SelectedPath = initialDirectory

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

        public static string Unicode2String(string source)
        {
            return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(
                         source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
        }

        public static DataTable List2DataTable<T>(List<T> objects)
        {
            //取出第一个实体的所有Propertie
            var entityType = objects[0].GetType();
            var entityProperties = entityType.GetProperties();

            //生成DataTable的structure
            //生产代码中，应将生成的DataTable结构Cache起来，此处略
            var dt = new DataTable();
            for (var i = 0; i < entityProperties.Length; i++)
            {
                dt.Columns.Add(entityProperties[i].Name);
            }
            //将所有entity添加到DataTable中
            foreach (var entity in objects)
            {
                //检查所有的的实体都为同一类型
                if (entity.GetType() != entityType)
                {
                    throw new Exception("type error");
                }
                var entityValues = new object[entityProperties.Length];
                for (var i = 0; i < entityProperties.Length; i++)
                {
                    entityValues[i] = entityProperties[i].GetValue(entity, null);
                }
                dt.Rows.Add(entityValues);
            }

            return dt;
        }

        public static void DataTable2Excel(this DataTable dt, string filename)
        {
            var wb = new XLWorkbook();
            wb.Worksheets.Add(dt, "Sheet1");
            wb.SaveAs(filename);
        }
    }
}
