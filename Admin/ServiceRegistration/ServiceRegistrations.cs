using Application.Interfaces.V1;
using Application.Interfaces.V1.Asn;
using Application.Interfaces.V1.Notifications;
using Application.Interfaces.V1.User;
using Application.Notifications;
using Application.Notifications.Resolvers;
using Application.Notifications.Templates;
using Application.Services.Notifications;
using Application.Services.User;
using BSRApplication.Interfaces.V1;
using Infrastructure.Email;
using Infrastructure.Repositories;
using Infrastructure.Services.V1;
using Infrastructure.Services.V1.Asn.Infrastructure.Services;
using Infrastructure.Services.V1.User;
using Infrastructure.Utilitys;
using PDFKit;
using PdfSharp.Fonts;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Admin.ServiceRegistration
{
    public static class ServiceRegistrations
    {
        public static IServiceCollection AddDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IRecipientPolicy, RecipientPolicy>();

            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IUserDirectoryService, UserDirectoryService>();

            services.AddScoped<IEmailTemplate<PpsStatusChangedModel>, PpsStatusChangedTemplate>();
            services.AddScoped<IRecipientResolver<PpsStatusChangedModel>, PpsBuyerRecipientResolver>();
            services.AddScoped<INotifier<PpsStatusChangedModel>, EmailNotifier<PpsStatusChangedModel>>();

            services.AddScoped<IEmailTemplate<PpsRequestCreatedModel>, PpsRequestCreatedTemplate>();
            services.AddScoped<IRecipientResolver<PpsRequestCreatedModel>, PpsRoleRecipientResolver>();
            services.AddScoped<INotifier<PpsRequestCreatedModel>, EmailNotifier<PpsRequestCreatedModel>>();

            services.AddScoped<IEmailTemplate<InspectionCreatedModel>, InspectionCreatedTemplate>();
            services.AddScoped<IRecipientResolver<InspectionCreatedModel>, QaByRegionRecipientResolver>();
            services.AddScoped<INotifier<InspectionCreatedModel>, EmailNotifier<InspectionCreatedModel>>();

            services.AddScoped<IEmailTemplate<VendorAccountModel>, VendorAccountTemplate>();
            services.AddScoped<IRecipientResolver<VendorAccountModel>, VendorAccountRecipientResolver>();
            services.AddScoped<INotifier<VendorAccountModel>, EmailNotifier<VendorAccountModel>>();

            services.AddScoped<IEmailTemplate<ForgotPasswordModel>, ForgotPasswordTemplate>();
            services.AddScoped<IRecipientResolver<ForgotPasswordModel>, ForgotPasswordRecipientResolver>();
            services.AddScoped<INotifier<ForgotPasswordModel>, EmailNotifier<ForgotPasswordModel>>();
        
            services.AddScoped<IEmailTemplate<PasswordResetLinkModel>, PasswordResetLinkTemplate>();
            services.AddScoped<IRecipientResolver<PasswordResetLinkModel>, PasswordResetLinkRecipientResolver>();
            services.AddScoped<INotifier<PasswordResetLinkModel>, EmailNotifier<PasswordResetLinkModel>>();

            services.AddScoped<IEmailTemplate<DoiConfirmedModel>, DoiConfirmedTemplate>();
            services.AddScoped<IRecipientResolver<DoiConfirmedModel>, DoiConfirmedRecipientResolver>();
            services.AddScoped<INotifier<DoiConfirmedModel>, EmailNotifier<DoiConfirmedModel>>();

            services.AddScoped<IEmailTemplate<DailyScheduleModel>, DailyScheduleTemplate>();
            services.AddScoped<IRecipientResolver<DailyScheduleModel>, DailyScheduleRecipientResolver>();
            services.AddScoped<INotifier<DailyScheduleModel>, EmailNotifier<DailyScheduleModel>>();

            services.AddScoped<IAttachmentProvider<DailyScheduleModel>, DailyScheduleAttachmentProvider>();

            services.AddScoped<IEmailTemplate<AsnGeneratedModel>, ASNGeneratedTemplate>();
            services.AddScoped<IRecipientResolver<AsnGeneratedModel>, ASNGeneratedRecipientResolver>();
            services.AddScoped<INotifier<AsnGeneratedModel>, EmailNotifier<AsnGeneratedModel>>();


            services.AddScoped<IEmailTemplate<TestMailModel>, TestEmailTemplate>();
            services.AddScoped<IRecipientResolver<TestMailModel>, TestMailRecipientResolver>();
            services.AddScoped<INotifier<TestMailModel>, EmailNotifier<TestMailModel>>();
            

            return services;
        }
    }
}
