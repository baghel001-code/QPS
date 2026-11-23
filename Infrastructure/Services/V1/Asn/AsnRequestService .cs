using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.V1.Asn
{
    using Application.DTOs.Asn;
    using Application.Interfaces;
    using Application.Interfaces.V1.Asn;
    using Dapper;
    using Domain.Entities;
    using Domain.Entities.Modals;
    using Domain.Entities.Request;
    using Domain.Entities.Response;
    using Domain.Enums;
    using Domain.Records;
    using global::Infrastructure.Utilitys;
    using System.Data;

    namespace Infrastructure.Services
    {
        public class AsnRequestService : IAsnRequestService
        {
            private const string ProcName = "[asn].[POIR_ASN_Request_CRUD]";
            private readonly DapperHelper _dapperHelper;
            public AsnRequestService(DapperHelper dapperHelper)
            {
                _dapperHelper = dapperHelper;
            }

            public async Task<GenRes> CreateAsync(CreateAsnRequestDto dto, CancellationToken ct = default)
            {
                var Res = new GenRes();
                var parameters = new DynamicParameters();
                parameters.Add("@CMD", "CREATE");
                parameters.Add("@INSP_ID", dto.InspId);
                parameters.Add("@INSPECT_ID", dto.InspectId);
                parameters.Add("@SHIP_QTY", dto.ShipQty);
                parameters.Add("@REQUESTED_QTY", dto.RequestedQty);
                parameters.Add("@REMARKS", dto.Remarks);
                parameters.Add("@PO_NET_VALUE", dto.PoNetValue);
                parameters.Add("@CREATED_BY", dto.CreatedBy);
                //return await _dapperHelper.ExecuteStoredProcedureAsync(ProcName, parameters);
                parameters.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
                var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync(ProcName, parameters);
                if (!parameters.Get<int>("Error_Code").Equals(0))
                {
                    Res.responseCode = parameters.Get<int>("Error_Code");
                    Res.responseMessage = parameters.Get<string>("Error_Message");
                }
                return Res;
            }

            public async Task<GenRes> CompleteAsync(CompleteAsnRequestDto dto, CancellationToken ct = default)
            {
                var Res = new GenRes();
                var parameters = new DynamicParameters();
                parameters.Add("@CMD", "COMPLETE");
                parameters.Add("@ASN_REQ_ID", dto.AsnReqId);
                parameters.Add("@ASN_NUMBER", dto.AsnNumber);
                parameters.Add("@ASN_ATTACHMENT", dto.AsnAttachment);
                parameters.Add("@UPDATED_BY", dto.UpdatedBy);
                //await _dapperHelper.ExecuteStoredProcedureAsync(ProcName, parameters);
                parameters.Add("@Error_Code", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Error_Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);
                var ResSet = await _dapperHelper.ExecuteStoredProcedureAsync(ProcName, parameters);
                if (!parameters.Get<int>("Error_Code").Equals(0))
                {
                    Res.responseCode = parameters.Get<int>("Error_Code");
                    Res.responseMessage = parameters.Get<string>("Error_Message");
                }
                return Res;
            }

            public async Task UpdateGrcAsync(UpdateAsnGrcDto dto, CancellationToken ct = default)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CMD", "UPDATE_GRC");
                parameters.Add("@ASN_REQ_ID", dto.AsnReqId);
                parameters.Add("@GRC_QTY", dto.GrcQty);
                parameters.Add("@GRC_VALUE", dto.GrcValue);
                parameters.Add("@GRC_DATE", dto.GrcDate);
                parameters.Add("@UPDATED_BY", dto.UpdatedBy);
                await _dapperHelper.ExecuteStoredProcedureAsync(ProcName, parameters);
            }

            public async Task<AsnDetailsSummary> GetSummaryByPoAsync(string poNumber, CancellationToken ct = default)
            {

                var parameters = new DynamicParameters();
                parameters.Add("@CMD", "GET_SUMMARY_BY_PO");
                parameters.Add("@PO_NUMBER", poNumber);
                var Out = await _dapperHelper.ExecuteStoredProcedureListAsync<AsnSummaryFlatRow>(ProcName, parameters);
                var flatRows = (List<AsnSummaryFlatRow>)Out;
                return new AsnDetailsSummary
                {
                    PoNumber = poNumber,
                    Inspections = flatRows
                        .GroupBy(r => r.InspectionNumber)
                        .Select(g => new InspectionAsnGroup
                        {
                            InspectionNumber = g.Key,
                            ShipQty = g.First().ShipQty,
                            Rows = g.Select(r => new AsnDetailRow
                            {
                                Sno = (int)r.Sno,
                                RequestedQty = r.RequestedQty,
                                AsnNumber = r.AsnNumber,
                                DateOfCreation = r.DateOfCreation,
                                BalanceQty = r.BalanceQty,
                                Status = r.Status,
                                Remarks = r.Remarks,
                                ASN_ATTACHMENT=r.ASN_ATTACHMENT
                            }).ToList()
                        }).ToList()
                };

            }

            public async Task<IEnumerable<AsnRequest>> GetByInspectionAsync(long inspId, CancellationToken ct = default)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CMD", "GET_BY_INSPECTION");
                parameters.Add("@INSP_ID", inspId);
                return await _dapperHelper.ExecuteStoredProcedureListAsync<AsnRequest>(ProcName, parameters);
            }

            public async Task<PendingASNList> GetPendingAsync(ASNRequest_PendingComplete req)
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CMD", req.CMD);
                parameters.Add("@CREATED_BY", req.UserId);

                var rows = await _dapperHelper.ExecuteStoredProcedureListAsync<AsnSummaryFlatRow>(ProcName, parameters);

                return new PendingASNList
                {
                    asnSummaryFlatRows = rows.ToList(),
                };
            }

            public async Task<AsnRequest?> GetByIdAsync(long asnReqId, CancellationToken ct = default)
            {
                var parameters = new DynamicParameters();
                parameters.Add("CMD", "GET_BY_ID");
                parameters.Add("ASN_REQ_ID", asnReqId);
                var OUT = await _dapperHelper.ExecuteStoredProcedureListAsync<AsnRequest>(ProcName, parameters);
                return OUT.SingleOrDefault();
            }

            public async Task<AsnList> GetASNListAsync(string PoNumber)
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@CMD", "GET_PO_WISE_ASN_LIST");
                    parameters.Add("@PO_NUMBER", PoNumber);
                    var res= await _dapperHelper.ExecuteStoredProcedureListAsync<string>(ProcName, parameters);
                    return new AsnList(true, (List<string>)res, null);
                }
                catch (Exception ex) { return new AsnList(false,new List<string>(),ex.Message); }


            }

           
        }
    }
}
