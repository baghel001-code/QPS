using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Domain.Records
{
    public record LoginUser(string Id, string EmpCode, string Name, string MobilePhone, string Email, string Role, string SessionToken, Modules Modules, CopLoginRes copLoginRes)
    {
        public Claim[] ToClaims()
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, EmpCode),
                    new Claim(ClaimTypes.Name, Name),
                    new Claim(ClaimTypes.MobilePhone, MobilePhone),
                    new Claim(ClaimTypes.Email, Email),
                    new Claim(ClaimTypes.Role, Role),
                    new Claim(CustomClaimTypes.User_Id, Id),
                    new Claim(CustomClaimTypes.User_Management, Modules.User_Management.ToString()),
                    new Claim(CustomClaimTypes.Media, Modules.Media.ToString()),
                    new Claim(CustomClaimTypes.Menu, Modules.Menu.ToString()),
                    new Claim(CustomClaimTypes.CMS, Modules.CMS.ToString()),
                    new Claim(CustomClaimTypes.RGNS, copLoginRes.UserDetails.REGION_IDS?? string.Empty),
                    new Claim(CustomClaimTypes.SessionToken, SessionToken),
                    new Claim(CustomClaimTypes.UserType, "EMPLOYEE"),

                };

            // one Role claim PER role — IsInRole works unchanged for each value
            //Pass multi role per user ->  List<string> Roles in LogingUser record
            //foreach (var role in Roles)
            //{
            //    claims.Add(new Claim(ClaimTypes.Role, role));
            //}

            return claims.ToArray();
        }
    }

    public record LogedInVendor(string Id, string Code, string Name, string Gst, string SessionToken,string Role = "Vendor")
    {
        public Claim[] ToClaims()
        {
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Code),
                    new Claim(ClaimTypes.Name, Name),
                    new Claim(CustomClaimTypes.User_Id, Id),
                    new Claim(CustomClaimTypes.GST, Gst),
                    new Claim(ClaimTypes.Role, Role),
                    new Claim(CustomClaimTypes.SessionToken, SessionToken),
                    new Claim(CustomClaimTypes.UserType, "VENDOR"),
                };


            return claims.ToArray();
        }
    }

    public record SerializableClaim(string Type, string Value);
}
