using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigToolWPF.Model
{
    public class ExcelSheet
    {
        public int Id { get; set; }
        public string SheetName { get; set; }
        public bool IsSelected { get; set; }
        public bool IsMerged { get; set; }
        public string MergeStatus { get; set; } = "Not Merged";
    }
}
