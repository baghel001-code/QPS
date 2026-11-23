using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Entities.Response
{
    public class SapPoPdfResponse
    {
        [JsonPropertyName("po")]
        public string Po { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = "";

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = "";

        [JsonPropertyName("pdf_base64")]
        public string PdfBase64 { get; set; } = "";

        [JsonPropertyName("message")]
        public string message { get; set; } = "";
    }
}
