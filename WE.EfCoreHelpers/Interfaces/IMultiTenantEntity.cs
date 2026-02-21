using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WE.EfCoreHelpers.Interfaces
{
    public interface IMultiTenantEntity
    {
        Guid TenantId { get; set; }
    }
}
