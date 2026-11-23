using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Records
{
    public record AsnDetail
    {
        [JsonPropertyName("ASN")]
        public string Asn { get; set; } = string.Empty;
    }

    public record AsnLookupResult(bool Found, List<AsnDetail>? ASN_DETAILS, string? ErrorMessage, DataOrigin Origin = DataOrigin.NotFound);

    public record AsnList(bool Found, List<string> ASN_NUMBER, string? ErrorMessage);




}
