using Application.Interfaces.V1.Asn;
using Domain.Entities.Modals;
using Domain.Enums;
using Domain.Static;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Asn
{
    public class GetAsnListResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public GetAsnListResolver(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public IGetAsn Resolve(InstanceOrigin objectType)
        {
            return _serviceProvider.GetRequiredKeyedService<IGetAsn>(objectType);
        }
    }
}
