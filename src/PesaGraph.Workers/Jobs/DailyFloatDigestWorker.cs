using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PesaGraph.Liquidity.Services;
using PesaGraph.Notifications.Services;
using PesaGraph.Tenancy.Domain;
using PesaGraph.Tenancy.Repositories;

namespace PesaGraph.Workers.Jobs;

public class DailyFloatDigestWorker : BackgroundService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILiquidityService _liquidityService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<DailyFloatDigestWorker> _logger;

    public DailyFloatDigestWorker(
        ITenantRepository tenantRepository,
        ILiquidityService liquidityService,
        INotificationDispatcher notificationDispatcher,
        ILogger<DailyFloatDigestWorker> logger)
    {
        _tenantRepository = tenantRepository;
        _liquidityService = liquidityService;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DailyFloatDigestWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var tenants = await _tenantRepository.ListAsync(TenantStatus.Active, stoppingToken);

                foreach (var tenant in tenants)
                {
                    if (string.IsNullOrWhiteSpace(tenant.ContactPhone)) continue;

                    var cockpitResult = await _liquidityService.GetFloatCockpitAsync(tenant.Id, 50000m, stoppingToken);
                    if (cockpitResult.IsSuccess && cockpitResult.Value.ActiveAlerts.Count > 0)
                    {
                        var alertMessage = $"⚠️ *PesaGraph Daily Float Alert*\nTenant: {tenant.Name}\nTotal Liquidity: KES {cockpitResult.Value.TotalLiquidFloat:N2}\n\nThere are {cockpitResult.Value.ActiveAlerts.Count} low-float account warnings requiring attention.";
                        await _notificationDispatcher.SendNotificationAsync(new NotificationMessage(
                            tenant.Id,
                            tenant.ContactPhone,
                            alertMessage,
                            PreferWhatsApp: true), stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during daily float digest execution.");
            }

            // Run check every 4 hours
            await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
        }
    }
}
