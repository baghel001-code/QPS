using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace QPS.Tests.Architecture
{
    /// <summary>
    /// Keeps the layers pointing one way: Domain ← Application ← Infrastructure ← (Admin, QPSApi),
    /// and checks the QPSApi controller conventions the portal's API master relies on (api/v1/[Action]).
    /// </summary>
    public class LayeringTests
    {
        private static readonly Assembly Domain = typeof(global::Domain.Records.AuthUser).Assembly;
        private static readonly Assembly Application = typeof(global::Application.Services.PasswordHasher).Assembly;
        private static readonly Assembly Infrastructure = typeof(global::Infrastructure.Middleware.StaticTokenMiddleware).Assembly;
        private static readonly Assembly Api = typeof(global::QPSApi.Controllers.V1.MenuController).Assembly;

        private static IEnumerable<string> References(Assembly a) => a.GetReferencedAssemblies().Select(n => n.Name!);

        [Fact]
        public void Domain_depends_on_no_other_qps_project() =>
            Assert.Empty(References(Domain).Intersect(["Application", "Infrastructure", "Admin", "QPSApi"]));

        [Fact]
        public void Application_does_not_depend_on_infrastructure_or_the_apps() =>
            Assert.Empty(References(Application).Intersect(["Infrastructure", "Admin", "QPSApi"]));

        [Fact]
        public void Infrastructure_does_not_depend_on_the_apps() =>
            Assert.Empty(References(Infrastructure).Intersect(["Admin", "QPSApi"]));

        public static IEnumerable<object[]> Controllers() =>
            Api.GetTypes()
               .Where(t => t is { IsClass: true, IsAbstract: false } && t.Name.EndsWith("Controller") && typeof(ControllerBase).IsAssignableFrom(t))
               .Select(t => new object[] { t.Name });

        [Fact]
        public void The_api_has_controllers() => Assert.NotEmpty(Controllers());

        [Theory]
        [MemberData(nameof(Controllers))]
        public void Every_api_controller_is_an_ApiController_under_api_v1(string name)
        {
            var type = Api.GetTypes().Single(t => t.Name == name);
            Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
            var route = type.GetCustomAttribute<RouteAttribute>()?.Template ?? "";
            Assert.StartsWith("api/v1/", route, StringComparison.OrdinalIgnoreCase);
        }
    }
}
