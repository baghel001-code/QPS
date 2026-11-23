using Admin.Interfaces;
using Domain.Entities.Modals;
using Domain.Static;

namespace Admin.Services
{
    public class InspectionDialogLauncherFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public InspectionDialogLauncherFactory(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public IInspectionDialogLauncher Resolve(UserRoleContext role)
        {
            var dialogType = InspectionDialogResolver.Resolve(role);
            return _serviceProvider.GetRequiredKeyedService<IInspectionDialogLauncher>(dialogType);
        }
        public IAsnDialogLauncher ResolveAsn(UserRoleContext role)
        {
            var dialogType = InspectionDialogResolver.Resolve(role);
            return _serviceProvider.GetRequiredKeyedService<IAsnDialogLauncher>(dialogType);
        }
    }
}
