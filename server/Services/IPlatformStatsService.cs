using MediAid.Models;

namespace MediAid.Services;

public interface IPlatformStatsService
{
    Task<PlatformStats> GetStatsAsync();
}
