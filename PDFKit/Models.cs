using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFKit
{
    public enum TextStyle
    {
        Title,      // large, bold - document title
        Heading,    // medium, bold - section heading
        SubHeading, // medium, semi-bold
        Body,       // normal text
        Small,       // footnotes, captions
        ResultChip,
        SmallBold
    }

    public enum TextAlign
    {
        Left,
        Center,
        Right
    }

    public enum ImageSlotType
    {
        Default,
        Signature,
        Stamp
    }

    public sealed class PageSettings
    {
        public double MarginLeft { get; init; } = 40;
        public double MarginRight { get; init; } = 40;
        public double MarginTop { get; init; } = 40;
        public double MarginBottom { get; init; } = 50; // leaves room for footer/page numbers
        public bool Landscape { get; init; } = false;
    }

    /// <summary>
    /// Defines one column in a table. Widths are proportional weights,
    /// not fixed points - they're scaled to fill the available content width.
    /// </summary>
    public sealed class TableColumn
    {
        public required string Header { get; init; }
        public double Weight { get; init; } = 1.0;
        public TextAlign Align { get; init; } = TextAlign.Left;
    }

    public sealed class LetterheadInfo
    {
        public string CompanyName { get; init; } = "Vishal Mega Mart";
        public string? Tagline { get; init; }
        public string? LogoPath { get; init; } // optional path to a PNG/JPEG logo
        public string? DocumentTitle { get; init; } // e.g. "Factory Audit Report"
        public string? ReferenceNumber { get; init; } // e.g. CR/QPS number
        public string? FooterInfo { get; init; }
        public string? DateOfInspection { get; init; }
        public string? Last_Amendment_Date { get; init; }
    }


    /// <summary>
    /// Mirrors MudBlazor's chip colors for status labels (PASS/FAIL badges,
    /// section headers like "Defects Observed", "QA Documents").
    /// </summary>
    public enum ChipColor
    {
        Default,
        Success,
        Error,
        Warning,
        Secondary
    }

    /// <summary>
    /// Describes one table for AddSideBySideTables - an optional title plus
    /// the same columns/rows shape AddTable already uses.
    /// </summary>
    public sealed class TableSpec
    {
        public string? Title { get; init; }

        /// <summary>
        /// A single header cell that spans the full table width - the PdfKit
        /// equivalent of HTML's &lt;th colspan="N"&gt;, e.g.
        /// "No. of Check Carton/Bags and Pre-Pack" or "Measurement Sample
        /// Size :- 12 PCS" sitting above the data rows with no other header
        /// cells. If `Columns` also has non-blank headers, this renders as an
        /// extra row above them; if `Columns` headers are all blank, this is
        /// the only header row drawn (true colspan - no redundant empty strip).
        /// </summary>
        public string? ColSpanHeaderText { get; init; }

        public required IReadOnlyList<TableColumn> Columns { get; init; }
        public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

        /// <summary>Column indices whose values (PASS/FAIL/YES/NO) should render as colored pills.</summary>
        public ISet<int>? ChipColumns { get; init; }

        /// <summary>Row indices (into `Rows`) to render in bold plain text instead of chips - e.g. a "WITHIN TOLERANCE / PASS-FAIL" label row.</summary>
        public ISet<int>? BoldRows { get; init; }
        public ISet<int>? IconRows { get; init; }
    }


    /// <summary>
    /// One field in a multi-field single-line row (see AddFieldsRow) -
    /// mirrors a MudGrid row made of adjacent label/value MudItems.
    /// </summary>
    public sealed class RowField
    {
        public required string Label { get; init; }
        public string Value { get; init; } = "";
        public double Weight { get; init; } = 1.0;
    }

    /// <summary>
    /// A file to append at the end of the document - either a PDF (its pages
    /// are merged in) or an image (placed on its own page, scaled to fit).
    /// </summary>
    public sealed class AttachmentFile
    {
        public required string FilePath { get; init; }
        public string? Caption { get; init; }
    }

    /// <summary>
    /// Data for one signature/stamp block (see PdfBuilder.AddSignatureBlocks).
    ///
    /// SignatureImagePath and StampImagePath MUST be local file paths, not
    /// URLs - PdfBuilder never makes network calls. If your source data
    /// (e.g. a StampAndSignatureInfo with public URLs) holds URLs instead,
    /// download them to local temp files first - see
    /// AttachmentPathResolver.DownloadSingleFileAsync in the Infrastructure
    /// project for a ready-made helper that does exactly that.
    /// </summary>
    public sealed class SignatureBlockInfo
    {
        /// <summary>Header bar title, e.g. "Name of VMM QA".</summary>
        public required string HeaderTitle { get; init; }
        public string? Name { get; init; }
        public string? SignatureImagePath { get; init; }
        public byte[]? SignatureImage { get; init; }
        public string? StampImagePath { get; init; }
        public string? DateOfSignature { get; init; }
        public string StampNo { get; init; } = "";
    }




}