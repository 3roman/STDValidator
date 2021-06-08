using Stylet;

namespace STDValidator.Models
{
    public class Code : PropertyChangedBase
    {
        public int Index { get; set; }
        public string CodeName { get; set; }
        public string CodeNumber { get; set; }
        public string LatestCodeNumber { get; set; }
        public string Effectiveness { get; set; }
    }
}
