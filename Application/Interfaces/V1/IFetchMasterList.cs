using Domain.Entities.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.V1
{
    public interface IFetchMasterList
    {
        Task<MasterDataList> FetchMasterList(string cmd, int id = 0, string strField = "");
    }
}
