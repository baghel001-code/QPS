using Application.Interfaces.V1;
using ClosedXML.Excel;
using Dapper;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Infrastructure.Utilitys;
using System.Data;


namespace Infrastructure.Services.V1
{
    public class VendorService : IVendor
    {
        private readonly DapperHelper _dapperHelper;

        public VendorService(DapperHelper dapperHelper)
        {
            _dapperHelper = dapperHelper;
        }


        public async Task<GenRes> SetVendor(Vendor req)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@Id", req.VendorId, DbType.Int32);
            parameters.Add("@Vendor_Code", req.VendorCode, DbType.String);
            parameters.Add("@Vendor_Name", req.VendorName, DbType.String);
            parameters.Add("@Password", req.PassWord, DbType.String);
            parameters.Add("@DEFAULT_PASSWORD_CHANGED", req.Default_Password_Changed, DbType.String);
            parameters.Add("@GSTNo", req.GSTNo, DbType.String);
            parameters.Add("@IsActive", req.IsActive, DbType.Boolean);
            parameters.Add("@IsEmailSent", req.IsSentEmail, DbType.Boolean);
            parameters.Add("@Inserted_Updated_By", req.CreatedBy, DbType.Int32);


            // Table-valued parameter
            var addressDetailsTable = new DataTable();
            addressDetailsTable.Columns.Add("ADDRESS_ID", typeof(int));
            addressDetailsTable.Columns.Add("Address", typeof(string));
            addressDetailsTable.Columns.Add("City", typeof(string));
            addressDetailsTable.Columns.Add("PinCode", typeof(string));
            addressDetailsTable.Columns.Add("Region", typeof(string));
            addressDetailsTable.Columns.Add("Region_Description", typeof(string));
            addressDetailsTable.Columns.Add("Moble_Numbers", typeof(string));
            addressDetailsTable.Columns.Add("Email_Ids", typeof(string));
            addressDetailsTable.Columns.Add("Contact_Person", typeof(string));
            addressDetailsTable.Columns.Add("Is_Default", typeof(bool));
            foreach (var address in req.VendorAddresses)
            {
                addressDetailsTable.Rows.Add(address.AddressId, address.AddressLine1, address.City, address.PinCode, address.Region, address.RegionDescription, address.Mobile_Numbers, address.Email_Ids, address.Contact_Person, address.IsDefault);
            }
            parameters.Add("@ADDRESSES", addressDetailsTable.AsTableValuedParameter("AddressDetails"));

            //Output parameters
            parameters.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("PRO_INSERT_UPDATE_VENDOR_MASTER", parameters);
            if (!parameters.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = parameters.Get<int>("Error_Code");
                Res.responseMessage = parameters.Get<string>("Error_Message");
            }
            return Res;
        }

        public async Task<GetAllVendors> GetAllVendors()
        {
            var Res = new GetAllVendors();

            var parameters = new
            {
                Id = 0,
                StrField = "",
                Cmd = "Get_All_VENDORS"
            };
            var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<VendorModel>("PRO_PO_VENDOR_MASTER", parameters);
            Res.VendorsList = (List<VendorModel>)Out;
            return Res;
        }

        public async Task<GetAddressByVendorCode> GetAddressOnVendorCode(SelectListReq req)
        {
            var Res = new GetAddressByVendorCode();
            var parameters = new DynamicParameters();
            parameters.Add("@Id", req.Id, DbType.Int32);
            parameters.Add("@StrField", req.StrField, DbType.String);
            parameters.Add("@Cmd", req.Cmd, DbType.String);//Get_VENDORS_ADDRESS
            var OutSet = await _dapperHelper.ExecuteStoredProcedureListAsync<VendorAddressModel>("PRO_PO_VENDOR_MASTER", parameters);
            Res.AddressList = OutSet.Count() > 0 ? (List<VendorAddressModel>)OutSet : new List<VendorAddressModel>();

            return Res;
        }

        public async Task<GetVendorByCode> GetVendorByID(SelectListReq req)
        {
            var Res = new GetVendorByCode();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var (vendors, addresses) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<VendorModel, VendorAddressModel>("PRO_PO_VENDOR_MASTER", parameters);
                Res.VendorModel = vendors?.FirstOrDefault() ?? new VendorModel();
                Res.VendorAddressList = addresses?.ToList() ?? new List<VendorAddressModel>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;

        }

       

        public async Task<POResponse> GetPODetails(SelectListReq req)
        {
            var Res = new POResponse();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var (PO, Details) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<PO_HEADER_MODEL, PO_DETAILS_MODEL>("PRO_PO_VENDOR_MASTER", parameters);
                Res.POHeader = PO?.FirstOrDefault() ?? new PO_HEADER_MODEL();
                Res.PODetails = Details?.ToList() ?? new List<PO_DETAILS_MODEL>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;
        }

        public async Task<GenRes> SaveInspectionRequest(POHeader req)
        {
            var Res = new GenRes();


            var headerParams = new DynamicParameters();

            headerParams.Add("@POID", req.Id, DbType.Int32, ParameterDirection.InputOutput);
            headerParams.Add("@PO_NUMBER", req.PoNumber);
            headerParams.Add("@VENDOR_CODE", req.VendorCode);
            headerParams.Add("@VENDOR_NAME", req.VendorName);
            headerParams.Add("@FABRIC", req.Fabric);
            headerParams.Add("@COLOR", req.Color);
            headerParams.Add("@TOTAL_QTY", req.POTotalQty);
            headerParams.Add("@USER_ID", req.CreatedBy);
            headerParams.Add("@IS_CANCELLED", req.IsCancelled);
            headerParams.Add("@IS_QC_AT_DC", req.Is_Qc_At_Dc);

            // inspection
            headerParams.Add("@INSP_ID", req.InspId);
            headerParams.Add("@INSPECTION_DATE", req.DateOfInspection);
            headerParams.Add("@OFFERED_QTY", req.OfferedQty);
            headerParams.Add("@ADDRESS_ID", req.AddressId);
            headerParams.Add("@REGION_ID", req.RegionId);

            headerParams.Add("@INSPECTION_TYPE_ID", req.InspectionTypeId);
            headerParams.Add("@APPARELS", req.Apparels);
            headerParams.Add("@CATEGORY", req.Category);
            headerParams.Add("@BUYER_NAME", req.BuyerName);
            headerParams.Add("@BUYER_ID", req.BuyereId);
            headerParams.Add("@MC", req.MC);
            headerParams.Add("@MC_TEXT", req.MCText);
            headerParams.Add("@MRP", req.MRP);
            headerParams.Add("@BRAND_ID", req.Brand_Id);
            headerParams.Add("@BRAND_ID2", req.Brand_Id2);
            headerParams.Add("@FIT", req.FIT);

            // table valued parameter
            var poDetailsTable = new DataTable();
            poDetailsTable.Columns.Add("PO_DETAILS_ID", typeof(int));
            poDetailsTable.Columns.Add("LINE_ITEM", typeof(string));
            poDetailsTable.Columns.Add("ARTICLE", typeof(string));
            poDetailsTable.Columns.Add("ARTICLE_DESC", typeof(string));
            poDetailsTable.Columns.Add("PREPACK_QTY", typeof(decimal));
            poDetailsTable.Columns.Add("PURCHASE_TOT_QTY", typeof(decimal));
            poDetailsTable.Columns.Add("PREPACK_QTY_EACHES", typeof(decimal));
            poDetailsTable.Columns.Add("EACHES", typeof(decimal));
            poDetailsTable.Columns.Add("MRP", typeof(decimal));
            poDetailsTable.Columns.Add("ATTYP", typeof(string));
            poDetailsTable.Columns.Add("CREATED_BY", typeof(int));
            poDetailsTable.Columns.Add("UPDATED_BY", typeof(int));
            foreach (var dtl in req.Details)
            {
                poDetailsTable.Rows.Add(dtl.Id, dtl.LineItem, dtl.Article, dtl.ArticleDesc, dtl.PrepackQty, dtl.PurchaseTotQty, dtl.PrepackQtyEaches, dtl.Eaches, dtl.MRP, dtl.AtTyp, req.CreatedBy, req.CreatedBy);
            }

            headerParams.Add("@DETAILS", poDetailsTable.AsTableValuedParameter("TVP_PODETAIL"), dbType: DbType.Object, direction: ParameterDirection.Input);

            //Output parameters
            headerParams.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            headerParams.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            headerParams.Add("@RECORD_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_SAVE_PO", headerParams);
            int newPOID = headerParams.Get<int>("@POID");
            int INSPID = headerParams.Get<int>("@RECORD_ID");
            if (!headerParams.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = headerParams.Get<int>("Error_Code");
                Res.responseMessage = headerParams.Get<string>("Error_Message");
            }
            Res.recordId = INSPID;
            return Res;
        }

        public async Task<InspectionDetailResponse> GetInspectionRequestByID(SelectListReq req)
        {
            var Res = new InspectionDetailResponse();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var (PO, Details, AddressList) = await _dapperHelper.ExecuteStoredProcedureMultiple3ListAsync<PO_HEADER_MODEL, PO_DETAILS_MODEL, VendorAddressModel>("PRO_INSPECTION_DETAIL", parameters);
                Res.POHeader = PO?.FirstOrDefault() ?? new PO_HEADER_MODEL();
                Res.PODetails = Details?.ToList() ?? new List<PO_DETAILS_MODEL>();
                Res.AddressList = AddressList?.ToList() ?? new List<VendorAddressModel>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;

        }

        public async Task<MasterDataList> GetMasterData(SelectListReq req)
        {
            var Res = new MasterDataList();
            try
            {
                var parameters = new
                {
                    Id = req.Id,
                    StrField = req.StrField!,
                    Cmd = req.Cmd!
                };
                var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<MasterData>("PRO_MASTER_LOOKUPS", parameters);
                Res.masterDataList = (List<MasterData>)Out;

            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;
        }

        public async Task<GenRes> AddAddress(Vendor req)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@VENDOR_CODE", req.VendorCode, DbType.String);
            parameters.Add("@Inserted_Updated_By", req.CreatedBy, DbType.Int32);


            // Table-valued parameter
            var addressDetailsTable = new DataTable();
            addressDetailsTable.Columns.Add("ADDRESS_ID", typeof(int));
            addressDetailsTable.Columns.Add("Address", typeof(string));
            addressDetailsTable.Columns.Add("City", typeof(string));
            addressDetailsTable.Columns.Add("PinCode", typeof(string));
            addressDetailsTable.Columns.Add("Region", typeof(string));
            addressDetailsTable.Columns.Add("Region_Description", typeof(string));
            addressDetailsTable.Columns.Add("Moble_Numbers", typeof(string));
            addressDetailsTable.Columns.Add("Email_Ids", typeof(string));
            addressDetailsTable.Columns.Add("Contact_Person", typeof(string));
            addressDetailsTable.Columns.Add("Is_Default", typeof(bool));
            foreach (var address in req.VendorAddresses)
            {
                addressDetailsTable.Rows.Add(address.AddressId, address.AddressLine1, address.City, address.PinCode, address.Region, address.RegionDescription, address.Mobile_Numbers, address.Email_Ids, address.Contact_Person, address.IsDefault);
            }
            parameters.Add("@ADDRESSES", addressDetailsTable.AsTableValuedParameter("AddressDetails"));

            //Output parameters
            parameters.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            parameters.Add("@RECORD_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            //
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("PRO_INSERT_NEW_VENDOR_ADDRESS", parameters);
            if (!parameters.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = parameters.Get<int>("Error_Code");
                Res.responseMessage = parameters.Get<string>("Error_Message");
            }
            if (!parameters.Get<int>("RECORD_ID").Equals(0))
            {
                Res.recordId = parameters.Get<int>("RECORD_ID");
            }
            return Res;
        }

        public async Task<GenRes> UpdateDOI(UpdateDOIRequest req)
        {
            var Res = new GenRes();

            var headerParams = new DynamicParameters();
            headerParams.Add("@USER_ID", req.CreatedBy);
            headerParams.Add("@INSP_ID", req.InspId);
            headerParams.Add("@QA_ASSIGNED_DOI", req.QA_ASSIGNED_DOI);
            headerParams.Add("@OFFERED_QTY", req.OfferedQty);

            //Output parameters
            headerParams.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            headerParams.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_DOI_ASSIGNED_BY_QA", headerParams);
            if (!headerParams.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = headerParams.Get<int>("Error_Code");
                Res.responseMessage = headerParams.Get<string>("Error_Message");
            }
            return Res;
        }

        public async Task<GenRes> AssignInspector(AssignInspectorRequest req)
        {
            var Res = new GenRes();

            var headerParams = new DynamicParameters();
            headerParams.Add("@USER_ID", req.CreatedBy);
            headerParams.Add("@INSP_ID", req.InspId);
            headerParams.Add("@OFFERED_QTY", req.OfferedQty);
            headerParams.Add("@INSPECTOR_EMP_CODE", req.INSPECTOR_EMP_CODE);

            //Output parameters
            headerParams.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            headerParams.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_ASSIGNED_INSPECTOR", headerParams);
            if (!headerParams.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = headerParams.Get<int>("Error_Code");
                Res.responseMessage = headerParams.Get<string>("Error_Message");
            }
            return Res;
        }

        public async Task<GenRes> SaveInspection(InspectionModel request)
        {
            if (request.IsComplete)
            {
                request.Date_of_Completion = DateTime.Now;
            }
            else
            {
                request.Date_of_Completion = null;
            }
            var Res = new GenRes();

            var defectTable = new DataTable();
            defectTable.Columns.Add("DEFECTS_ID", typeof(int));
            defectTable.Columns.Add("INSPECTION_ID", typeof(int));
            defectTable.Columns.Add("DEFECTS", typeof(string));
            defectTable.Columns.Add("DIMENSION", typeof(string));
            defectTable.Columns.Add("MAJOR", typeof(int));
            defectTable.Columns.Add("MINOR", typeof(int));

            foreach (var d in request.Defects)
            {
                defectTable.Rows.Add(d.Defects_Id, request.InspectionId, d.Defects, d.Dimension, d.Major, d.Minor);
            }

            var parameters = new DynamicParameters();

            parameters.Add("@INSPECT_ID", request.InspectId);
            parameters.Add("@INSPECTION_ID", request.InspectionId);
            parameters.Add("@INSPECTOR_CODE", request.InspectorCode);
            parameters.Add("@SHIP_QTY", request.OfferedQuantity != null ? request.OfferedQuantity : 0);
            parameters.Add("@OFFERED_QTY", request.POOfferQuantity != null ? request.POOfferQuantity : 0);
            parameters.Add("@GPT_NO", request.GptNo);
            parameters.Add("@GPT_STATUS", request.GptStatus);
            parameters.Add("@GPT_APPROVED_BY", request.GptApprovedBy);
            parameters.Add("@GPT_APPROVED_DATE", request.GptApprovedDate);

            parameters.Add("@NO_OF_BAGS_CARTONS_QTY", request.NoOfBagsCartonsQty != null ? request.NoOfBagsCartonsQty : 0);
            parameters.Add("@NO_OF_BAGS_CARTONS_UNIT", request.NoOfBagsCartonsUnit);
            parameters.Add("@PACKING_STATUS", request.PackingStatus);
            parameters.Add("@INLINE_DONE", request.InlineDone);
            parameters.Add("@PACKING_LIST", request.PackingList);

            parameters.Add("@PPS_RELEASE_DATE", request.PpsReleaseDate);
            parameters.Add("@PPS_EXPIRY_DATE", request.PpsExpiryDate);

            parameters.Add("@FIBER_CONTENT_ON_PO", request.FiberContentOnPo);
            parameters.Add("@FIBER_CONTENT_ON_TEST_REPORT", request.FiberContentOnTestReport);
            parameters.Add("@FIBER_CONTENT_ON_WC", request.FiberContentOnWc);

            parameters.Add("@GSM_ON_PO", request.GsmOnPo);
            parameters.Add("@GSM_ON_TEST_REPORT", request.GsmOnTestReport);
            parameters.Add("@GSM_ON_BULK", request.GsmOnBulk);

            parameters.Add("@MEASUREMENT_SAMPLE_SIZE", request.MeasurementSampleSize);
            parameters.Add("@MEASUREMENT_ATTACHMENT", request.MeasurementAttachment);

            parameters.Add("@NO_OF_CHECK", request.NoOfCheck != null ? request.NoOfCheck : 0);
            parameters.Add("@NO_OF_CHECK_UNIT", request.NoOfCheckUnit);
            parameters.Add("@NO_OF_PREPACK_CHECK", request.NoOfPrepackCheck != null ? request.NoOfPrepackCheck : 0);

            parameters.Add("@WORKMANSHIP", request.Workmanship);
            parameters.Add("@INSPECTION_RESULT", request.InspectionResult);

            parameters.Add("@REMARK_COMMENT", request.RemarkComment);
            parameters.Add("@NOTE", request.Note);
            parameters.Add("@NAME_OF_FACTORY_REPRESENTATIVE", request.NameOfFactoryRepresentative);

            parameters.Add("@TEST_REPORT_ATTACHMENT", request.TestReportAttachment);
            parameters.Add("@GEN_ATTACHMENT_FIRST", request.GenAttachmentFirst);
            parameters.Add("@GEN_ATTACHMENT_SECOND", request.GenAttachmentSecond);

            parameters.Add("@IS_COMPLETE", request.IsComplete);
            parameters.Add("@USER_ID", request.UserId);

            parameters.Add("@RESEAN_FOR_FAIL", request.Reason_for_fail);
            parameters.Add("@COLOR_CODE", request.ColorCode);
            parameters.Add("@MFG_DATE", request.MFG_Date);
            parameters.Add("@GPT_COMM_APPROVAL_DATE", request.GPT_Comm_Approval_Date);
            parameters.Add("@DATE_OF_COMPLETION", request.Date_of_Completion);
            parameters.Add("@SEASON", request.Season);
            parameters.Add("@AREA_OF_FAIL_GPTR", request.Area_Of_Fail_GPTR);
            parameters.Add("@GPT_COMM_APPROVED_BY", request.GTP_Comm_Approved_By);
            parameters.Add("@IS_EDIT_BY_ADMIN", request.Is_Edit_By_Admin);

            parameters.Add("@GPT_ELASTIC_NO", request.gptElasticNo);
            parameters.Add("@GPT_ELASTIC_STATUS", request.gptElasticStatus);
            parameters.Add("@GPT_ELASTIC_APPROVED_BY", request.gptElasticApprovedBy);
            parameters.Add("@GPT_ELASTIC_APPROVED_DATE", request.gptElasticApprovedDate);
            parameters.Add("@CONFORMITY_SIZE", request.conformitySize);
            parameters.Add("@CONFORMITY_SIZE_STATUS", request.conformitySizeStatus);
            parameters.Add("@CONFORMITY_PREPACK", request.conformityPrepack);
            parameters.Add("@CONFORMITY_PREPACK_STATUS", request.conformityPrepackStatus);
            parameters.Add("@EMBROIDERY_PF", request.embroideryPf);
            parameters.Add("@ACCESSORIES_PF", request.accessoriesPf);
            parameters.Add("@BARCODE_PF", request.barcodePf);
            parameters.Add("@NO_PLASTIC_PF", request.noPlasticPf);
            parameters.Add("@BOGO_PF", request.bogoPf);
            parameters.Add("@THREAD_YN", request.threadYn);
            parameters.Add("@BUTTON_YN", request.buttonYn);
            parameters.Add("@ZIPPER_YN", request.zipperYn);
            parameters.Add("@INTERLINING_YN", request.interliningYn);
            parameters.Add("@ELASTIC_YN", request.elasticYn);
            parameters.Add("@MEASUREMENT_SAMPLE_QTY", request.measurementSampleQty != null ? request.measurementSampleQty : 0);

            parameters.Add("@ZIPPER_RPT_NO", request.ZipperRptNo);
            parameters.Add("@ZIPPER_STATUS", request.ZipperStatus);
            parameters.Add("@ZIPPER_APPROVED_BY", request.ZipperApprovedBy);
            parameters.Add("@ZIPPER_APPROVED_DATE", request.ZipperApprovedDate);
            parameters.Add("@FIT", request.Fit);
            parameters.Add("@COMM_APPROVAL_DOC", request.CommApprovalDoc);
            parameters.Add("@DEBIT_AMOUNT", request.DebitAmount);
            parameters.Add("@ADMIN_REMARK", request.AdminRemark);

            parameters.Add("@ABORTIVE_REASON_IDS", request.AbortiveReasonIds);
            parameters.Add("@OTHER_ABORTIVE_REASON", request.OtherAbortiveReason);
            parameters.Add("@AMT_TRAVELLING", request.AmtTravelling);

            parameters.Add("@SELECTED_BAG_CARTONS_NO", request.SelectedBagCartonsNo);

            parameters.Add("@IS_RESET_BY_ADMIN", request.IsResetByAdmin);

            parameters.Add("@DEFECTS", defectTable.AsTableValuedParameter("dbo.TVP_INSPECTION_DEFECTS"), dbType: DbType.Object, direction: ParameterDirection.Input);

            parameters.Add("@OUT_INSPECT_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("dbo.SP_SAVE_INSPECTION", parameters);


            Res.recordId = parameters.Get<int>("@OUT_INSPECT_ID");
            Res.responseCode = parameters.Get<int>("@ERROR_CODE");
            Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");


            return Res;
        }

        public async Task<InspectResponse> GetInspectDataByID(SelectListReq req)
        {
            var Res = new InspectResponse();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var (Insp, Def, AQL) = await _dapperHelper.ExecuteStoredProcedureMultiple3ListAsync<InspectDetails, DefectsResponse, AQLDataResponse>("PRO_INSPECTION_DETAIL", parameters);
                Res.InspectDetails = Insp?.FirstOrDefault() ?? new InspectDetails();
                Res.Defects = Def?.ToList() ?? new List<DefectsResponse>();
                Res.AQLList = AQL?.ToList() ?? new List<AQLDataResponse>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;

        }

        public async Task<GenRes> UpdateOfferedQty(UpdateDOIRequest req)
        {
            var Res = new GenRes();

            var headerParams = new DynamicParameters();
            headerParams.Add("@USER_ID", req.CreatedBy);
            headerParams.Add("@INSP_ID", req.InspId);
            headerParams.Add("@OFFERED_QTY", req.OfferedQty);

            //Output parameters
            headerParams.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            headerParams.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_CHANGE_OFFERED_QTY_BY_INSPECTOR", headerParams);
            if (!headerParams.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = headerParams.Get<int>("Error_Code");
                Res.responseMessage = headerParams.Get<string>("Error_Message");
            }
            return Res;
        }

        public async Task<DashboardModelResponse> GetDashboardData(SelectListReq obj)
        {
            var Res = new DashboardModelResponse();

            var parameters = new
            {
                Id = 0,
                StrField = obj.StrField,
                Cmd = obj.Cmd// "QA_DASHBOARD"
            };
            var (InspList, QAFList) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<DashboardModel, QAFREEZEModel>("PRO_QA_DASHBOARD", parameters);
            Res.ListofInspections = InspList?.ToList() ?? new List<DashboardModel>();
            Res.ListofFreezeQA = QAFList?.ToList() ?? new List<QAFREEZEModel>();
            return Res;
        }

        public async Task<AssignPendingInspectionListResponse> GetInspectionsListByIds(string Ids)
        {
            var Res = new AssignPendingInspectionListResponse();

            var parameters = new DynamicParameters();

            var defectTable = new DataTable();
            defectTable.Columns.Add("RECORD_ID", typeof(Int64));

            foreach (var d in Ids.Split(',').ToList())
            {
                defectTable.Rows.Add(Int64.Parse(d));
            }

            parameters.Add("@INSP_IDS", defectTable.AsTableValuedParameter("dbo.IdList"), dbType: DbType.Object, direction: ParameterDirection.Input);

            var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<INSPECTIONLISTMODEL>("SP_GET_INSPECTIONS_BY_IDS", parameters);
            Res.ListofInspections = (List<INSPECTIONLISTMODEL>)Out;
            return Res;
        }

        public async Task<AssignPendingInspectionListResponse> GetQALISTTOMAIL(SelectListReq req)
        {
            var Res = new AssignPendingInspectionListResponse();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var (InspList, EmailList) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<INSPECTIONLISTMODEL, QAMADIN_BUYER_QA_EMAILS_MODEL>("PRO_QA_DASHBOARD", parameters);
                Res.ListofInspections = InspList?.ToList() ?? new List<INSPECTIONLISTMODEL>();
                Res.EmailIds = EmailList?.ToList() ?? new List<QAMADIN_BUYER_QA_EMAILS_MODEL>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;

        }

        public async Task<GenRes> FreezQA(AssignInspectorFreezeRequest obj)
        {
            var Res = new GenRes();

            var headerParams = new DynamicParameters();
            headerParams.Add("@USER_ID", obj.CreatedBy);
            headerParams.Add("@QA_ASSIGNED_DOI", obj.QA_ASSIGNED_DOI);
            headerParams.Add("@IS_FREEZE", obj.IsFreeze);
            headerParams.Add("@DETAILS", obj.Details);

            //Output parameters
            headerParams.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
            headerParams.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_ASSIGNED_INSPECTOR_FREEZE", headerParams);
            if (!headerParams.Get<int>("Error_Code").Equals(0))
            {
                Res.responseCode = headerParams.Get<int>("Error_Code");
                Res.responseMessage = headerParams.Get<string>("Error_Message");
            }
            return Res;
        }

        public async Task<GenRes> UploadVendorExcel(VendorExcel req)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@EXCEL_FILE", req.EXCEL_FILE, DbType.String);
            parameters.Add("@NO_OF_RECORDS", req.NO_OF_RECORDS, DbType.Int32);
            parameters.Add("@NO_INCORRECT_RECORDS", req.NO_INCORRECT_RECORDS, DbType.Int32);
            parameters.Add("@INCORRECT_DETAILS", req.INCORRECT_DETAILS, DbType.String);
            parameters.Add("@USER_ID", req.USER_ID, DbType.Int32);

            // Table-valued parameter Errors
            var ErrorTable = new DataTable();
            ErrorTable.Columns.Add("ROWNUMBER", typeof(int));
            ErrorTable.Columns.Add("VENDORCODE", typeof(string));
            ErrorTable.Columns.Add("ERRORMESSAGE", typeof(string));
            foreach (var de in req.DataErrors)
            {
                ErrorTable.Rows.Add(de.RowNumber, de.VendorCode, de.Message);
            }
            parameters.Add("@ERRORS", ErrorTable.AsTableValuedParameter("TVP_Excel_Errors"));

            // Table-valued parameter
            var VENDOR_DETAILSTable = new DataTable();
            VENDOR_DETAILSTable.Columns.Add("VENDOR_CODE", typeof(int));
            VENDOR_DETAILSTable.Columns.Add("EMAIL_IDS", typeof(string));
            foreach (var vd in req.Data.Where(a => a.IsInvalid == false).ToList())
            {
                VENDOR_DETAILSTable.Rows.Add(vd.VENDOR_CODE, vd.EMAIL_IDS);
            }
            parameters.Add("@VENDOR_DETAILS", VENDOR_DETAILSTable.AsTableValuedParameter("TVP_Excel_VendorsDtails"));

            //Output parameters
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_INSERT_VENDOR_EXCEL_DATA", parameters);
            if (parameters.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = parameters.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");
                }
            }
            if (ResSet == 0)
            {
                //if()
            }
            return Res;
        }

        public async Task<GetVendorExcelData> GetVendorExcelData(SelectListReq req)
        {
            var Res = new GetVendorExcelData();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var (vendors, addresses) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<VendorExcelModel, VendorExcelDataModel>("PRO_PO_VENDOR_MASTER", parameters);
                Res.ExcelsHistory = vendors?.ToList() ?? new List<VendorExcelModel>();
                Res.Vendors = addresses?.ToList() ?? new List<VendorExcelDataModel>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;

        }
        public async Task<GenRes> UpdateCMStatus(UpdateCM_Status_Details req)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@USER_ID", req.USER_ID, DbType.Int32);

            // Table-valued parameter Errors
            var CMStatus = new DataTable();
            CMStatus.Columns.Add("VENDOR_CODE", typeof(string));
            CMStatus.Columns.Add("CM_STATUS", typeof(int));

            foreach (var de in req.Data)
            {
                CMStatus.Rows.Add(de.VendorCode, de.StatusID);
            }
            parameters.Add("@VENDOR_DETAILS", CMStatus.AsTableValuedParameter("TVP_Excel_CM_VendorsDtails"));

            //Output parameters
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_UPDATE_CM_STATUS", parameters);
            if (parameters.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = parameters.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");
                }
            }
            if (ResSet == 0)
            {
                //if()
            }
            return Res;
        }

        public async Task<GenRes> UpdateLinkPO(UpdateLinkPO_Details req)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@INSPID", req.INSP_ID, DbType.Int32);
            parameters.Add("@LINK_PO_NUMBER", req.LinkPoNumber, DbType.String);
            parameters.Add("@USER_ID", req.USER_ID, DbType.Int32);

            //Output parameters
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_UPDATE_LINK_PO", parameters);
            if (parameters.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = parameters.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");
                }
            }

            return Res;
        }

        public async Task<GenRes> SyncPOdetailsService(POHeader req)
        {
            var Res = new GenRes();


            var headerParams = new DynamicParameters();

            headerParams.Add("@POID", req.Id, DbType.Int32, ParameterDirection.InputOutput);
            headerParams.Add("@PO_NUMBER", req.PoNumber);
            headerParams.Add("@FABRIC", req.Fabric);
            headerParams.Add("@COLOR", req.Color);
            headerParams.Add("@TOTAL_QTY", req.POTotalQty);
            headerParams.Add("@USER_ID", req.CreatedBy);

            headerParams.Add("@APPARELS", req.Apparels);
            headerParams.Add("@CATEGORY", req.Category);
            headerParams.Add("@BUYER_ID", req.BuyereId);
            headerParams.Add("@MC", req.MC);
            headerParams.Add("@MC_TEXT", req.MCText);
            headerParams.Add("@MRP", req.MRP);
            headerParams.Add("@BRAND_ID", req.Brand_Id);
            headerParams.Add("@BRAND_ID2", req.Brand_Id2);
            headerParams.Add("@FIT", req.FIT);
            headerParams.Add("@VENDOR_NAME", req.VendorName);   // NEW
            headerParams.Add("@BUYER_NAME", req.BuyerName);     // NEW

            // Output parameters
            headerParams.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            headerParams.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            headerParams.Add("@WARNING_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output); // NEW
            headerParams.Add("@RECORD_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dapperHelper.ExecuteStoredProcedureAsync("SP_UPDATE_POHEADER", headerParams);

            int errorCode = headerParams.Get<int?>("@ERROR_CODE") ?? 0;
            if (errorCode != 0)
            {
                Res.responseCode = errorCode;
                Res.responseMessage = headerParams.Get<string?>("@ERROR_MESSAGE");
                return Res;
            }

            // Success, but quantity impact detected: pass the warning to the UI (starts with "WARNING:")
            var warning = headerParams.Get<string?>("@WARNING_MESSAGE");
            if (!string.IsNullOrWhiteSpace(warning))
                Res.responseMessage = warning;

            Res.recordId = headerParams.Get<int?>("@RECORD_ID") ?? 0;
            return Res;
        }

        public async Task<byte[]> InspectionExportToExcel(SelectListReq Obj)
        {



            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inspection");

            // Headers
            worksheet.Cell(1, 1).Value = "Inspection #";
            worksheet.Cell(1, 2).Value = "PO #";
            worksheet.Cell(1, 3).Value = "Vendor Code";
            worksheet.Cell(1, 4).Value = "Vendor Name ";
            worksheet.Cell(1, 5).Value = "Location";
            worksheet.Cell(1, 6).Value = "Category";
            worksheet.Cell(1, 7).Value = "DoI";
            worksheet.Cell(1, 8).Value = "QA DoI";
            worksheet.Cell(1, 9).Value = "Inspector (QA)";


            var insps = await GetInspectionsListByIds(Obj.StrField!);

            int row = 2;
            foreach (var item in insps.ListofInspections)
            {
                // Sample Data
                worksheet.Cell(row, 1).Value = item.INSPECTION_NO;//"Inspection #";
                worksheet.Cell(row, 2).Value = item.PO_NUMBER;//"PO #";
                worksheet.Cell(row, 3).Value = $"{item.VENDOR_CODE}";//"Vendor Code";
                worksheet.Cell(row, 4).Value = $"{item.VENDOR_NAME}";//"Vendor Name ";
                worksheet.Cell(row, 5).Value = item.ADDRESS_LINE1;//"Location";
                worksheet.Cell(row, 6).Value = item.CATEGORY;//"Category";
                worksheet.Cell(row, 7).Value = item.DATE_OF_INSPECTION; ;//"Description";
                worksheet.Cell(row, 8).Value = item.QA_ASSIGNED_DOI.Year == 1 ? "" : item.QA_ASSIGNED_DOI;
                worksheet.Cell(row, 9).Value = $"{item.EMP_NAME}({item.INSPECTOR_EMP_CODE})";//"Inspector";
                row++;
            }

            //------------------

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<HierarchyListResponse> GetHierarchyList(SelectListReq Obj)
        {
            var Res = new HierarchyListResponse();

            var parameters = new DynamicParameters();
            parameters.Add("@ACTION", Obj.Cmd);
            parameters.Add("@EMP_CODE", Obj.StrField);
            var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<MASTER_HIERARCHY_MODEL>("SP_MASTER_HIERARCHY_CRUD", parameters);
            Res.HierarchyList = (List<MASTER_HIERARCHY_MODEL>)Out;
            return Res;
        }

        public async Task<GenRes> AddUpdateHierarchy(HierarchyDto Obj)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@ACTION", Obj.Action, DbType.String);
            parameters.Add("@EMP_CODE", Obj.EmpCode, DbType.String);
            parameters.Add("@EMP_NAME", Obj.EmpName, DbType.String);
            parameters.Add("@EMAIL_ID", Obj.EmailId, DbType.String);
            parameters.Add("@MOBILE_NO", Obj.MobileNo, DbType.String);
            parameters.Add("@ROLE_ID", Obj.RoleId, DbType.Int32);
            parameters.Add("@REPORTING_EMP_ID", Obj.ReportingEmpId, DbType.String);
            parameters.Add("@IS_ACTIVE", Obj.IsActive, DbType.Boolean);

            //Output parameters
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_MASTER_HIERARCHY_CRUD", parameters);
            if (parameters.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = parameters.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");
                }
            }

            return Res;
        }

        public async Task<EmailManagementResponse> GetEmailMgtList(SelectListReq Obj)
        {
            var Res = new EmailManagementResponse();

            var parameters = new DynamicParameters();
            parameters.Add("@ACTION", Obj.Cmd);
            var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<EmailManagement>("SP_EMAIL_LIST_CRUD", parameters);
            Res.EmailsList = (List<EmailManagement>)Out;
            return Res;
        }
        public async Task<GenRes> AddUpdateEmailMgt(EmailManagementDto Obj)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@ACTION", Obj.Action, DbType.String);
            parameters.Add("@NAME", Obj.Name, DbType.String);
            parameters.Add("@EMAIL_ID", Obj.EmailId, DbType.String);
            parameters.Add("@IS_ACTIVE", Obj.IsActive, DbType.Boolean);

            //Output parameters
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_EMAIL_LIST_CRUD", parameters);
            if (parameters.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = parameters.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");
                }
            }

            return Res;
        }

        public async Task<MasterColorCodeResponse> GetAllColorCodeList(SelectListReq Obj)
        {
            var Res = new MasterColorCodeResponse();

            var parameters = new DynamicParameters();
            parameters.Add("@ACTION", Obj.Cmd);
            var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<MasterColorCode>("SP_MASTER_COLOR_CODE_CRUD", parameters);
            Res.ColorCodeList = (List<MasterColorCode>)Out;
            return Res;
        }
        public async Task<GenRes> AddUpdateColorCode(MasterColorCodeDto Obj)
        {
            var Res = new GenRes();
            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@ACTION", Obj.Action, DbType.String);
            parameters.Add("@COLOR_CODE_ID", Obj.ColorCodeId, DbType.Int32);
            parameters.Add("@COLOR_CODE", Obj.ColorCode, DbType.String);
            parameters.Add("@DESCRIPTION", Obj.Description, DbType.String);
            parameters.Add("@ACTIVE", Obj.Active, DbType.Boolean);
            parameters.Add("@ORDER_NO", Obj.OrderNo, DbType.Int32);

            //Output parameters
            parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
            var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_MASTER_COLOR_CODE_CRUD", parameters);
            if (parameters.ParameterNames.Contains("ERROR_CODE"))
            {
                int errorCode = parameters.Get<int?>("@ERROR_CODE") ?? 0;
                if (errorCode != 0)
                {
                    Res.responseCode = errorCode;
                    Res.responseMessage = parameters.Get<string>("@ERROR_MESSAGE");
                }
            }

            return Res;
        }

        public async Task<InspectionQueryResult> GenerateInspectionPdfAsync(SelectListReq req)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Id", req.Id, DbType.Int32);
            parameters.Add("@StrField", req.StrField, DbType.String);
            parameters.Add("@Cmd", req.Cmd, DbType.String);
            var (detail, defects, reasons) = await _dapperHelper.ExecuteStoredProcedureMultiple3ListAsync<InspectionReportDetail, InspectionDefectItem, AbortiveReasonItem>("PRO_INSPECTION_DETAIL", parameters);
            var InspectDetails = detail?.FirstOrDefault() ?? new InspectionReportDetail();
            var Defects = defects?.ToList() ?? new List<InspectionDefectItem>();
            var Reasons = reasons?.ToList() ?? new List<AbortiveReasonItem>();
            return new InspectionQueryResult(InspectDetails, Defects, Reasons);

        }

        public async Task<ReceivedPendingOngoingInspections> GetAllRecPenOngInspections(InspectionSearchFilterMultiSelection req)
        {
            req.Sanitize();
            var Res = new ReceivedPendingOngoingInspections();
            var parameters = new DynamicParameters();
            parameters.Add("@STRFIELD", req.StrField, DbType.String);
            parameters.Add("@CMD", req.Cmd, DbType.String);
            parameters.Add("@VendorIds", req.VendorIds.Any() ? string.Join(",", req.VendorIds) : null, DbType.String);
            parameters.Add("@BuyerIds", req.BuyerIds.Any() ? string.Join(",", req.BuyerIds) : null, DbType.String);
            parameters.Add("@CategoryIds", req.CategoryIds.Any() ? string.Join(",", req.CategoryIds) : null, DbType.String);
            parameters.Add("@ProductIds", req.ProductIds.Any() ? string.Join(",", req.ProductIds) : null, DbType.String);
            parameters.Add("@InspectorIds", req.InspectorIds.Any() ? string.Join(",", req.InspectorIds) : null, DbType.String);
            parameters.Add("@Mrp", req.Mrp, DbType.Decimal);
            parameters.Add("@PONumber", req.PONumber, DbType.String);
            parameters.Add("@InspectionNumber", req.InspectionNumber, DbType.String);
            parameters.Add("@RequestType", req.RequestType, DbType.String);
            parameters.Add("@SubType", req.SubType, DbType.String);
            parameters.Add("@IS_QC_AT_DC", req.IS_QC_AT_DC, DbType.Boolean);
            parameters.Add("@FromDate", req.FromDate, DbType.Date);
            parameters.Add("@ToDate", req.ToDate, DbType.Date);
            parameters.Add("@PageNumber", req.PageNumber, DbType.Int32);
            parameters.Add("@PageSize", req.PageSize, DbType.Int32);
            //Output parameters
            parameters.Add("@TotalCount", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var (Out, totalCount) = await _dapperHelper.ExecuteStoredProcedureListDPAsync<INSPECTION_REQUEST_LIST_MODEL>("SP_SEARCH_INSPECTIONS", parameters);
            Res.InspectionList = (List<INSPECTION_REQUEST_LIST_MODEL>)Out;
            Res.TotalRows = totalCount;

            return Res;
        }

        public async Task<byte[]> RecPenOngInspectionsExportToExcel(InspectionSearchFilterMultiSelection req)
        {
            string LISTTYPE = req.RequestType.Split('|')[1].Trim();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("All_Inspections");
            #region Header
            // Headers
            if (LISTTYPE == "ALL_LIST")
            {
                worksheet.Cell(1, 1).Value = "Inspection Date";
                worksheet.Cell(1, 2).Value = "Type of Inspection";
                worksheet.Cell(1, 3).Value = "Inspection Report No.";
                worksheet.Cell(1, 4).Value = "Name of the VMM QA";
                worksheet.Cell(1, 5).Value = "Buyer";
                worksheet.Cell(1, 6).Value = "Article No.";
                worksheet.Cell(1, 7).Value = "PO #";
                worksheet.Cell(1, 8).Value = "Vendor";
                worksheet.Cell(1, 9).Value = "Description";
                worksheet.Cell(1, 10).Value = "MRP";
                worksheet.Cell(1, 11).Value = "Brand Label";
                worksheet.Cell(1, 12).Value = "Color";
                worksheet.Cell(1, 13).Value = "FIT";
                worksheet.Cell(1, 14).Value = "Category";
                worksheet.Cell(1, 15).Value = "PO QTY.";
                worksheet.Cell(1, 16).Value = "Offered Qty ";
                worksheet.Cell(1, 17).Value = "APPS/NON APPS";
                worksheet.Cell(1, 18).Value = "Location";
                worksheet.Cell(1, 19).Value = "Address";
                worksheet.Cell(1, 20).Value = "Contact Name ";
                worksheet.Cell(1, 21).Value = "Contact #";
            }
            if (LISTTYPE == "REPORT_LIST")
            {
                worksheet.Cell(1, 1).Value = "Inspection Date";
                worksheet.Cell(1, 2).Value = "Type of Inspection";
                worksheet.Cell(1, 3).Value = "Inspection Report No.";
                worksheet.Cell(1, 4).Value = "Name of the VMM QA";
                worksheet.Cell(1, 5).Value = "Buyer Name";
                worksheet.Cell(1, 6).Value = "Article No.";
                worksheet.Cell(1, 7).Value = "PO #";
                worksheet.Cell(1, 8).Value = "Vendor Name";
                worksheet.Cell(1, 9).Value = "Description";
                worksheet.Cell(1, 10).Value = "MRP";
                worksheet.Cell(1, 11).Value = "Brand Label";
                worksheet.Cell(1, 12).Value = "Color";
                worksheet.Cell(1, 13).Value = "FIT";
                worksheet.Cell(1, 14).Value = "Category";
                worksheet.Cell(1, 15).Value = "PO QTY.";
                worksheet.Cell(1, 16).Value = "Offered Qty ";
                worksheet.Cell(1, 17).Value = "Ship Qty";
                worksheet.Cell(1, 18).Value = "Sample Size";
                worksheet.Cell(1, 19).Value = "APPS/NON APPS";
                worksheet.Cell(1, 20).Value = "Location";
                worksheet.Cell(1, 21).Value = "Address";
                worksheet.Cell(1, 22).Value = "Contact Name ";
                worksheet.Cell(1, 23).Value = "Contact #";
                worksheet.Cell(1, 24).Value = "Inspection Result";
                worksheet.Cell(1, 25).Value = "Fail Reason";
                worksheet.Cell(1, 26).Value = "Abortive Reason";
                worksheet.Cell(1, 27).Value = "Defects";
                worksheet.Cell(1, 28).Value = "Allowed";
                worksheet.Cell(1, 29).Value = "Status";
                worksheet.Cell(1, 30).Value = "Remarks Comments";
                worksheet.Cell(1, 31).Value = "Note";
                worksheet.Cell(1, 32).Value = "Fiber Content OnTest Report";
                worksheet.Cell(1, 33).Value = "Fiber Content On Label";
                worksheet.Cell(1, 34).Value = "Fiber Content On PO";
                worksheet.Cell(1, 35).Value = "GSM on PO";
                worksheet.Cell(1, 36).Value = "GSM on Test Report";
                worksheet.Cell(1, 37).Value = "GSM On Bulk";
                worksheet.Cell(1, 38).Value = "No. of Bags/Carton";
                worksheet.Cell(1, 39).Value = "Selected Bags/Carton No.";
                worksheet.Cell(1, 40).Value = "No. of Check Carton/Bag";
                worksheet.Cell(1, 41).Value = "No. of Check Pre-Pack";
                worksheet.Cell(1, 42).Value = "Mesurement Sample Size";
                worksheet.Cell(1, 43).Value = "Measurement Pass/Fail";
                worksheet.Cell(1, 44).Value = "Packing Status";
                worksheet.Cell(1, 45).Value = "Packing List";
                worksheet.Cell(1, 46).Value = "Inline Done";
                worksheet.Cell(1, 47).Value = "PPS Release Date";
                worksheet.Cell(1, 48).Value = "PPS Expiry Date";
                worksheet.Cell(1, 49).Value = "MFG Date";
                worksheet.Cell(1, 50).Value = "Season";
                worksheet.Cell(1, 51).Value = "Color Code";
                worksheet.Cell(1, 52).Value = "GPT Report No";
                worksheet.Cell(1, 53).Value = "Test Report Status";
                worksheet.Cell(1, 54).Value = "Area of failure in GPT Test ";
                worksheet.Cell(1, 55).Value = "Approved Date";
                worksheet.Cell(1, 56).Value = "Approved By (if GPT fails)";
                worksheet.Cell(1, 57).Value = "GPT Elastic No.";
                worksheet.Cell(1, 58).Value = "Test Report Status";
                worksheet.Cell(1, 59).Value = "Approved Date";
                worksheet.Cell(1, 60).Value = "Approved By (if Elastic fails)";
                worksheet.Cell(1, 61).Value = "Zipper Report No.";
                worksheet.Cell(1, 62).Value = "Zipper Report Status";
                worksheet.Cell(1, 63).Value = "Approved Date";
                worksheet.Cell(1, 64).Value = "If Fail (Zipper) Approved By";
                worksheet.Cell(1, 65).Value = "Size";
                worksheet.Cell(1, 66).Value = "Embriodery/Print (Should be as per PPS)";
                worksheet.Cell(1, 67).Value = "Accessories (Should be as per PPS)";
                worksheet.Cell(1, 68).Value = "Barcode Compliances";
                worksheet.Cell(1, 69).Value = "No Plastic Tag/Kimble/Tag";
                worksheet.Cell(1, 70).Value = "BOGO / Discount Tag";
                worksheet.Cell(1, 71).Value = "Pre-Pack Ratio";
                worksheet.Cell(1, 72).Value = "Thread From Nominated (Y/N)";
                worksheet.Cell(1, 73).Value = "Button From Nominated (Y/N)";
                worksheet.Cell(1, 74).Value = "Zipper From Nominated (Y/N)";
                worksheet.Cell(1, 75).Value = "Interlining From Nominated Vendor (Y/N)";
                worksheet.Cell(1, 76).Value = "Elastic From Nominated (Y/N)";
                worksheet.Cell(1, 77).Value = "FI Comm. Appr. Date";
                worksheet.Cell(1, 78).Value = "FI Comm. Approved By ";
                worksheet.Cell(1, 79).Value = "Comm. Approval Documents (mail dated, debit etc)";
                worksheet.Cell(1, 80).Value = "Debit Amount";
                worksheet.Cell(1, 81).Value = "Abortive Charges";
                worksheet.Cell(1, 82).Value = "Admin Remark";
            }
            //-----------------
            #endregion
            req.RequestType = "REPORT";
            var POList = await GetAllRecPenOngInspections(req);
            int row = 2;
            if (POList != null)
            {
                if (POList.InspectionList.Count > 0)
                {
                    foreach (var item in POList.InspectionList)
                    {
                        if (LISTTYPE == "ALL_LIST")
                        {
                            #region Data
                            worksheet.Cell(row, 1).Value = item.DATE_OF_INSPECTION;//"Inspection Date ";
                            worksheet.Cell(row, 2).Value = item.TYPE_OF_INSPECTION;//"Type of Inspection";
                            worksheet.Cell(row, 3).Value = item.INSPECTION_REPORT_NO;//"Inspection Report No.";
                            worksheet.Cell(row, 4).Value = req.IsVendor ? "" : item.INSPECTOR;//"Name of the VMM QA";
                            worksheet.Cell(row, 5).Value = item.BUYER_NAME;//"Buyer Name";
                            worksheet.Cell(row, 6).Value = item.ARTICLE_NO;//"Article No.";
                            worksheet.Cell(row, 7).Value = item.PO_NUMBER;//"PO #";
                            worksheet.Cell(row, 8).Value = $"{item.VENDOR}";//"Vendor Code";
                            worksheet.Cell(row, 9).Value = item.DESCRIPTION;//"Description";
                            worksheet.Cell(row, 10).Value = item.MRP;//"MRP";
                            worksheet.Cell(row, 11).Value = item.BRAND_LABEL;//"Brand Label";
                            worksheet.Cell(row, 12).Value = item.COLOR;//"Color";
                            worksheet.Cell(row, 13).Value = item.FIT;//"FIT";
                            worksheet.Cell(row, 14).Value = item.CATEGORY;//"Category";
                            worksheet.Cell(row, 15).Value = item.PO_QTY;//"PO QTY.";
                            worksheet.Cell(row, 16).Value = item.OFFERED_QTY;//"Offered Qty ";
                            worksheet.Cell(row, 17).Value = item.APPS_NON_APPS;//"APPS/NON APPS";
                            worksheet.Cell(row, 18).Value = item.LOCATION;//"Location";
                            worksheet.Cell(row, 19).Value = item.ADDRESS;//"Address";
                            worksheet.Cell(row, 20).Value = item.CONTACT_NAME;//"Contact Name ";
                            worksheet.Cell(row, 21).Value = item.CONTACT_NUMBER;//"Contact #";
                            #endregion
                        }
                        if (LISTTYPE == "REPORT_LIST")
                        {
                            #region Data
                            worksheet.Cell(row, 1).Value = item.DATE_OF_COMPLETION;//"Inspection Date ";
                            worksheet.Cell(row, 2).Value = item.TYPE_OF_INSPECTION;//"Type of Inspection";
                            worksheet.Cell(row, 3).Value = item.INSPECTION_REPORT_NO;//"Inspection Report No.";
                            worksheet.Cell(row, 4).Value = item.INSPECTOR;//"Name of the VMM QA";
                            worksheet.Cell(row, 5).Value = item.BUYER_NAME;//"Buyer Name";
                            worksheet.Cell(row, 6).Value = item.ARTICLE_NO;//"Article No.";
                            worksheet.Cell(row, 7).Value = item.PO_NUMBER;//"PO #";
                            worksheet.Cell(row, 8).Value = $"{item.VENDOR}";//"Vendor Code";
                            worksheet.Cell(row, 9).Value = item.DESCRIPTION;//"Description";
                            worksheet.Cell(row, 10).Value = item.MRP;//"MRP";
                            worksheet.Cell(row, 11).Value = item.BRAND_LABEL;//"Brand Label";
                            worksheet.Cell(row, 12).Value = item.COLOR;//"Color";
                            worksheet.Cell(row, 13).Value = item.FIT;//"FIT";
                            worksheet.Cell(row, 14).Value = item.CATEGORY;//"Category";
                            worksheet.Cell(row, 15).Value = item.PO_QTY;//"PO QTY.";
                            worksheet.Cell(row, 16).Value = item.OFFERED_QTY;//"Offered Qty ";
                            worksheet.Cell(row, 17).Value = item.SHIP_QTY;//"Ship Qty";
                            worksheet.Cell(row, 18).Value = item.SAMPLE_SIZE;//"Sample Size";
                            worksheet.Cell(row, 19).Value = item.APPS_NON_APPS;//"APPS/NON APPS";
                            worksheet.Cell(row, 20).Value = item.LOCATION;//"Location";
                            worksheet.Cell(row, 21).Value = item.ADDRESS;//"Address";
                            worksheet.Cell(row, 22).Value = item.CONTACT_NAME;//"Contact Name ";
                            worksheet.Cell(row, 23).Value = item.CONTACT_NUMBER;//"Contact #";
                            worksheet.Cell(row, 24).Value = item.INSP_RESULT;//"Inspection_Result";
                            worksheet.Cell(row, 25).Value = item.FAIL_REASON;//"Fail Reason";
                            worksheet.Cell(row, 26).Value = item.ABORTIVE_REASON;//"Abortive Reason";
                            worksheet.Cell(row, 27).Value = item.DEFECTS;//"Defects";
                            worksheet.Cell(row, 28).Value = item.ALLOWED;//"Allowed";
                            worksheet.Cell(row, 29).Value = item.STATUS;//"Status";
                            worksheet.Cell(row, 30).Value = item.REMARKS_DETAILS;//"Remarks (details)";
                            worksheet.Cell(row, 31).Value = item.NOTE;//"Note";
                            worksheet.Cell(row, 32).Value = item.FIBER_CONTENT_ON_TEST_REPORT;//"Fiber Content OnTest Report";
                            worksheet.Cell(row, 33).Value = item.FIBER_CONTENT_ON_LABEL;//"Fiber Content On Label";
                            worksheet.Cell(row, 34).Value = item.FIBER_CONTENT_ON_PO;//"Fiber Content On PO";
                            worksheet.Cell(row, 35).Value = item.GSM_ON_PO;//"GSM on PO";
                            worksheet.Cell(row, 36).Value = item.GSM_ON_TEST_REPORT;//"GSM on Test Report";
                            worksheet.Cell(row, 37).Value = item.GSM_ON_BULK;//"GSM On Bulk";
                            worksheet.Cell(row, 38).Value = item.NO_OF_BAGS_CARTON;//"No. of Bags/Carton";
                            worksheet.Cell(row, 39).Value = item.SELECTED_BAGS_CARTON_NO;//"Selected Bags/Carton No.";
                            worksheet.Cell(row, 40).Value = item.NO_OF_CHECK_CARTON_BAG;//"No. of Check Carton/Bag";
                            worksheet.Cell(row, 41).Value = item.NO_OF_CHECK_PRE_PACK;//"No. of Check Pre-Pack";
                            worksheet.Cell(row, 42).Value = item.MESUREMENT_SAMPLE_SIZE;//"Mesurement Sample Size";
                            worksheet.Cell(row, 43).Value = item.MEASUREMENT_PASS_FAIL;//"Measurement Pass/Fail";
                            worksheet.Cell(row, 44).Value = item.PACKING_STATUS;//"Packing Status";
                            worksheet.Cell(row, 45).Value = item.PACKING_LIST;//"Packing List";
                            worksheet.Cell(row, 46).Value = item.INLINE_DONE;//"Inline Done";
                            worksheet.Cell(row, 47).Value = item.PPS_RELEASE_DATE;//"PPS Release Date";
                            worksheet.Cell(row, 48).Value = item.PPS_EXPIRY_DATE;//"PPS Expiry Date";
                            worksheet.Cell(row, 49).Value = item.MFG_DATE;//"MFG Date";
                            worksheet.Cell(row, 50).Value = item.SEASON;//"Season";
                            worksheet.Cell(row, 51).Value = item.COLOR_CODE;//"Color Code";
                            worksheet.Cell(row, 52).Value = item.GPT_REPORT_NO;//"GPT Report No";
                            worksheet.Cell(row, 53).Value = item.TEST_REPORT_STATUS;//"Test Report Status";
                            worksheet.Cell(row, 54).Value = item.AREA_OF_FAILURE_IN_GPT_TEST;//"Area of failure in GPT Test ";
                            worksheet.Cell(row, 55).Value = item.APPROVED_DATE;//"Approved Date";
                            worksheet.Cell(row, 56).Value = item.APPROVED_BY_IF_GPT_FAILS;//"Approved By (if GPT fails)";
                            worksheet.Cell(row, 57).Value = item.GPT_ELASTIC_NO;//"GPT Elastic No.";
                            worksheet.Cell(row, 58).Value = item.ELASTIC_TEST_REPORT_STATUS;//"Test Report Status";
                            worksheet.Cell(row, 59).Value = item.ELASTIC_APPROVED_DATE;//"Approved Date";
                            worksheet.Cell(row, 60).Value = item.APPROVED_BY_IF_ELASTIC_FAILS;//"Approved By (if Elastic fails)";
                            worksheet.Cell(row, 61).Value = item.ZIPPER_REPORT_NO;//"Zipper Report No.";
                            worksheet.Cell(row, 62).Value = item.ZIPPER_REPORT_STATUS;//"Zipper Report Status";
                            worksheet.Cell(row, 63).Value = item.ZIPPER_APPROVED_DATE;//"Approved Date";
                            worksheet.Cell(row, 64).Value = item.IF_FAIL_ZIPPER_APPROVED_BY;//"If Fail (Zipper) Approved By";
                            worksheet.Cell(row, 65).Value = item.SIZE;//"Size";
                            worksheet.Cell(row, 66).Value = item.EMBRIODERY_PRINT_SHOULD_BE_AS_PER_PPS;//"Embriodery/Print (Should be as per PPS)";
                            worksheet.Cell(row, 67).Value = item.ACCESSORIES_SHOULD_BE_AS_PER_PPS;//"Accessories (Should be as per PPS)";
                            worksheet.Cell(row, 68).Value = item.BARCODE_COMPLIANCES;//"Barcode Compliances";
                            worksheet.Cell(row, 69).Value = item.NO_PLASTIC_TAG_KIMBLE_TAG;//"No Plastic Tag/Kimble/Tag";
                            worksheet.Cell(row, 70).Value = item.BOGO_DISCOUNT_TAG;//"BOGO / Discount Tag";
                            worksheet.Cell(row, 71).Value = item.PRE_PACK_RATIO;//"Pre-Pack Ratio";
                            worksheet.Cell(row, 72).Value = item.THREAD_FROM_NOMINATED_Y_N;//"Thread From Nominated (Y/N)";
                            worksheet.Cell(row, 73).Value = item.BUTTON_FROM_NOMINATED_Y_N;//"Button From Nominated (Y/N)";
                            worksheet.Cell(row, 74).Value = item.ZIPPER_FROM_NOMINATED_Y_N;//"Zipper From Nominated (Y/N)";
                            worksheet.Cell(row, 75).Value = item.INTERLINING_FROM_NOMINATED_VENDOR_Y_N;//"Interlining From Nominated Vendor (Y/N)";
                            worksheet.Cell(row, 76).Value = item.ELASTIC_FROM_NOMINATED_Y_N;//"Elastic From Nominated (Y/N)";
                            worksheet.Cell(row, 77).Value = item.FI_COMM_APPR_DATE;//"FI Comm. Appr. Date";
                            worksheet.Cell(row, 78).Value = item.FI_COMM_APPROVED_BY;//"FI Comm. Approved By ";
                            worksheet.Cell(row, 79).Value = item.COMM_APPROVAL_DOCUMENTS;//"Comm. Approval Documents (mail dated, debit etc)";
                            worksheet.Cell(row, 80).Value = item.DEBIT_AMOUNT;//"Debit Amount";
                            worksheet.Cell(row, 81).Value = item.AMOUNT_OF_TRAVELLING_AND_OTHER_EXPENCES;//"Amount of Travelling and Other Expences";
                            worksheet.Cell(row, 82).Value = item.ADMIN_REMARK;// "Admin Remark";
                            #endregion
                        }
                        row++;
                    }
                }
                else
                {
                    worksheet.Range(row, 1, row, 21).Merge();
                    // Set value after merge
                    worksheet.Cell(row, 1).Value = "No Record Found.";

                    // Optional formatting
                    worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                }
            }
            else
            {

                worksheet.Range(row, 1, row, 21).Merge();
                // Set value after merge
                worksheet.Cell(row, 1).Value = "Data Found.";

                // Optional formatting
                worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(row, 1).Style.Font.Bold = true;
            }
            //------------------

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
