using Domain.Entities.Modals;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Static
{
    public static class InspectionDialogResolver
    {
        public static InspectionDialogType Resolve(UserRoleContext role)
        {
            if (role.IsVendor) return InspectionDialogType.VendorInspectionRequest;
            if (role.IsASN) return InspectionDialogType.AsnInspection;
            if (role.IsQA || role.IsQAM ) return InspectionDialogType.QaInspection;
            if (role.IsBFT || role.IsBuyer || role.IsQAAdmin) return InspectionDialogType.BftAsnRequest;

            throw new InvalidOperationException("No dialog type resolved for your role.");
        }
    }
}
