using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Request
{
    public class SelectListReq
    {
        public int Id { get; set; }
        public string? Cmd {  get; set; }
        public string? StrField { get; set; } = string.Empty;
        public string? ResponseType { get; set; } = string.Empty;
        public ExcelReportFilter excelReportFilter { get; set; } = new();
    }

    public class ExcelReportFilter
    {
        public string? SearchQuery { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
