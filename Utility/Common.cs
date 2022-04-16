using ClosedXML.Excel;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
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

        private static DataTable ListToDataTable<T>(IList<T> items)
        {
            if (items == null || items.Count < 1)
            {
                return new DataTable();
            }

            var itemProperties = items[0].GetType().GetProperties();

            var dt = new DataTable("dt");
            for (var i = 0; i < itemProperties.Length; i++)
            {
                dt.Columns.Add(itemProperties[i].Name);
            }

            foreach (var item in items)
            {
                var entityValues = new object[itemProperties.Length];
                for (var i = 0; i < itemProperties.Length; i++)
                {
                    entityValues[i] = itemProperties[i].GetValue(item, null);
                }
                dt.Rows.Add(entityValues);
            }

            return dt;
        }

        public static void ExportToExcel<T>(IList<T> items, string fileName)
        {
            var wb = new XLWorkbook();
            wb.Worksheets.Add(ListToDataTable(items), "Sheet1");
            wb.SaveAs(fileName);
        }

        public static DataTable ImportFromExcel(string fileName)
        {
            var connectionString = $"Provider=Microsoft.Ace.OleDb.12.0; data source={fileName};;Extended Properties='Excel 12.0; HDR=No; IMEX=1'";
            var adapter = new OleDbDataAdapter("SELECT * FROM [Sheet1$]", connectionString);
            var ds = new DataSet();
            adapter.Fill(ds, "anyNameHere");
            return ds.Tables["anyNameHere"];
        }

        public static string GetData(string url)
        {
            var data = string.Empty;
            using (var client = new WebClient())
            {
                data = client.DownloadString(url);
            }

            return data;
        }
    }
}
