using Stylet;
using System.Text.RegularExpressions;

namespace STDValidator.Models
{
    public enum CodeState
    {
        None            = 0,
        Valid           = 1,
        Expired         = 2,
        NotFound        = 3,
    }


    public class Code : PropertyChangedBase
    {
        private string _number;
        public string Number
        {
            get { return _number; }
            set
            {
                _number = UnitizeCodeNumber(value);
            }
        }
        public string Name { get; set; }
        public string LatestNumber { get; set; }
        public CodeState State { get; set; }


        protected virtual string UnitizeCodeNumber(string codeNumber)
        {
            var number  = codeNumber.Replace("／", string.Empty); // 替换全角斜杠
            number      = number.Replace("_", "-"); // 替换下划线
            number      = Regex.Replace(number, "[\u3000\u0020]{1,}", " "); // 替换两个以上中文或英文空格为一个英文空格
            number      = number.ToUpper().Trim(); // 转大写

            return number;
        }
    }
}
