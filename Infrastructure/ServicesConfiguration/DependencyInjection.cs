using Application.Interfaces.V1;
using Application.Interfaces.V1.Asn;
using Application.Interfaces.V1.User;
using BSRApplication.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.V1;
using Infrastructure.Services.V1.Asn.Infrastructure.Services;
using Infrastructure.Services.V1.User;
using Infrastructure.Utilitys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PDFKit;
using PdfSharp.Fonts;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ServicesConfiguration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, int useProxyValue, string logFilePath, LoggingLevelSwitch levelSwitch)
        {
            // ─── SERILOG SETUP ───────────────────────────────────────────

            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Information() //implement stop and reduce logging
                .MinimumLevel.ControlledBy(levelSwitch)
                // Suppress noisy Microsoft logs
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)

                // Add context to every log
                .Enrich.FromLogContext()
                //.Enrich.WithMachineName()
                //.Enrich.WithThreadId()

                // ── Console (dev only) ──────────────────────────────
                .WriteTo.Console(
                    outputTemplate:
                    "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}")

                // ── Info logs ───────────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level == LogEventLevel.Information)
                    .WriteTo.File(
                        path: $"{logFilePath}Info/info-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} {Message}{NewLine}"))
                // "{Timestamp:yyyy-MM-dd HH:mm:ss} {Message:lj} {Properties:j}{NewLine}{Exception}"))


                // ── Warning logs ────────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level == LogEventLevel.Warning)
                    .WriteTo.File(
                        path: $"{logFilePath}Warnings/warning-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}"))

                // ── Error logs ──────────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level == LogEventLevel.Error)
                    .WriteTo.File(
                        path: $"{logFilePath}Errors/error-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}"))
                // ── Critical logs ───────────────────────────────────
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e =>
                        e.Level == LogEventLevel.Fatal)
                    .WriteTo.File(
                        path: $"{logFilePath}Critical/critical-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 90, // keep critical longer
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}"))

                .CreateLogger();

            // Wire Serilog into .NET's ILogger
            services.AddLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddSerilog(Log.Logger);
            });

            // ─── EXISTING REGISTRATIONS (unchanged) ─────────────────
            //Register ApiMasterService as IAPICall
            services.AddSingleton<IAPICall>(provider => new APICallService(useProxyValue));
            // Register DapperHelper (add it to the DI container)
            services.AddSingleton<DapperHelper>();
            //// Register APIService as IAPI
            services.AddScoped<IAPI, APIService>();
            // Register APIService as IAPI
            services.AddScoped<IUser, UserService>();
            services.AddScoped<ISessionTokenService, SessionTokenService>();
            services.AddScoped<IUserAuthServices, UserAuthServices>();

            services.AddScoped<IEmailManager, EmailManager>();

            services.AddScoped<IVendor, VendorService>();
            services.AddScoped<IAsnRequestService, AsnRequestService>();
            //-----------Start Audit Services Registration-------------

            // INFRASTRUCTURE — Dapper Repositories
            services.AddScoped<IAuditRepository, AuditRepository>();
            services.AddScoped<IMasterRepository, AuditMasterRepository>();
            // APPLICATION — Business Services
            services.AddScoped<IAuditWizardService, AuditWizardService>();
            services.AddScoped<IMasterDataService, MasterDataService>();


            //-----------End Audit

            services.AddScoped<IVendorAudit, VendorAuditService>();
            // Register RoleService as IRole
            services.AddScoped<IRole, RoleService>();
            // Register PPSRequestService as IPPSRequest
            services.AddScoped<IPPSRequest, PPSRequestService>();
            // Register RoleService as IRole
            services.AddScoped<IMenu, MenuService>();
            // Register SetFlagsService as ISetFlags
            services.AddScoped<ISetFlags, SetFlagsService>();

            // Register AnyListService as IAnyList
            services.AddScoped<IAnyList, AnyListService>();
            GlobalFontSettings.FontResolver = new SimpleFontResolver(Path.Combine(AppContext.BaseDirectory, "Fonts"));
            return services;
        }
    }
}
