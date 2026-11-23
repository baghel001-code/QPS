using Application.Interfaces.V1;
using Dapper;
using DocumentFormat.OpenXml.Drawing;
using Domain.Entities.Modals;
using Domain.Entities.Request;
using Domain.Entities.Response;
using Infrastructure.Utilitys;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services.V1
{
    internal class VendorAuditService : IVendorAudit
    {
        private readonly DapperHelper _dapperHelper;
        public VendorAuditService(DapperHelper dapperHelper)
        {
            _dapperHelper = dapperHelper;
        }

        public async Task<GenRes> CreateUpdateVendorAuditRequest(VendorAuditFormModel req)
        {
            var Res = new GenRes();
            try
            {
                var parameters = new DynamicParameters();
                // Input parameters
                parameters.Add("@AUDIT_REQ_ID", req.AuditReqId, DbType.Int64);
                parameters.Add("@USER_ID", req.UserId, DbType.Int32);
                parameters.Add("@TEMP_VENDOR_CODE", req.TempVendorCode, DbType.String);
                parameters.Add("@BUSINESS_HEAD", req.BusinessHead, DbType.Int32);
                parameters.Add("@AUDIT_DATE", req.AuditDate, DbType.Date);
                parameters.Add("@GST_NUMBER", req.GstNumber, DbType.String);
                parameters.Add("@VENDOR_NAME", req.VendorName, DbType.String);
                parameters.Add("@REGISTERED_ADDRESS", req.RegisteredAddress, DbType.String);
                parameters.Add("@FACTORY_LOCATION", req.FactoryLocation, DbType.String);
                parameters.Add("@CONTACT_NAME", req.ContactName, DbType.String);
                parameters.Add("@CONTACT_NUMBER", req.ContactNumber, DbType.String);
                parameters.Add("@CATEGORY", req.Category, DbType.String);
                parameters.Add("@REQUESTED_BUYER_CODE", req.RequestedBuyerName, DbType.String);
                parameters.Add("@PRODUCT_CATEGORY", req.ProductCategory, DbType.String);
                parameters.Add("@FACTORY_LICENCE_NUMBER", req.FactoryLicenceNumber, DbType.String);
                parameters.Add("@FACTORY_AREA_SQFT", req.FactoryAreaSqFt, DbType.Decimal);
                parameters.Add("@NUMBER_OF_EMPLOYEES", req.NumberOfEmployees, DbType.Int32);
                parameters.Add("@GEN_ATTACHMENT_FIRST", req.GenAttachmentFirst, DbType.String);
                parameters.Add("@GST_API_RAW_RESPONSE", req.GSTApiResponse, DbType.String);
                parameters.Add("@IS_COMPLETE", req.IsComplete, DbType.Boolean);
                parameters.Add("@REQ_STATUS", req.ReqStatus, DbType.String);
                parameters.Add("@REJECTION_REASON", req.RejectionReason, DbType.String);
                parameters.Add("@APPROVED_BY", req.ApprovedBy, DbType.Int32);
                parameters.Add("@PROMOTE_TO_MASTER", req.PromoteToMaster, DbType.Boolean);
                parameters.Add("@VENDOR_MASTER_ID", req.VendorMasterId, DbType.Int32);

                // Table-valued parameter
                var checkListTable = new DataTable();
                checkListTable.Columns.Add("CHECKLIST_ID", typeof(int));
                checkListTable.Columns.Add("CHECKLIST_MST_ID", typeof(int));
                checkListTable.Columns.Add("STATUS", typeof(int));
                checkListTable.Columns.Add("VALIDITY_DATE", typeof(string));
                checkListTable.Columns.Add("REMARKS", typeof(string));
                checkListTable.Columns.Add("ATTACHMENTS", typeof(string));
                foreach (var item in req.Checklist)
                {
                    checkListTable.Rows.Add(item.Id, item.CheckListMasterId, item.Status, item.ValidityDate, item.Remarks, item.Attachments);
                }
                parameters.Add("@Checklist", checkListTable.AsTableValuedParameter("dbo.TVP_ChecklistUpsert"), dbType: DbType.Object, direction: ParameterDirection.Input);



                //Output parameters
                parameters.Add("@RECORD_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ERROR_CODE", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ERROR_MESSAGE", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
                var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync("SP_FACTORY_AUDIT_REQUEST_SAVE", parameters);
                if (!parameters.Get<int>("ERROR_CODE").Equals(0))
                {
                    Res.responseCode = parameters.Get<int>("ERROR_CODE");
                    Res.responseMessage = parameters.Get<string>("ERROR_MESSAGE");
                }
                else
                {
                    Res.recordId = parameters.Get<int>("RECORD_ID");
                }
            }
            catch
            {

            }
            return Res;
        }

        public async Task<GetFactoryAuditListResponse> GetAllVendorAuditRequest(FactoryAuditListRequest req)
        {
            var Res = new GetFactoryAuditListResponse();

            var parameters = new DynamicParameters();
            // Input parameters
            parameters.Add("@BUSINESS_HEAD", req.BUSINESS_HEAD, DbType.Int32);
            parameters.Add("@REQ_STATUS", req.REQ_STATUS, DbType.String);
            parameters.Add("@GST_NUMBER", req.GST_NUMBER, DbType.String);
            parameters.Add("@VENDOR_NAME", req.VENDOR_NAME, DbType.String);
            parameters.Add("@AUDIT_DATE_FROM", req.AUDIT_DATE_FROM, DbType.Date);
            parameters.Add("@AUDIT_DATE_TO", req.AUDIT_DATE_TO, DbType.Date);
            parameters.Add("@IS_COMPLETE", req.IS_COMPLETE, DbType.Boolean);
            parameters.Add("@PROMOTED_TO_MASTER", req.PROMOTED_TO_MASTER, DbType.Boolean);
            parameters.Add("@REQUESTED_BUYER_CODE", req.REQUESTED_BUYER_CODE, DbType.String);
            parameters.Add("@INCLUDE_INACTIVE", req.INCLUDE_INACTIVE, DbType.Boolean);
            parameters.Add("@PAGE_NUMBER", req.PAGE_NUMBER, DbType.Int32);
            parameters.Add("@PAGE_SIZE", req.PAGE_SIZE, DbType.Int32);

            var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<FactoryAuditRequestDto>("SP_FACTORY_AUDIT_REQUEST_GETLIST", parameters);
            Res.ITEMS = (List<FactoryAuditRequestDto>)Out;
            return Res;
        }

        public async Task<GSTINLocalDBResponse> GetGSTDetails(SelectListReq req)
        {
            var Res = new GSTINLocalDBResponse();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Id", req.Id, DbType.Int32);
                parameters.Add("@StrField", req.StrField, DbType.String);
                parameters.Add("@Cmd", req.Cmd, DbType.String);
                var GST = await _dapperHelper.ExecuteStoredProcedureListAsync<GSTINLocalDBResponse>("PRO_PO_VENDOR_MASTER", parameters);
                Res = GST != null ? GST.First() : new GSTINLocalDBResponse();
                return Res;
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;
        }

        public async Task<GetFactoryAuditByIdResponse> GetVendorAuditRequestByID(SelectListReq req)
        {
            var Res = new GetFactoryAuditByIdResponse();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AUDIT_REQ_ID", req.Id, DbType.Int32);
                parameters.Add("@INCLUDE_INACTIVE", 0, DbType.Boolean);

                var (auditReq, checklist) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<FactoryAuditRequestDto, FactoryAuditChecklistDto>("SP_FACTORY_AUDIT_REQUEST_GETBYID", parameters);
                Res.HEADER = auditReq?.FirstOrDefault() ?? new FactoryAuditRequestDto();
                Res.CHECKLIST = checklist?.ToList() ?? new List<FactoryAuditChecklistDto>();
            }
            catch (Exception ex)
            {
                Res.responseCode = 1;
                Res.responseMessage = ex.Message;
            }
            return Res;
        }
    }
}
