using Domain.Entities.Base;
using System.IO;
using System.Text.Json.Serialization;

namespace Domain.Entities.Response
{
    public class GSTINResponse : BaseResponse
    {
        [JsonPropertyName("gstin")]
        public string Gstin { get; set; } = string.Empty;

        [JsonPropertyName("lgnm")]
        public string LegalName { get; set; } = string.Empty;

        [JsonPropertyName("stj")]
        public string StateJurisdiction { get; set; } = string.Empty;

        [JsonPropertyName("ctj")]
        public string CentralJurisdiction { get; set; } = string.Empty;

        [JsonPropertyName("rgdt")]
        public string RegistrationDate { get; set; } = string.Empty;

        [JsonPropertyName("ctb")]
        public string ConstitutionOfBusiness { get; set; } = string.Empty;

        [JsonPropertyName("dty")]
        public string TaxpayerType { get; set; } = string.Empty;

        [JsonPropertyName("nba")]
        public List<string> NatureOfBusinessActivities { get; set; } = new();

        [JsonPropertyName("sts")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("cxdt")]
        public string CancellationDate { get; set; } = string.Empty;

        [JsonPropertyName("tradeNam")]
        public string TradeName { get; set; } = string.Empty;

        [JsonPropertyName("stjCd")]
        public string StateJurisdictionCode { get; set; } = string.Empty;

        [JsonPropertyName("ctjCd")]
        public string CentralJurisdictionCode { get; set; } = string.Empty;

        [JsonPropertyName("pradr")]
        public PrincipalAddressDto? PrincipalAddress { get; set; }

        [JsonPropertyName("adadr")]
        public List<AdditionalAddressDto> AdditionalAddresses { get; set; } = new();

        [JsonPropertyName("filingFreq")]
        public string FilingFrequency { get; set; } = string.Empty;

        [JsonPropertyName("lastUpdated")]
        public string LastUpdated { get; set; } = string.Empty;


    }

    public class PrincipalAddressDto
    {
        [JsonPropertyName("fullAddress")]
        public string? FullAddress { get; set; }

        [JsonPropertyName("addr")]
        public AddressDetailDto? AddressDetail { get; set; }

        [JsonPropertyName("ntr")]
        public string NatureOfAddress { get; set; } = string.Empty;
    }

    public class AddressDetailDto
    {
        [JsonPropertyName("bnm")]
        public string BuildingName { get; set; } = string.Empty;

        [JsonPropertyName("st")]
        public string Street { get; set; } = string.Empty;

        [JsonPropertyName("loc")]
        public string Location { get; set; } = string.Empty;

        [JsonPropertyName("bno")]
        public string BuildingNumber { get; set; } = string.Empty;

        [JsonPropertyName("stcd")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("flno")]
        public string FloorNumber { get; set; } = string.Empty;

        [JsonPropertyName("lt")]
        public string Latitude { get; set; } = string.Empty;

        [JsonPropertyName("lg")]
        public string Longitude { get; set; } = string.Empty;

        [JsonPropertyName("dst")]
        public string District { get; set; } = string.Empty;

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("pncd")]
        public string Pincode { get; set; } = string.Empty;
        // Full Address Property
        [JsonIgnore]
        public string FullAddress
        {
            get
            {
                var addressParts = new[] { BuildingNumber, FloorNumber, BuildingName, Street, Location, City, District, State, Pincode };

                return string.Join(", ", addressParts.Where(x => !string.IsNullOrWhiteSpace(x)));


            }
        }

    }

    public class AdditionalAddressDto
    {
        [JsonPropertyName("fullAddress")]
        public string? FullAddress { get; set; }

        [JsonPropertyName("addr")]
        public AddressDetailDto? AddressDetail { get; set; }

        [JsonPropertyName("ntr")]
        public string NatureOfAddress { get; set; } = string.Empty;
    }

    public class GSTINLocalDBResponse : BaseResponse
    {
        public string GST_NUMBER { get; set; } = string.Empty;
        public string VENDOR_NAME { get; set; } = string.Empty;
        public string REGISTERED_ADDRESS { get; set; } = string.Empty;
        public string GST_API_RAW_RESPONSE { get; set; } = string.Empty;
    }
}
