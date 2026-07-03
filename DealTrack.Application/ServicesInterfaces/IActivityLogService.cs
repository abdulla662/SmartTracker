using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealTrack.Application.ServicesInterfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(string action, Guid? entityId = null, string? entityType = null, CancellationToken ct = default);
    }
}
