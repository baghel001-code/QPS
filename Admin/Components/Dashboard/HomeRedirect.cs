using Application.Interfaces.V1.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Security.Claims;

namespace Admin.Components.Dashboard;

/// <summary>
/// Decides where a signed-in user should land when they hit "/".
/// Returns null when the user should stay on the home dashboard.
/// </summary>
/// 
public static class HomeRedirect
{
    public static string? TargetFor(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return null;

        var role = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? string.Empty;
        // Employees by role
        return role switch
        {
            "1006" => "/OngoingInspections",
            "1007" or "1008" or "Vendor" => "/InspectionList",
            "1014" => "/AsnReqList",
            _ => "/V1/Login2BsrAccount"   // everyone else stays on the home dashboard
        };
    }
}
