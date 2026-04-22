using POS_system_cs.Application.Models;

namespace POS_system_cs.Application.Services;

public interface IReportService
{
    Task<ReportDashboard> GetDashboardAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
