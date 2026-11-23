using Application.Interfaces.V1;
using Domain.Entities.Common;
using Domain.Entities.Request;
using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class FetchMasterListService : IFetchMasterList
    {
        private readonly IAPICall _api;

        public FetchMasterListService(IAPICall api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
        }
        public Task<MasterDataList> FetchMasterList(string cmd, int id = 0, string strField = "") =>
           _api.CallingAPI<MasterDataList, SelectListReq>(AllApiNames.GET_MASTER_DATA,new SelectListReq { Id = id, Cmd = cmd, StrField = strField });
    }
}
