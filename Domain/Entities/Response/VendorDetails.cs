using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Domain.Entities.Response
{
    public class VendorResponse
    {
        [JsonPropertyName("VENDOR_DETAILS")]
        public List<VendorDetail> VendorDetails { get; set; }
    }

    public class VendorDetail
    {
        [JsonPropertyName("VENDOR_CODE")]
        public string VendorCode { get; set; }

        [JsonPropertyName("VENDOR_NAME")]
        public string VendorName { get; set; }

        [JsonPropertyName("CITY")]
        public string City { get; set; }

        [JsonPropertyName("POST_CODE")]
        public string PostCode { get; set; }

        [JsonPropertyName("ADDRESS")]
        public string Address { get; set; }

        [JsonPropertyName("REGION")]
        public string Region { get; set; } = "";

        [JsonPropertyName("REGION_DESC")]
        public string RegionDescription { get; set; }

        [JsonPropertyName("GST")]
        public string GSTNo { get; set; }

        [JsonPropertyName("TELEPHONE_NUMBER")]
        public string TelephoneNumber { get; set; }

        [JsonPropertyName("TELEPHONE_NUMBER2")]
        public string TelephoneNumber2 { get; set; }

        [JsonPropertyName("TELEPHONE_NUMBER3")]
        public string TelephoneNumber3 { get; set; }

        public string ContactNo { get; set; }
        public string EmailId { get; set; }
        public string Contact_Person { get; set; } = "";

        public bool IsDefault { get; set; }

        public Guid VendorUId { get; set; }




    }

    public class VendorAddressDetail
    {
        public int AddressId { get; set; }
        public string AddressLine1 { get; set; } = "";
        public string AddressLine2 { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string PinCode { get; set; } = "";
        public string Email_Ids { get; set; } = "";
        public string Mobile_Numbers { get; set; } = "";
        public string Contact_Person { get; set; } = "";
        public bool IsDefault { get; set; }
        public string Region { get; set; } = "";
        public string RegionDescription { get; set; } = "";
        public int Created_By { get; set; }
        public string Created_Date { get; set; } = "";
        public string Updated_Date { get; set; } = "";
        public Guid AddressGUId { get; set; }
    }

    public class Vendor
    {
        public int VendorId { get; set; } = 0;
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
        public string GSTNo { get; set; } = "";
        public string PassWord { get; set; } = "";
        public bool Default_Password_Changed { get; set; }
        public bool IsActive { get; set; }
        public bool IsSentEmail { get; set; }
        public string LastLoginDate { get; set; } = "";
        public string CurrentLoginDate { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public string CreatedDate { get; set; } = "";
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; } = "";
        public string ModifiedDate { get; set; } = "";
        public int ModifiedBy { get; set; }
        public List<VendorAddressDetail> VendorAddresses { get; set; } = new List<VendorAddressDetail>();

        public Guid VendorGUId { get; set; }
        public string DefaultEmailId { get; set; } = "";


    }


    public class VendorExcel
    {
        public string EXCEL_FILE { get; set; }
        public int USER_ID { get; set; }
        public int NO_OF_RECORDS
        {
            get
            { return Data.Count; }
        }
        public int NO_INCORRECT_RECORDS
        {
            get
            { return Data.Count(a => a.IsInvalid); }
        }
        public string INCORRECT_DETAILS
        {
            get
            {
                var invalidRows = Data.Where(a => a.IsInvalid).ToList();
                var result = new
                {
                    totalInvalidRows = invalidRows.Count,
                    errors = invalidRows.Select(a => new
                    {
                        rowNumber = a.RowNumber,
                        vendorCode = a.VENDOR_CODE,
                        message = a.InvalidMessage
                    })
                };
                return JsonSerializer.Serialize(result);
            }

        }

        public List<VendorExcelDetails> Data { get; set; } = new List<VendorExcelDetails>();
        public List<VendorExcelUploadError> DataErrors
        {
            get

            {
                //return Data.Where(x => x.IsInvalid==false).Select(x => new VendorExcelUploadError
                //{
                //    RowNumber = x.RowNumber,
                //    VendorCode = x.VENDOR_CODE,
                //    Message = x.InvalidMessage
                //}).ToList();
                return new();
            }
        }

    }
    public class VendorExcelDetails
    {
        public int RowNumber { get; set; }
        public string VENDOR_CODE { get; set; }
        public string EMAIL_IDS { get; set; }
        public bool IsInvalid { get; set; }
        public string InvalidMessage { get; set; } = "";


    }
    public class VendorExcelUploadError
    {
        public int RowNumber { get; set; }
        public string VendorCode { get; set; }
        public string Message { get; set; }
    }

    public class VendorExcelValidationResult
    {
        public int TotalInvalidRows { get; set; }
        public List<VendorExcelUploadError> Errors { get; set; } = new();
    }

    //------------
    public class UpdateCM_Status_Details
    {
        public int USER_ID { get; set; }
        public List<CM_Status_Details> Data { get; set; } = new List<CM_Status_Details>();

    }
    public class CM_Status_Details
    {
        public string VendorCode { get; set; }
        public int StatusID { get; set; }
        public string EmailId { get; set; }
    }
    public class UpdateLinkPO_Details
    {
        public int INSP_ID { get; set; }
        public int USER_ID { get; set; }
        public string LinkPoNumber { get; set; }

    }

}
