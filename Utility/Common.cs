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
        public static IEnumerable<string> EnumrateFiles(string specificDirectory)
        {
            if (!Directory.Exists(specificDirectory))
            {
                return null;
            }
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            IEnumerable<string> files = Directory.EnumerateFiles(specificDirectory, "*.*", SearchOption.AllDirectories);

            return files?.Select(x => Path.GetFileNameWithoutExtension(x));
        }

        public static string SelectDirectory(string LastDirectoryLogFile)
        {
            string logFile = Path.Combine(Path.GetTempPath(), LastDirectoryLogFile);
            string lastDirectory = string.Empty;
            if (File.Exists(logFile))
            {
                lastDirectory = File.ReadAllText(logFile);
            }
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (Directory.Exists(lastDirectory))
            {
                fbd.SelectedPath = lastDirectory;
            }
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(logFile, fbd.SelectedPath);
                return fbd.SelectedPath;
            }

            return string.Empty;
        }

        public static string GetData(string url)
        {
            string data = string.Empty;
            try
            {
                data = new WebClient().DownloadString(url);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return data;
        }
    }
}
