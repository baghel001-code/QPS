using MiniMapper;
using Domain.Entities.AppSettings;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using EmailService.Models;
using MyNewEncDec;

namespace Application.AutoMapper
{
    public class PasswordEncryptConverter : IValueConverter<string, string>
    {
        private readonly New_Enc_Dec _Enc_Dec;

        public PasswordEncryptConverter(New_Enc_Dec enc_Dec)
        {
            _Enc_Dec = enc_Dec;
        }

        public string Convert(string source, ResolutionContext context)
            => _Enc_Dec.My_Decode(source);
    }

    public class ServerSettingsProfile : Profile
    {

        public ServerSettingsProfile()
        {

            CreateMap<EmailServerSettings, ServerSettings>()
                .ForMember(dest => dest.SmtpServer, opt => opt.MapFrom(src => src.SmtpServer))
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.SmtpUser))
                .ForMember(dest => dest.Password, opt => opt.ConvertUsing<PasswordEncryptConverter, string>(src => src.SmtpPwd))
                .ForMember(dest => dest.Port, opt => opt.MapFrom(src => src.SmtpPort))
                .ForMember(dest => dest.UseDefaultCredentials, opt => opt.MapFrom(src => src.SmtpDefaultCredential))
                .ForMember(dest => dest.EnableSsl, opt => opt.MapFrom(src => src.EnableSsl));

        }

    }

    public class ApplicationProfile : Profile
    {
        public ApplicationProfile()
        {
            CreateMap<DateTime, string>().ConvertUsing(d => d.ToString("yyyy-MM-dd"));
            CreateMap<string, DateTime>().ConvertUsing(s => DateTime.Parse(s));

            CreateMap<Vendor, VendorModel>()
                   .ForMember(dest => dest.VENDOR_CODE, cfg => cfg.MapFrom(src => src.VendorCode))
                   .ForMember(dest => dest.VENDOR_NAME, cfg => cfg.MapFrom(src => src.VendorName))
                   .ForMember(dest => dest.VENDOR_ID, cfg => cfg.MapFrom(src => src.VendorId))
                   .ForMember(dest => dest.PASSWORD, cfg => cfg.MapFrom(src => src.PassWord))
                   .ForMember(dest => dest.DEFAULT_PASSWORD_CHANGED, cfg => cfg.MapFrom(src => src.Default_Password_Changed))
                   .ForMember(dest => dest.IP_ADDRESS, cfg => cfg.MapFrom(src => src.IPAddress))
                   .ForMember(dest => dest.CURRENT_LOGIN_DATE, cfg => cfg.MapFrom(src => src.CurrentLoginDate))
                   .ForMember(dest => dest.LAST_LOGIN_DATE, cfg => cfg.MapFrom(src => src.LastLoginDate))
                   .ForMember(dest => dest.IS_ACTIVE, cfg => cfg.MapFrom(src => src.IsActive))
                   .ForMember(dest => dest.IS_EMAIL_SENT, cfg => cfg.MapFrom(src => src.IsSentEmail))
                   .ForMember(dest => dest.GST_NO, cfg => cfg.MapFrom(src => src.GSTNo))
                   .ForMember(dest => dest.CREATED_BY, cfg => cfg.MapFrom(src => src.CreatedBy))
                   .ForMember(dest => dest.CREATED_BY_NAME, cfg => cfg.MapFrom(src => src.CreatedByName))
                   .ForMember(dest => dest.CREATED_DATE, cfg => cfg.MapFrom(src => src.CreatedDate))
                   .ForMember(dest => dest.MODIFIED_BY, cfg => cfg.MapFrom(src => src.ModifiedBy))
                   .ForMember(dest => dest.MODIFIED_DATE, cfg => cfg.MapFrom(src => src.ModifiedDate)).ReverseMap();

            CreateMap<Vendor, VendorDetail>().ReverseMap();

            CreateMap<VendorDetail, VendorAddressDetail>()
               .ForMember(dest => dest.PinCode, cft => cft.MapFrom(src => src.PostCode))
               .ForMember(dest => dest.AddressLine1, cft => cft.MapFrom(src => src.Address))
               .ForMember(dest => dest.Email_Ids, cft => cft.MapFrom(src => src.EmailId))
               .ForMember(dest => dest.Mobile_Numbers, cft => cft.MapFrom(src => src.ContactNo))
               .ForMember(dest => dest.Contact_Person, cft => cft.MapFrom(src => src.Contact_Person));

            CreateMap<VendorAddressDetail, VendorAddressModel>()
                .ForMember(dest => dest.ADDRESS_ID, cft => cft.MapFrom(src => src.AddressId))
                .ForMember(dest => dest.ADDRESS_LINE1, cft => cft.MapFrom(src => src.AddressLine1))
                .ForMember(dest => dest.ADDRESS_LINE2, cft => cft.MapFrom(src => src.AddressLine2))
                .ForMember(dest => dest.CITY, cft => cft.MapFrom(src => src.City))
                .ForMember(dest => dest.PINCODE, cft => cft.MapFrom(src => src.PinCode))
                .ForMember(dest => dest.EMAIL_IDS, cft => cft.MapFrom(src => src.Email_Ids))
                .ForMember(dest => dest.MOBILE_NUMBERS, cft => cft.MapFrom(src => src.Mobile_Numbers))
                .ForMember(dest => dest.CONTACT_PERSON, cft => cft.MapFrom(src => src.Contact_Person))
                .ForMember(dest => dest.REGION_CODE, cft => cft.MapFrom(src => src.Region))
                .ForMember(dest => dest.REGION_DESCRIPTION, cft => cft.MapFrom(src => src.RegionDescription))
                .ForMember(dest => dest.IS_DEFAULT, cft => cft.MapFrom(src => src.IsDefault))
                .ForMember(dest => dest.CREATED_BY, cft => cft.MapFrom(src => src.Created_By))
                .ForMember(dest => dest.CREATED_DATE, cft => cft.MapFrom(src => src.Created_Date))
                .ReverseMap();

            CreateMap<POHeader, PO_HEADER_MODEL>()
               .ForMember(dest => dest.POID, cfg => cfg.MapFrom(src => src.Id))
               .ForMember(dest => dest.INSP_ID, cfg => cfg.MapFrom(src => src.InspId))
               .ForMember(dest => dest.PO_NUMBER, cfg => cfg.MapFrom(src => src.PoNumber))
               .ForMember(dest => dest.LINK_PO_NUMBER, cfg => cfg.MapFrom(src => src.LinkPoNumber))
               .ForMember(dest => dest.VENDOR_CODE, cfg => cfg.MapFrom(src => src.VendorCode))
               .ForMember(dest => dest.VENDOR_NAME, cfg => cfg.MapFrom(src => src.VendorName))
               .ForMember(dest => dest.COLOR, cfg => cfg.MapFrom(src => src.Color))
               .ForMember(dest => dest.FABRIC, cfg => cfg.MapFrom(src => src.Fabric))
               .ForMember(dest => dest.DATE_OF_INSPECTION, cfg => cfg.MapFrom(src => src.DateOfInspection))
               .ForMember(dest => dest.OFFERED_QTY, cfg => cfg.MapFrom(src => src.OfferedQty))
               .ForMember(dest => dest.TOTAL_QTY, cfg => cfg.MapFrom(src => src.POTotalQty))
               .ForMember(dest => dest.ADDRESS_ID, cfg => cfg.MapFrom(src => src.AddressId))
               .ForMember(dest => dest.REGION_ID, cfg => cfg.MapFrom(src => src.RegionId))
               .ForMember(dest => dest.LOCATION, cft => cft.MapFrom(src => src.InspectionLocation))
               .ForMember(dest => dest.CREATED_BY, cft => cft.MapFrom(src => src.Created_By))
               .ForMember(dest => dest.CREATED_ON, cft => cft.MapFrom(src => src.CreatedOn))

               .ForMember(dest => dest.IS_DATE_ASSIGNED_BY_QA, cft => cft.MapFrom(src => src.Is_Date_Assigned_By_QA))
               .ForMember(dest => dest.IS_INSPECTOR_ASSIGNED, cft => cft.MapFrom(src => src.Is_Inspector_Assigned))
               .ForMember(dest => dest.IS_QC_AT_DC, cft => cft.MapFrom(src => src.Is_Qc_At_Dc))
               .ForMember(dest => dest.INSP_REQ_STATUS, cft => cft.MapFrom(src => src.Insp_Req_Status))
               .ForMember(dest => dest.STATUS_CODE, cft => cft.MapFrom(src => src.Status_Code))
               .ForMember(dest => dest.INSP_RESULT, cft => cft.MapFrom(src => src.Inspection_Result))
               .ForMember(dest => dest.INSPECTOR_EMP_CODE, cft => cft.MapFrom(src => src.InspectorId))
               .ForMember(dest => dest.QA_ASSIGNED_DOI, cfg => cfg.MapFrom(src => src.QA_ASSIGNED_DOI))

               .ForMember(dest => dest.INSPECTION_TYPE_ID, cft => cft.MapFrom(src => src.InspectionTypeId))
               .ForMember(dest => dest.APPARELS, cft => cft.MapFrom(src => src.Apparels))
               .ForMember(dest => dest.CATEGORY, cft => cft.MapFrom(src => src.Category))
               .ForMember(dest => dest.BUYER_ID, cft => cft.MapFrom(src => src.BuyereId))
               .ForMember(dest => dest.BUYER_NAME, cfg => cfg.MapFrom(src => src.BuyerName))
               .ForMember(dest => dest.INSPECTION_NO, cfg => cfg.MapFrom(src => src.InspectionNo))
               .ForMember(dest => dest.INSPECTOR, cfg => cfg.MapFrom(src => src.Inspector))
               .ForMember(dest => dest.MC, cfg => cfg.MapFrom(src => src.MC))
               .ForMember(dest => dest.MC_TEXT, cfg => cfg.MapFrom(src => src.MCText))
               .ForMember(dest => dest.MRP, cfg => cfg.MapFrom(src => src.MRP))
               .ForMember(dest => dest.BRAND_ID, cfg => cfg.MapFrom(src => src.Brand_Id))
               .ForMember(dest => dest.BRAND_ID2, cfg => cfg.MapFrom(src => src.Brand_Id2))
               .ForMember(dest => dest.FIT, cfg => cfg.MapFrom(src => src.FIT))
               .ForMember(dest => dest.ISLASTREQUESTPENDING, cfg => cfg.MapFrom(src => src.IsLastRequestPending))

               .ReverseMap()

               .ForMember(dest => dest.RegionList, cfg => cfg.Ignore())
               .ForMember(dest => dest.InspectionTypeList, cfg => cfg.Ignore())
               .ForMember(dest => dest.InspectorList, cfg => cfg.Ignore())
               .ForMember(dest => dest.AddressList, cfg => cfg.Ignore())
               .ForMember(dest => dest.Details, cfg => cfg.Ignore());


            CreateMap<PODetails, PO_DETAILS_MODEL>()
              .ForMember(dest => dest.PO_DETAILS_ID, cfg => cfg.MapFrom(src => src.Id))
              .ForMember(dest => dest.LINE_ITEM, cfg => cfg.MapFrom(src => src.LineItem))
              .ForMember(dest => dest.ARTICLE, cfg => cfg.MapFrom(src => src.Article))
              .ForMember(dest => dest.ARTICLE_DESC, cfg => cfg.MapFrom(src => src.ArticleDesc))
              .ForMember(dest => dest.PREPACK_QTY, cfg => cfg.MapFrom(src => src.PrepackQty))
              .ForMember(dest => dest.PURCHASE_TOT_QTY, cfg => cfg.MapFrom(src => src.PurchaseTotQty))
              .ForMember(dest => dest.PREPACK_QTY_EACHES, cfg => cfg.MapFrom(src => src.PrepackQtyEaches))
              .ForMember(dest => dest.EACHES, cfg => cfg.MapFrom(src => src.Eaches))
              .ForMember(dest => dest.MRP, cfg => cfg.MapFrom(src => src.MRP))
              .ForMember(dest => dest.ATTYP, cfg => cfg.MapFrom(src => src.AtTyp))
              .ReverseMap();

            CreateMap<InspectionModel, InspectDetails>()
              .ForMember(dest => dest.INSPECT_ID, cfg => cfg.MapFrom(src => src.InspectId))
              .ForMember(dest => dest.INSPECTION_ID, cfg => cfg.MapFrom(src => src.InspectionId))
              .ForMember(dest => dest.INSPECTOR_CODE, cfg => cfg.MapFrom(src => src.InspectorCode))
              .ForMember(dest => dest.OFFERED_QTY, cfg => cfg.MapFrom(src => src.OfferedQuantity))
              .ForMember(dest => dest.GPT_NO, cfg => cfg.MapFrom(src => src.GptNo))
              .ForMember(dest => dest.GPT_STATUS, cfg => cfg.MapFrom(src => src.GptStatus))
              .ForMember(dest => dest.GPT_APPROVED_BY, cfg => cfg.MapFrom(src => src.GptApprovedBy))
              .ForMember(dest => dest.GPT_APPROVED_DATE, cfg => cfg.MapFrom(src => src.GptApprovedDate))
              .ForMember(dest => dest.NO_OF_BAGS_CARTONS_QTY, cfg => cfg.MapFrom(src => src.NoOfBagsCartonsQty))
              .ForMember(dest => dest.NO_OF_BAGS_CARTONS_UNIT, cfg => cfg.MapFrom(src => src.NoOfBagsCartonsUnit))
              .ForMember(dest => dest.PACKING_STATUS, cfg => cfg.MapFrom(src => src.PackingStatus))
              .ForMember(dest => dest.INLINE_DONE, cfg => cfg.MapFrom(src => src.InlineDone))
              .ForMember(dest => dest.PACKING_LIST, cfg => cfg.MapFrom(src => src.PackingList))
              .ForMember(dest => dest.PPS_RELEASE_DATE, cfg => cfg.MapFrom(src => src.PpsReleaseDate))
              .ForMember(dest => dest.PPS_EXPIRY_DATE, cfg => cfg.MapFrom(src => src.PpsExpiryDate))
              .ForMember(dest => dest.FIBER_CONTENT_ON_PO, cfg => cfg.MapFrom(src => src.FiberContentOnPo))
              .ForMember(dest => dest.FIBER_CONTENT_ON_TEST_REPORT, cfg => cfg.MapFrom(src => src.FiberContentOnTestReport))
              .ForMember(dest => dest.FIBER_CONTENT_ON_WC, cfg => cfg.MapFrom(src => src.FiberContentOnWc))
              .ForMember(dest => dest.GSM_ON_PO, cfg => cfg.MapFrom(src => src.GsmOnPo))
              .ForMember(dest => dest.GSM_ON_TEST_REPORT, cfg => cfg.MapFrom(src => src.GsmOnTestReport))
              .ForMember(dest => dest.GSM_ON_BULK, cfg => cfg.MapFrom(src => src.GsmOnBulk))
              .ForMember(dest => dest.MEASUREMENT_SAMPLE_SIZE, cfg => cfg.MapFrom(src => src.MeasurementSampleSize))
              .ForMember(dest => dest.MEASUREMENT_ATTACHMENT, cfg => cfg.MapFrom(src => src.MeasurementAttachment))
              .ForMember(dest => dest.NO_OF_CHECK, cfg => cfg.MapFrom(src => src.NoOfCheck))
              .ForMember(dest => dest.NO_OF_CHECK_UNIT, cfg => cfg.MapFrom(src => src.NoOfCheckUnit))
              .ForMember(dest => dest.NO_OF_PREPACK_CHECK, cfg => cfg.MapFrom(src => src.NoOfPrepackCheck))
              .ForMember(dest => dest.WORKMANSHIP, cfg => cfg.MapFrom(src => src.Workmanship))
              .ForMember(dest => dest.AQL, cfg => cfg.MapFrom(src => src.Aql))
              .ForMember(dest => dest.AQL_SAMPLE_SIZE, cfg => cfg.MapFrom(src => src.AqlSampleSize))
              .ForMember(dest => dest.AQL_2_5_MAJOR, cfg => cfg.MapFrom(src => src.Aql_2_5_Major))
              .ForMember(dest => dest.AQL_2_5_MINOR, cfg => cfg.MapFrom(src => src.Aql_2_5_Minor))
              .ForMember(dest => dest.DEFECTS_ALLOWED, cfg => cfg.MapFrom(src => src.DefectsAllowed))
              .ForMember(dest => dest.DEFECTS_STATUS, cfg => cfg.MapFrom(src => src.DefectsStatus))
              .ForMember(dest => dest.INSPECTION_RESULT, cfg => cfg.MapFrom(src => src.InspectionResult))
              .ForMember(dest => dest.REMARK_COMMENT, cfg => cfg.MapFrom(src => src.RemarkComment))
              .ForMember(dest => dest.NOTE, cfg => cfg.MapFrom(src => src.Note))
              .ForMember(dest => dest.NAME_OF_FACTORY_REPRESENTATIVE, cfg => cfg.MapFrom(src => src.NameOfFactoryRepresentative))
              .ForMember(dest => dest.TEST_REPORT_ATTACHMENT, cfg => cfg.MapFrom(src => src.TestReportAttachment))
              .ForMember(dest => dest.GEN_ATTACHMENT_FIRST, cfg => cfg.MapFrom(src => src.GenAttachmentFirst))
              .ForMember(dest => dest.GEN_ATTACHMENT_SECOND, cfg => cfg.MapFrom(src => src.GenAttachmentSecond))
              .ForMember(dest => dest.IS_COMPLETE, cfg => cfg.MapFrom(src => src.IsComplete))
              .ForMember(dest => dest.DATE_OF_COMPLETION, cfg => cfg.MapFrom(src => src.Date_of_Completion))

              .ForMember(dest => dest.RESEAN_FOR_FAIL, cfg => cfg.MapFrom(src => src.Reason_for_fail))
              .ForMember(dest => dest.COLOR_CODE, cfg => cfg.MapFrom(src => src.ColorCode))
              .ForMember(dest => dest.SEASON, cfg => cfg.MapFrom(src => src.Season))
              .ForMember(dest => dest.AREA_OF_FAIL_GPTR, cfg => cfg.MapFrom(src => src.Area_Of_Fail_GPTR))
              .ForMember(dest => dest.GPT_COMM_APPROVAL_DATE, cfg => cfg.MapFrom(src => src.GPT_Comm_Approval_Date))
              .ForMember(dest => dest.GPT_COMM_APPROVED_BY, cfg => cfg.MapFrom(src => src.GTP_Comm_Approved_By))
              .ForMember(dest => dest.MFG_DATE, cfg => cfg.MapFrom(src => src.MFG_Date))

              .ForMember(dest => dest.GPT_ELASTIC_NO, cfg => cfg.MapFrom(src => src.gptElasticNo))
              .ForMember(dest => dest.GPT_ELASTIC_STATUS, cfg => cfg.MapFrom(src => src.gptElasticStatus))
              .ForMember(dest => dest.GPT_ELASTIC_APPROVED_BY, cfg => cfg.MapFrom(src => src.gptElasticApprovedBy))
              .ForMember(dest => dest.GPT_ELASTIC_APPROVED_DATE, cfg => cfg.MapFrom(src => src.gptElasticApprovedDate))
              .ForMember(dest => dest.CONFORMITY_SIZE, cfg => cfg.MapFrom(src => src.conformitySize))
              .ForMember(dest => dest.CONFORMITY_SIZE_STATUS, cfg => cfg.MapFrom(src => src.conformitySizeStatus))
              .ForMember(dest => dest.CONFORMITY_PREPACK, cfg => cfg.MapFrom(src => src.conformityPrepack))
              .ForMember(dest => dest.CONFORMITY_PREPACK_STATUS, cfg => cfg.MapFrom(src => src.conformityPrepackStatus))
              .ForMember(dest => dest.EMBROIDERY_PF, cfg => cfg.MapFrom(src => src.embroideryPf))
              .ForMember(dest => dest.ACCESSORIES_PF, cfg => cfg.MapFrom(src => src.accessoriesPf))
              .ForMember(dest => dest.BARCODE_PF, cfg => cfg.MapFrom(src => src.barcodePf))
              .ForMember(dest => dest.NO_PLASTIC_PF, cfg => cfg.MapFrom(src => src.noPlasticPf))
              .ForMember(dest => dest.BOGO_PF, cfg => cfg.MapFrom(src => src.bogoPf))
              .ForMember(dest => dest.THREAD_YN, cfg => cfg.MapFrom(src => src.threadYn))
              .ForMember(dest => dest.BUTTON_YN, cfg => cfg.MapFrom(src => src.buttonYn))
              .ForMember(dest => dest.ZIPPER_YN, cfg => cfg.MapFrom(src => src.zipperYn))
              .ForMember(dest => dest.INTERLINING_YN, cfg => cfg.MapFrom(src => src.interliningYn))
              .ForMember(dest => dest.ELASTIC_YN, cfg => cfg.MapFrom(src => src.elasticYn))
              .ForMember(dest => dest.MEASUREMENT_SAMPLE_QTY, cfg => cfg.MapFrom(src => src.measurementSampleQty))

              .ForMember(dest => dest.ZIPPER_RPT_NO, cfg => cfg.MapFrom(src => src.ZipperRptNo))
              .ForMember(dest => dest.ZIPPER_STATUS, cfg => cfg.MapFrom(src => src.ZipperStatus))
              .ForMember(dest => dest.ZIPPER_APPROVED_BY, cfg => cfg.MapFrom(src => src.ZipperApprovedBy))
              .ForMember(dest => dest.ZIPPER_APPROVED_DATE, cfg => cfg.MapFrom(src => src.ZipperApprovedDate))
              .ForMember(dest => dest.FIT, cfg => cfg.MapFrom(src => src.Fit))
              .ForMember(dest => dest.COMM_APPROVAL_DOC, cfg => cfg.MapFrom(src => src.CommApprovalDoc))
              .ForMember(dest => dest.DEBIT_AMOUNT, cfg => cfg.MapFrom(src => src.DebitAmount))
              .ForMember(dest => dest.ADMIN_REMARK, cfg => cfg.MapFrom(src => src.AdminRemark))
              .ForMember(dest => dest.ABORTIVE_REASON_IDS, cfg => cfg.MapFrom(src => src.AbortiveReasonIds))
              .ForMember(dest => dest.OTHER_ABORTIVE_REASON, cfg => cfg.MapFrom(src => src.OtherAbortiveReason))
              .ForMember(dest => dest.AMT_TRAVELLING, cfg => cfg.MapFrom(src => src.AmtTravelling))
              .ForMember(dest => dest.SELECTED_BAG_CARTONS_NO, cfg => cfg.MapFrom(src => src.SelectedBagCartonsNo))

              .ReverseMap();

            CreateMap<InspectionDefectDto, DefectsResponse>()
             .ForMember(dest => dest.DEFECTS_ID, cfg => cfg.MapFrom(src => src.Defects_Id))
             .ForMember(dest => dest.DEFECTS, cfg => cfg.MapFrom(src => src.Defects))
             .ForMember(dest => dest.DIMENSION, cfg => cfg.MapFrom(src => src.Dimension))
             .ForMember(dest => dest.MAJOR, cfg => cfg.MapFrom(src => src.Major))
             .ForMember(dest => dest.MINOR, cfg => cfg.MapFrom(src => src.Minor))
             .ReverseMap();

            CreateMap<InspectionAssignRowDto, INSPECTIONLISTMODEL>().ReverseMap();

            CreateMap<HierarchyDto, MASTER_HIERARCHY_MODEL>()
              .ForMember(dest => dest.EMP_CODE, cfg => cfg.MapFrom(src => src.EmpCode))
              .ForMember(dest => dest.EMP_NAME, cfg => cfg.MapFrom(src => src.EmpName))
              .ForMember(dest => dest.EMAIL_ID, cfg => cfg.MapFrom(src => src.EmailId))
              .ForMember(dest => dest.MOBILE_NO, cfg => cfg.MapFrom(src => src.MobileNo))
              .ForMember(dest => dest.ROLE_ID, cfg => cfg.MapFrom(src => src.RoleId))
              .ForMember(dest => dest.ROLE_NAME, cfg => cfg.MapFrom(src => src.RoleName))
              .ForMember(dest => dest.REPORTING_MANAGER, cfg => cfg.MapFrom(src => src.ReportingManager))
              .ForMember(dest => dest.REPORTING_EMP_ID, cfg => cfg.MapFrom(src => src.ReportingEmpId))
              .ForMember(dest => dest.IS_ACTIVE, cfg => cfg.MapFrom(src => src.IsActive))
              .ForMember(dest => dest.CREATED_ON, cfg => cfg.MapFrom(src => src.CreatedOn))
              .ForMember(dest => dest.HIERARCHY_LEVEL, cfg => cfg.MapFrom(src => src.HierarchyLevel))
              .ReverseMap();

            CreateMap<EmailManagementDto, EmailManagement>()
              .ForMember(dest => dest.EMAIL_ID, cfg => cfg.MapFrom(src => src.EmailId))
              .ForMember(dest => dest.NAME, cfg => cfg.MapFrom(src => src.Name))
              .ForMember(dest => dest.LAST_UPDATED_DATE, cfg => cfg.MapFrom(src => src.Last_Updated_Date))
              .ForMember(dest => dest.CREATED_BY_EMP_CODE, cfg => cfg.MapFrom(src => src.Created_By_Emp_Code))
              .ForMember(dest => dest.IS_ACTIVE, cfg => cfg.MapFrom(src => src.IsActive))
              .ReverseMap();

            CreateMap<MasterColorCodeDto, MasterColorCode>()
             .ForMember(dest => dest.COLOR_CODE_ID, cfg => cfg.MapFrom(src => src.ColorCodeId))
             .ForMember(dest => dest.COLOR_CODE, cfg => cfg.MapFrom(src => src.ColorCode))
             .ForMember(dest => dest.DESCRIPTION, cfg => cfg.MapFrom(src => src.Description))
             .ForMember(dest => dest.ACTIVE, cfg => cfg.MapFrom(src => src.Active))
             .ForMember(dest => dest.ORDER_NO, cfg => cfg.MapFrom(src => src.OrderNo))
             .ReverseMap();

            CreateMap<VendorAuditFormModel, FactoryAuditRequestDto>()
             .ForMember(dest => dest.AUDIT_REQ_ID, cfg => cfg.MapFrom(src => src.AuditReqId))
             .ForMember(dest => dest.AUDIT_ID, cfg => cfg.MapFrom(src => src.AuditId))
             .ForMember(dest => dest.TEMP_VENDOR_CODE, cfg => cfg.MapFrom(src => src.TempVendorCode))
             .ForMember(dest => dest.BUSINESS_HEAD, cfg => cfg.MapFrom(src => src.BusinessHead))
             .ForMember(dest => dest.AUDIT_DATE, cfg => cfg.MapFrom(src => src.AuditDate))
             .ForMember(dest => dest.GST_NUMBER, cfg => cfg.MapFrom(src => src.GstNumber))
             .ForMember(dest => dest.VENDOR_NAME, cfg => cfg.MapFrom(src => src.VendorName))
             .ForMember(dest => dest.REGISTERED_ADDRESS, cfg => cfg.MapFrom(src => src.RegisteredAddress))
             .ForMember(dest => dest.FACTORY_LOCATION, cfg => cfg.MapFrom(src => src.FactoryLocation))
             .ForMember(dest => dest.CONTACT_NAME, cfg => cfg.MapFrom(src => src.ContactName))
             .ForMember(dest => dest.CONTACT_NUMBER, cfg => cfg.MapFrom(src => src.ContactNumber))
             .ForMember(dest => dest.CATEGORY, cfg => cfg.MapFrom(src => src.Category))
             .ForMember(dest => dest.REQUESTED_BUYER_CODE, cfg => cfg.MapFrom(src => src.RequestedBuyerName))
             .ForMember(dest => dest.PRODUCT_CATEGORY, cfg => cfg.MapFrom(src => src.ProductCategory))
             .ForMember(dest => dest.FACTORY_LICENCE_NUMBER, cfg => cfg.MapFrom(src => src.FactoryLicenceNumber))
             .ForMember(dest => dest.FACTORY_AREA_SQFT, cfg => cfg.MapFrom(src => src.FactoryAreaSqFt))
             .ForMember(dest => dest.NUMBER_OF_EMPLOYEES, cfg => cfg.MapFrom(src => src.NumberOfEmployees))
             .ForMember(dest => dest.GEN_ATTACHMENT_FIRST, cfg => cfg.MapFrom(src => src.GenAttachmentFirst))
             .ForMember(dest => dest.GST_API_RAW_RESPONSE, cfg => cfg.MapFrom(src => src.GSTApiResponse))
             .ForMember(dest => dest.HEAD, cfg => cfg.MapFrom(src => src.Head))
             .ForMember(dest => dest.EMP_NAME, cfg => cfg.MapFrom(src => src.CreatedBy))
             .ForMember(dest => dest.CREATED_ON, cfg => cfg.MapFrom(src => src.CreatedOn))
             .ForMember(dest => dest.REQ_STATUS, cfg => cfg.MapFrom(src => src.ReqStatus))
             .ReverseMap();

            CreateMap<AuditChecklistItem, FactoryAuditChecklistDto>()
            .ForMember(dest => dest.CHECKLIST_ID, cfg => cfg.MapFrom(src => src.Id))
            .ForMember(dest => dest.AUDIT_REQ_ID, cfg => cfg.MapFrom(src => src.Audit_Req_Id))
            .ForMember(dest => dest.CHECKLIST_MST_ID, cfg => cfg.MapFrom(src => src.CheckListMasterId))
            .ForMember(dest => dest.DESCRIPTION, cfg => cfg.MapFrom(src => src.Description))
            .ForMember(dest => dest.STATUS, cfg => cfg.MapFrom(src => src.Status))
            .ForMember(dest => dest.VALIDITY_DATE, cfg => cfg.MapFrom(src => src.ValidityDate))
            .ForMember(dest => dest.REMARKS, cfg => cfg.MapFrom(src => src.Remarks))
            .ForMember(dest => dest.ATTACHMENTS, cfg => cfg.MapFrom(src => src.Attachments))
            .ReverseMap();


        }
    }
}
