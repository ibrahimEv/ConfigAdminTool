namespace ConfigToolWPF.Model
{
    public class ExcelSheet
    {
        public int Id { get; set; }
        public string SheetName { get; set; }
        public bool IsSelected { get; set; } = true;
        public bool IsMerged { get; set; }
        public string MergeStatus { get; set; } = "Not Merged";
        public string ErrorMessage { get; set; }
    }
}
