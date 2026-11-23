namespace Domain.Entities.AppSettings
{
    /// <summary>How the PO quantity is taken from the SAP_PO_DETAILS response (PO Sync Case Analysis, N17).</summary>
    public enum PoTotalQtyMode
    {
        /// <summary>PURCHASE_TOT_QTY of the first row: correct when SAP repeats the PO total on every line (behaviour so far).</summary>
        FirstRow = 0,

        /// <summary>Sum of PURCHASE_TOT_QTY, one value per PO line item: correct when PURCHASE_TOT_QTY is the line quantity.</summary>
        SumOfLines = 1,
    }

    /// <summary>"Sap" section of the portal's appsettings.json.</summary>
    public sealed class SapPoOptions
    {
        public const string SectionName = "Sap";

        /// <summary>Switch to SumOfLines once the SAP team confirms PURCHASE_TOT_QTY is a line quantity.</summary>
        public PoTotalQtyMode PoTotalQtyMode { get; set; } = PoTotalQtyMode.FirstRow;
    }
}
