using DocumentFormat.OpenXml.EMMA;
using Domain.Entities.Modals;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Configuration;
using PDFKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.V1
{
    public sealed class InspectionReportPdfGenerator
    {
        private readonly InspectionReportPdfModel _r;
        private readonly string _rootPaths;
        private readonly string _publicBaseUrl;

        public InspectionReportPdfGenerator(InspectionReportPdfModel model, string rootPaths, string publicBaseUrl)
        {
            _r = model ?? throw new ArgumentNullException(nameof(model));
            _rootPaths = rootPaths;
            _publicBaseUrl = publicBaseUrl;
        }

        public byte[] Generate()
        {
            var r = _r;
            using var builder = new PdfBuilder();
            var (rpt, sup, title) = GetInspectionLabel(r.InspectionTypeId);
            string reportTitleLine = rpt == "Inline" ? title : $"{rpt}{sup} - {title}";
            // ---- Header: company name, PASS/FAIL chip, report title ----
            builder.AddLetterhead(new LetterheadInfo
            {
                CompanyName = "Vishal Mega Mart Limited",
                Tagline = "Quality & Process Standards",
                LogoPath = _rootPaths + "\\resources\\vishal-mega-mart-logo.png",
                DocumentTitle = reportTitleLine,
                ReferenceNumber = $"Report #:{r.ReportNo}",
                FooterInfo = $" [ PO:{r.PONumber} | Report #:{r.ReportNo} ] \n This is system generated inspection report.",
                DateOfInspection = $"FI Date: {r.FIDate?.ToString("dd MMM, yyyy hh:mm tt") ?? ""}",
                Last_Amendment_Date = r.Last_Amendment_Date != null ? $"Last Amendment On: {r.Last_Amendment_Date?.ToString("dd MMM, yyyy hh:mm tt") ?? ""}" : ""

            });

            builder.AddChip(r.InspectionResultText, r.IsPass ? ChipColor.Success : ChipColor.Error, TextAlign.Center, TextStyle.ResultChip);


            //builder.AddText(reportTitleLine, TextStyle.SubHeading, TextAlign.Center);
            builder.AddSpacing(8);

            // ---- Buyer / Report No ----
            builder.AddWrappedKeyValue("Buyer Name:-", $"{r.BuyerName}");

            builder.AddSectionBar("PO Details");
            // ---- Basic info grid ----
            builder.AddWrappedKeyValue("PO No:", $"{r.PONumber} ");
            //builder.AddWrappedKeyValue("FI Date:", $"{r.FIDate?.ToString("dd-MM-yyyy HH:mm") ?? ""}");
            builder.AddWrappedKeyValue("Vendor:", $"{r.VendorName} ({r.VendorCode})");
            builder.AddWrappedKeyValue("Vendor Location:", $"{r.VendorLocation}");

            string descFitMrp = string.Join(" / ", new[]
            {
            string.IsNullOrEmpty(r.MCText) ? "---" : r.MCText,
            string.IsNullOrEmpty(r.Fit) ? "---" : r.Fit,
            r.MRP > 0 ? r.MRP.ToString() : "---"});

            builder.AddWrappedKeyValue("Order Quantity:", $"{r.POTotalQty.ToString("0")}");

            string categoryLabelColor = string.Join(" / ", new[]
            {
            string.IsNullOrEmpty(r.Category) ? "---" : r.Category,
            string.IsNullOrEmpty(r.BrandId) ? "---" : r.BrandId,
            string.IsNullOrEmpty(r.Color) ? "---" : r.Color});

            builder.AddWrappedKeyValue("Offered Quantity:", $"{r.OfferedQuantity.ToString()}");
            builder.AddWrappedKeyValue("Ship Qty.:", $"{r.ShipQuantity.ToString()}");
            builder.AddWrappedKeyValue("Description/FIT/MRP:", $"{descFitMrp}");
            builder.AddWrappedKeyValue("Category/Label/Color:", $"{categoryLabelColor}");

            builder.AddWrappedKeyValue("Article Number:", r.ArticleNumber);


            builder.AddSectionBar("GPT / Elastic / Zipper & Packing Details");
            // ---- GPT / Elastic / Zipper / Packing / MFG block ----
            builder.AddFieldsRow(
                new RowField { Label = "GPT. No.:", Value = r.GptNo, Weight = 6 },
                new RowField { Label = "Status:", Value = r.GptStatus, Weight = 6 });
            builder.AddFieldsRow(
                new RowField { Label = "Approved By (if GPT fails):", Value = r.GptApprovedBy, Weight = 6 },
                new RowField { Label = "Approved Date:", Value = r.GptApprovedDate, Weight = 6 });

            builder.AddWrappedKeyValue("Area of fail GPTR", r.AreaOfFailGptr);

            builder.AddFieldsRow(
                new RowField { Label = "GPT. Elastic No.:", Value = r.GptElasticNo, Weight = 6 },
                new RowField { Label = "Status:", Value = r.GptElasticStatus, Weight = 6 });
            builder.AddFieldsRow(
                new RowField { Label = "Approved By (if Elastic fails):", Value = r.GptElasticApprovedBy, Weight = 6 },
                new RowField { Label = "Approved Date:", Value = r.GptElasticApprovedDate, Weight = 6 });

            builder.AddFieldsRow(
                new RowField { Label = "Zipper Report NO.:", Value = r.ZipperRptNo, Weight = 6 },
                new RowField { Label = "Zipper Status:", Value = r.ZipperStatus, Weight = 6 });
            builder.AddFieldsRow(
                new RowField { Label = "Approved By (If Zipper fail ):", Value = r.ZipperApprovedBy, Weight = 6 },
                new RowField { Label = "Approval Date:", Value = r.ZipperApprovedDate, Weight = 6 });

            builder.AddFieldsRow(
                new RowField { Label = "No. Of Bags/Carton:", Value = $"{r.NoOfBagsCartonsUnit}", Weight = 6 },
                new RowField { Label = "Packing Status:", Value = $"(100% Packed) - {r.PackingStatus}", Weight = 6 });
            builder.AddFieldsRow(
                new RowField { Label = "Inline Done:", Value = r.InlineDone, Weight = 6 },
                new RowField { Label = "Packing List:", Value = r.PackingList, Weight = 6 });
            builder.AddFieldsRow(
                new RowField { Label = "PPS Release Date:", Value = r.PpsReleaseDate ?? "", Weight = 6 },
                new RowField { Label = "PPS Expiry Date:", Value = r.PpsExpiryDate ?? "", Weight = 6 });

            builder.AddWrappedKeyValue("MFG Date:", r.MfgDate ?? "");

            builder.AddFieldsRow(
                new RowField { Label = "Season:", Value = r.Season, Weight = 4 },
                new RowField { Label = "Color Code:", Value = r.ColorCode, Weight = 4 });

            builder.AddWrappedKeyValue("Fiber Content on (PO Copy):", r.FiberContentOnPo);
            builder.AddWrappedKeyValue("Fiber Content on (Test Report):", r.FiberContentOnTestReport);
            builder.AddWrappedKeyValue("Fiber Content on W/C Label:", r.FiberContentOnWc);

            builder.AddFieldsRow(
                new RowField { Label = "GSM on (PO Copy):", Value = r.GsmOnPo, Weight = 6 },
                new RowField { Label = "GSM on (Test Report):", Value = r.GsmOnTestReport, Weight = 6 });

            builder.AddFieldsRow(
               new RowField { Label = "GSM on Bulk:", Value = r.GsmOnBulk, Weight = 6 },
                new RowField { Label = "FIT:", Value = r.InspectionFit, Weight = 6 });

            builder.AddWrappedKeyValue("Selected Bags/Cartons No.:", r.SelectedBagCartonsNo);

            builder.AddFieldsRow(
                new RowField { Label = "Workmanship:", Value = r.Workmanship, Weight = 6 },
                new RowField { Label = "Fail Reason:", Value = r.FailReason, Weight = 6 });
            builder.AddFieldsRow(
                new RowField { Label = "AQL", Value = "Major/Minor 2.5/2.5", Weight = 6 },
                new RowField { Label = "AQL Sample Size:", Value = $"{r.AqlSampleSize} PCS", Weight = 6 });

            builder.AddSpacing(15);

            builder.AddSectionBar("Conformity & Defects Observed");
            // ---- Conformity: two side-by-side tables ----
            builder.AddSideBySideTables(
                left: new TableSpec
                {
                    //Title = "Conformity",
                    Columns = new List<TableColumn>
                    {
                    new() { Header = "Conformity", Weight = 5 },
                    new() { Header = "PASS / FAIL", Weight = 1, Align = TextAlign.Right }
                    },
                    Rows = new List<List<string>>
                    {
                    new() { $"Size : {r.ConformitySize}", r.ConformitySizeStatus },
                    new() { "Embroidery/Print (Should be as per PPS)", r.EmbroideryPf },
                    new() { "Accessories (Should be as per PPS)", r.AccessoriesPf },
                    new() { "Barcode Compliances", r.BarcodePf },
                    new() { "No Plastic Tag/Kimble/Tag", r.NoPlasticPf },
                    new() { "BOGO / Discount Tag", r.BogoPf }
                    },
                    ChipColumns = new HashSet<int> { 1 }
                },
                right: new TableSpec
                {
                    //Title = "Conformity",
                    Columns = new List<TableColumn>
                    {
                    new() { Header = "Conformity", Weight = 5 },
                    new() { Header = "YES / NO", Weight = 1, Align = TextAlign.Right }
                    },
                    Rows = new List<List<string>>
                    {
                    new() { $"Pre-Pack Ratio : {r.ConformityPrepack}", r.ConformityPrepackStatus },
                    new() { "Thread From Nominated Vendor", r.ThreadYn },
                    new() { "Button From Nominated Vendor", r.ButtonYn },
                    new() { "Zipper From Nominated Vendor", r.ZipperYn },
                    new() { "Interlining From Nominated Vendor", r.InterliningYn },
                    new() { "Elastic From Nominated Vendor", r.ElasticYn }
                    },
                    ChipColumns = new HashSet<int> { 1 }
                });

            //builder.AddSpacing(12);

            // ---- Measurement sample size + Check carton/pre-pack: side by side ----

            builder.AddSideBySideTables(
            left: new TableSpec
            {
                ColSpanHeaderText = $"Measurement Sample Size :- {r.MeasurementSampleQty} PCS",
                Columns = new List<TableColumn>
                {
                    new() { Header = "", Weight = 3 },
                    new() { Header = "", Weight = 2, Align = TextAlign.Right }
                },
                Rows = new List<List<string>>
                {
                    new() { "WITHIN TOLERANCE", "PASS / FAIL" },
                    new() { "Measurement", r.MeasurementSampleSizeResult }
                },
                ChipColumns = new HashSet<int> { 1 },
                BoldRows = new HashSet<int> { 0 }
            },
            right: new TableSpec
            {
                ColSpanHeaderText = "No. of Check Carton/Bags and Pre-Pack",
                Columns = new List<TableColumn>
                {
                    new() { Header = "", Weight = 2.5 },
                    new() { Header = "", Weight = 2.5 }
                },
                Rows = new List<List<string>>
                {
                    new() { "Carton/ Bag No.", $" {r.NoOfCheckUnit} OPEN" },
                    new() { "No. of Pre-Pack Check", $"{r.NoOfPrepackCheck} PRE PACK" }
                }
            });

            //builder.AddSpacing(12);

            // ---- Defects table ----
            //builder.AddSectionBar("Defects Observed");
            //builder.AddChip("Defects Observed", ChipColor.Secondary);

            var defectRows = r.Defects
                .Select(d => new List<string>
                {
                (d.DefectsId == 1001 || d.DefectsId == 1002) ? "" : d.SNo.ToString(),
                d.Defect,
                d.Major.ToString(),
                d.Minor.ToString(),
                d.Status
                })
                .ToList();
            var totalRowIndex = r.Defects
                .Select((d, i) => (d, i))
                .Where(x => x.d.DefectsId == 1001)
                .Select(x => x.i)
                .ToHashSet();
            var AllowedRowIndex = r.Defects
                .Select((d, i) => (d, i))
                .Where(x => x.d.DefectsId == 1002)
                .Select(x => x.i)
                .ToHashSet();

            builder.AddTable(
                columns: new List<TableColumn>
                {
                new() { Header = "SR.No.", Weight = 1, Align = TextAlign.Left },
                new() { Header = "DEFECT", Weight = 4 },
                new() { Header = "MAJOR", Weight = 1, Align = TextAlign.Left },
                new() { Header = "MINOR", Weight = 1, Align = TextAlign.Left },
                new() { Header = "STATUS", Weight = 1, Align = TextAlign.Left }
                },
                rows: defectRows,
                chipColumns: new HashSet<int> { 4 },
                boldRows: totalRowIndex,
                iconRows: AllowedRowIndex
            );

            //builder.AddSpacing(24);

            builder.AddSectionBar("Inspection Fail/Remark");//"Inspection Sign-Off" or "Signatures & Stamps"
            builder.AddWrappedKeyValue("Fail/Remark Comment:", r.RemarkComment);
            builder.AddWrappedKeyValue("Note -", r.Note);

            // ---- Final approval / remarks ----
            builder.AddSectionBar("Commercial Approval & Remarks");//"Inspection Sign-Off" or "Signatures & Stamps"

            builder.AddWrappedKeyValue("FI Comm. Appr. Date:", r.CommApprovalDate ?? "");
            builder.AddWrappedKeyValue("FI Comm. Approved By:", r.CommApprovedBy);
            builder.AddWrappedKeyValue("Comm. Approval Document:", r.CommApprovalDoc);
            builder.AddWrappedKeyValue("Debit Amount:", $"{r.DebitAmount.ToString("C0", new CultureInfo("en-IN"))}/-");
            builder.AddWrappedKeyValue("Admin Remark:", r.AdminRemark);

            string stampImagePath = _rootPaths + "\\resources\\signstamp\\Stamp04.png";

            builder.AddSpacing(4);

            builder.AddSignatureBlocks(
             left: new SignatureBlockInfo
             {
                 HeaderTitle = "Name of VMM QA",
                 Name = $"{r.InspectorName}",
                 SignatureImagePath = "",//_rootPaths + "\\signstamp\\Sign04.png",
                 SignatureImage = r.QASign,
                 StampImagePath = stampImagePath,// _rootPaths + "\\signstamp\\Stamp04.png",
                 DateOfSignature = "",
                 StampNo = r.QAStampNo
             },
             right: new SignatureBlockInfo
             {
                 HeaderTitle = "Name of the Factory Representative",
                 Name = r.FactoryRepresentative,
                 SignatureImagePath = "",//_logoPath,
                 StampImagePath = "",// _logoPath,
                 DateOfSignature = ""
             });
            if (r.Is_Edit_By_Admin)
            {
                builder.AddSpacing(15);
                builder.AddSignatureBlock(
                            left: new SignatureBlockInfo
                            {
                                HeaderTitle = "Name of Admin/QAM",
                                Name = r.AdminName,
                                SignatureImagePath = "",// _rootPaths + "\\signstamp\\Sign05.png",
                                SignatureImage = r.AdminSign,
                                StampImagePath = stampImagePath,//_rootPaths + "\\signstamp\\Stamp04.png",
                                DateOfSignature = "",
                                StampNo = r.AdminStampNo
                            });
            }

            builder.AddSpacing(15);
            if (r.QADocuments?.Any() == true)
            {
                builder.AddSectionBar("QA Uploaded Documents");
                foreach (var item in r.QADocuments)
                {
                    builder.AddHyperlink($"{item.Title}", ToPublicUrl(_publicBaseUrl, item.Url));// ToFileUri(_publicBaseUrl, item.Url)
                }
            }


            builder.AddSpacing(15);
            if (r.AdminDocuments?.Any() == true)
            {
                builder.AddSectionBar("Admin Uploaded Documents");
                foreach (var item in r.AdminDocuments)
                {
                    builder.AddHyperlink($"{item.Title}", ToPublicUrl(_publicBaseUrl, item.Url));
                }
            }


            builder.WithPageNumbers();
            builder.WithWatermark("VISHAL MEGA MART\n INSPECTION REPORT", opacity: 0.10);//CONFIDENTIAL
            //// ---- Attachments: QA Documents, then Admin Documents, in order ----
            //for (int i = 0; i < _qaAttachmentPaths.Count; i++)
            //{
            //    builder.AddAttachment(_qaAttachmentPaths[i], $"QA Documents - Attachment {i + 1}");
            //}

            //for (int i = 0; i < _adminAttachmentPaths.Count; i++)
            //{
            //    builder.AddAttachment(_adminAttachmentPaths[i], $"Admin Documents - Attachment {i + 1}");
            //}


            return builder.ToBytes();
        }

        public byte[] GenerateAbortivePdf()
        {
            var r = _r;
            using var builder = new PdfBuilder();
            var (rpt, sup, title) = GetInspectionLabel(r.InspectionTypeId);
            string reportTitleLine = rpt == "Inline" ? title : $"{rpt}{sup} - {title}";
            // ---- Header: company name, PASS/FAIL chip, report title ----
            builder.AddLetterhead(new LetterheadInfo
            {
                CompanyName = "Vishal Mega Mart Limited",
                Tagline = "Quality & Process Standards",
                LogoPath = _rootPaths + "\\resources\\vishal-mega-mart-logo.png",
                DocumentTitle = reportTitleLine,
                ReferenceNumber = $"Report #:{r.ReportNo}",
                FooterInfo = $" [ PO:{r.PONumber} | Report #:{r.ReportNo} ] \n This is system generated inspection report.",
                DateOfInspection = $"FI Date: {r.FIDate?.ToString("dd MMM, yyyy hh:mm tt") ?? ""}",
                Last_Amendment_Date = r.Last_Amendment_Date != null ? $"Last Amendment On: {r.Last_Amendment_Date?.ToString("dd MMM, yyyy hh:mm tt") ?? ""}" : ""
            });

            builder.AddChip(r.InspectionResultText, r.IsPass ? ChipColor.Success : ChipColor.Error, TextAlign.Center, TextStyle.ResultChip);

            // ---- Buyer / Report No ----
            builder.AddSpacing(8);
            builder.AddWrappedKeyValue("Buyer Name:-", $"{r.BuyerName}");
            builder.AddSectionBar("PO Details");

            // ---- Basic info grid ----
            builder.AddWrappedKeyValue("PO No:", $"{r.PONumber} ");
            builder.AddWrappedKeyValue("Vendor:", $"{r.VendorName} ({r.VendorCode})");
            builder.AddWrappedKeyValue("Vendor Location:", $"{r.VendorLocation}");

            string descFitMrp = string.Join(" / ", new[]
            {
            string.IsNullOrEmpty(r.MCText) ? "---" : r.MCText,
            string.IsNullOrEmpty(r.Fit) ? "---" : r.Fit,
            r.MRP > 0 ? r.MRP.ToString() : "---"});

            builder.AddWrappedKeyValue("Order Quantity:", $"{r.POTotalQty.ToString("0")}");

            string categoryLabelColor = string.Join(" / ", new[]
            {
            string.IsNullOrEmpty(r.Category) ? "---" : r.Category,
            string.IsNullOrEmpty(r.BrandId) ? "---" : r.BrandId,
            string.IsNullOrEmpty(r.Color) ? "---" : r.Color});

            builder.AddWrappedKeyValue("Offered Quantity:", $"{r.OfferedQuantity.ToString()}");
            builder.AddWrappedKeyValue("Ship Qty.:", $"{r.OfferedQuantity.ToString()}");
            builder.AddWrappedKeyValue("Description/FIT/MRP:", $"{descFitMrp}");
            builder.AddWrappedKeyValue("Category/Label/Color:", $"{categoryLabelColor}");
            builder.AddWrappedKeyValue("Article Number:", r.ArticleNumber);

            // ---- Abortive Reason ----
            builder.AddSpacing(4);
            builder.AddSectionBar("Reason(s) for Abortive Inspection");

            var reasonRows = r.Reasons.Select(d => new List<string> { d.SNo.ToString(), d.Reason }).ToList();
            builder.AddTable(
                columns: new List<TableColumn>
                {
                new() { Header = "Sr.No.", Weight = 0.5, Align = TextAlign.Left },
                new() { Header = "Reason", Weight = 6 }
                },
                rows: reasonRows
            );

            builder.AddSpacing(4);

            builder.AddSectionBar("Acceptance of Charges");//"Inspection Sign-Off" or "Signatures & Stamps"
            builder.AddText("I hereby acknowledge and accept that the inspection has been postponed due to the above reason(s), and I agree to bear and pay the charges listed below.");

            builder.AddText("Postponement Charge / P.O :");//Postponement Charges (Per P.O.)

            builder.AddWrappedKeyValue("Travel and Other Expenses : ", $"{r.Amt_Travelling.ToString("C0", new CultureInfo("en-IN"))}/-");

            string stampImagePath = _rootPaths + "\\resources\\signstamp\\Stamp04.png";

            builder.AddSpacing(4);

            builder.AddSignatureBlocks(
             left: new SignatureBlockInfo
             {
                 HeaderTitle = "Name of VMM QA",
                 Name = $"{r.InspectorName}",
                 SignatureImagePath = "",//_rootPaths + "\\signstamp\\Sign04.png",
                 SignatureImage = r.QASign,
                 StampImagePath = stampImagePath,// _rootPaths + "\\signstamp\\Stamp04.png",
                 DateOfSignature = "",
                 StampNo = r.QAStampNo
             },
             right: new SignatureBlockInfo
             {
                 HeaderTitle = "Name of the Factory Representative",
                 Name = r.FactoryRepresentative,
                 SignatureImagePath = "",//_logoPath,
                 StampImagePath = "",// _logoPath,
                 DateOfSignature = ""
             });
            if (r.Is_Edit_By_Admin)
            {
                builder.AddSpacing(15);
                builder.AddSignatureBlock(
                            left: new SignatureBlockInfo
                            {
                                HeaderTitle = "Name of Admin/QAM",
                                Name = r.AdminName,
                                SignatureImagePath = "",// _rootPaths + "\\signstamp\\Sign05.png",
                                SignatureImage = r.AdminSign,
                                StampImagePath = stampImagePath,//_rootPaths + "\\signstamp\\Stamp04.png",
                                DateOfSignature = "",
                                StampNo = r.AdminStampNo
                            });
            }

            builder.AddSpacing(10);
            if (r.QADocuments?.Any() == true)
            {
                builder.AddSectionBar("QA Uploaded Documents");
                foreach (var item in r.QADocuments)
                {
                    builder.AddHyperlink($"{item.Title}", ToPublicUrl(_publicBaseUrl, item.Url));
                }
            }


            builder.AddSpacing(10);
            if (r.AdminDocuments?.Any() == true)
            {
                builder.AddSectionBar("Admin Uploaded Documents");
                foreach (var item in r.AdminDocuments)
                {
                    builder.AddHyperlink($"{item.Title}", ToPublicUrl(_publicBaseUrl, item.Url));
                }
            }

            builder.WithPageNumbers();
            builder.WithWatermark("VISHAL MEGA MART\n INSPECTION REPORT", opacity: 0.10);//CONFIDENTIAL

            return builder.ToBytes();
        }

        private static (string rpt, string sup, string title) GetInspectionLabel(int typeId) => typeId switch
        {
            1 => ("1", "st", "Final Inspection Report"),
            2 => ("2", "nd", "Final Inspection Report"),
            3 => ("3", "rd", "Final Inspection Report"),
            4 => ("Inline", "", "Inline Inspection Report"),
            _ => ("", "", "Inspection Report")
        };

        private static string ToFileUri(string rootPath, string relativeUrl)
        {
            string combined = Path.Combine(rootPath, relativeUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
            return new Uri(combined).AbsoluteUri;
        }

        private static string ToPublicUrl(string baseUrl, string relativeUrl)
        {
            return baseUrl.TrimEnd('/') + "/" + relativeUrl.TrimStart('/');
        }
    }

    public static class AttachmentPathResolver
    {
        /// <summary>
        /// Resolves comma-separated attachment references (absolute paths,
        /// URLs, or paths relative to wwwroot) into absolute local file paths.
        ///
        /// `webRootPath` should be the PHYSICAL wwwroot folder - e.g.
        /// IWebHostEnvironment.WebRootPath from your API project's Program.cs
        /// or a controller/service. This class deliberately does NOT take an
        /// IWebHostEnvironment directly (Infrastructure shouldn't depend on
        /// ASP.NET Core hosting types) - the caller in your API layer passes
        /// the already-resolved string instead. See the usage note below.
        ///
        /// `knownBaseUrl` (optional): if the PDF is generated on the SAME
        /// server that serves these files (e.g. attachments stored under
        /// wwwroot are also reachable at
        /// "https://qps.vishalmegamart.com:8024/Images/..."), pass that base
        /// URL here (e.g. "https://qps.vishalmegamart.com:8024") so a matching
        /// reference is mapped straight to the local file instead of making
        /// an HTTP round-trip back to itself - faster, and avoids depending on
        /// the site being reachable, TLS/cert issues, or firewall rules for
        /// that outbound call. Only references that DON'T match knownBaseUrl
        /// (truly external URLs, if any) fall through to an actual download.
        /// </summary>
        public static async Task<List<string>> ResolveAsync(
            string? commaSeparatedRefs,
            string webRootPath,
            string urlDownloadCacheFolder,
            HttpClient? httpClientForUrls = null,
            string? knownBaseUrl = null)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(commaSeparatedRefs)) return result;

            Directory.CreateDirectory(urlDownloadCacheFolder);

            foreach (var raw in commaSeparatedRefs.Split(','))
            {
                var reference = raw.Trim();
                if (string.IsNullOrEmpty(reference) || !reference.Contains('.')) continue;

                bool isUrl = reference.StartsWith("http://") || reference.StartsWith("https://");

                if (isUrl && !string.IsNullOrEmpty(knownBaseUrl) && reference.StartsWith(knownBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    // Same-server URL - strip the base and treat the rest as a
                    // wwwroot-relative path, no network call needed.
                    var relativeFromUrl = reference[knownBaseUrl.Length..].TrimStart('/', '\\');
                    var localFromUrl = Path.Combine(webRootPath, relativeFromUrl.Replace('/', Path.DirectorySeparatorChar));

                    if (File.Exists(localFromUrl))
                    {
                        result.Add(localFromUrl);
                        continue;
                    }
                    // Falls through to the HTTP branch below if the local
                    // mapping didn't actually exist (e.g. knownBaseUrl or the
                    // wwwroot mapping doesn't match reality) - safer than
                    // silently failing.
                }

                if (isUrl)
                {
                    if (httpClientForUrls is null)
                    {
                        throw new InvalidOperationException($"'{reference}' is a URL but no HttpClient was provided to download it.");
                    }

                    var fileName = Path.GetFileName(new Uri(reference).LocalPath);
                    var localPath = Path.Combine(urlDownloadCacheFolder, fileName);

                    var bytes = await httpClientForUrls.GetByteArrayAsync(reference);
                    await File.WriteAllBytesAsync(localPath, bytes);
                    result.Add(localPath);
                }
                else if (Path.IsPathRooted(reference) && File.Exists(reference))
                {
                    result.Add(reference); // already an absolute local/network path
                }
                else
                {
                    // Relative path as stored in the DB, e.g. "/Attachments/photo1.jpg"
                    // or "Attachments\photo1.jpg" - resolve against the physical
                    // wwwroot folder passed in from the API layer.
                    var relative = reference.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                    var resolved = Path.Combine(webRootPath, relative);

                    if (File.Exists(resolved))
                    {
                        result.Add(resolved);
                    }
                    else
                    {
                        throw new FileNotFoundException(
                            $"Attachment reference '{reference}' did not resolve to an existing file at '{resolved}'. " +
                            "Check that webRootPath matches your actual wwwroot and that the DB-stored path is relative to it.",
                            resolved);
                    }
                }
            }

            return result;
        }
    }





}
