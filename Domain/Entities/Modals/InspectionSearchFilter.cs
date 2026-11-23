using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Modals
{
    public class InspectionSearchFilter
    {
        public string VendorId { get; set; } = "";
        public string BuyerId { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string InspectorId { get; set; } = "";
        public string? Mrp { get; set; }          // free text, matched as LIKE
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public bool HasAnyFilter =>
            VendorId != "" || BuyerId != "" || CategoryId != "" ||
            ProductId != "" || InspectorId != "" ||
            !string.IsNullOrWhiteSpace(Mrp) ||
            FromDate.HasValue || ToDate.HasValue;

        public void Reset()
        {
            VendorId = BuyerId = CategoryId = ProductId = InspectorId = "";
            Mrp = null;
            FromDate = ToDate = null;
        }
    }


    public class InspectionSearchFilterMultiSelection : PagingState
    {
        public List<MasterData> Vendors { get; set; } = new();
        public List<MasterData> Buyers { get; set; } = new();
        public List<MasterData> Categories { get; set; } = new();
        public List<MasterData> Products { get; set; } = new();
        public List<MasterData> Inspectors { get; set; } = new();

        public List<string> VendorIds => Vendors.Select(m => m.String_ID).ToList();
        public List<string> BuyerIds => Buyers.Select(m => m.String_ID).ToList();
        public List<string> CategoryIds => Categories.Select(m => m.String_ID).ToList();
        public List<string> ProductIds => Products.Select(m => m.String_ID).ToList();
        public List<string> InspectorIds => Inspectors.Select(m => m.String_ID).ToList();

        public string? Mrp { get; set; }
        public string? PONumber { get; set; }
        public string? InspectionNumber { get; set; }
        public string RequestType { get; set; } = "VIEW";
        public string SubType { get; set; } = "ALL";
        //public DateTime? FromDate { get; set; }
        //public DateTime? ToDate { get; set; }
        public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);//new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime? ToDate { get; set; } = DateTime.Today.AddDays(15);
        public string? Cmd { get; set; }
        public string? StrField { get; set; } = string.Empty;
        public bool IsVendor { get; set; } = false;
        public bool IsCleared { get; private set; } = true;
        public bool IS_QC_AT_DC { get; set; } = false;

        public void MarkDirty()
        {
            IsCleared = false;
        }
        public void MarkReset()
        {
            IsCleared = true;
        }

        public bool Reset()
        {
            if (IsCleared && !HasAnyFilter())
                return false; // nothing to do — already in cleared state

            Vendors.Clear(); Buyers.Clear(); Categories.Clear();
            Products.Clear(); Inspectors.Clear();

            Mrp = null; PONumber = null; InspectionNumber = null;
            FromDate = ToDate = null; IS_QC_AT_DC = false;

            IsCleared = true;
            return true; // a real reset happened
        }

        public InspectionSearchFilterMultiSelection Clone()
        {
            return new InspectionSearchFilterMultiSelection
            {
                Vendors = new List<MasterData>(Vendors),
                Buyers = new List<MasterData>(Buyers),
                Categories = new List<MasterData>(Categories),
                Products = new List<MasterData>(Products),
                Inspectors = new List<MasterData>(Inspectors),

                Mrp = Mrp,
                PONumber = PONumber,
                InspectionNumber = InspectionNumber,
                RequestType = RequestType,
                FromDate = FromDate,
                ToDate = ToDate,
                Cmd = Cmd,
                StrField = StrField,
                PageNumber = PageNumber,
                PageSize = PageSize,
                IS_QC_AT_DC = IS_QC_AT_DC

            };
        }

        public void Sanitize()
        {
            Mrp = NullIfEmpty(Mrp);
            PONumber = NullIfEmpty(PONumber);
            InspectionNumber = NullIfEmpty(InspectionNumber);
            Cmd = NullIfEmpty(Cmd);
            StrField = NullIfEmpty(StrField);
        }

        private static string? NullIfEmpty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();

            if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            return trimmed;
        }

        public bool HasAnyFilter()
        {
            return Vendors.Any()
                || Buyers.Any()
                || Categories.Any()
                || Products.Any()
                || Inspectors.Any()
                || !string.IsNullOrWhiteSpace(Mrp)
                || !string.IsNullOrWhiteSpace(PONumber)
                || !string.IsNullOrWhiteSpace(InspectionNumber)
                || FromDate.HasValue
                || ToDate.HasValue
                || IS_QC_AT_DC;
        }
        public int ActiveFilterCount()
        {
            int count = 0;
            if (Vendors.Any()) count++;
            if (Buyers.Any()) count++;
            if (Categories.Any()) count++;
            if (Products.Any()) count++;
            if (Inspectors.Any()) count++;
            if (!string.IsNullOrWhiteSpace(Mrp)) count++;
            if (!string.IsNullOrWhiteSpace(PONumber)) count++;
            if (!string.IsNullOrWhiteSpace(InspectionNumber)) count++;
            if (FromDate.HasValue || ToDate.HasValue) count++;
            if (IS_QC_AT_DC) count++;
            return count;
        }

    }

    public class PagingState
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
