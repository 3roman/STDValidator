using System.Windows.Media;

namespace STDValidator.Models
{
    public class CodeEx : Code
    {
        public Brush TextColor { get; set; } = Brushes.Black;
        public string RawFilePath { get; set; }
    }
}
