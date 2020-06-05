using Caliburn.Micro;

namespace TableToStringConverter.ViewModels
{
    public class ReplacementViewModel : PropertyChangedBase
    {
        public string ReplacementKey { get; set; }
        public int ColumnIndex { get; set; }
        public char SplitOn { get; set; }
    }
}