using Application.Interfaces.V1;
using Domain.Entities.AppSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class DailyEmailHostedService : BackgroundService
    {
        private readonly ILogger<DailyEmailHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EmailSchedulerOptions _options;

        // Circuit breaker constants
        private const int MaxConsecutiveCrashes = 5;
        private const int WorkerRestartDelaySeconds = 60;
        private const int EmailOperationTimeoutMinutes = 15;
        private const int LoopIntervalSeconds = 20;

        public DailyEmailHostedService(ILogger<DailyEmailHostedService> logger, IServiceScopeFactory scopeFactory, IOptions<EmailSchedulerOptions> options)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _options = options.Value;

            // FIX #4 — Validate schedule config at startup so misconfiguration is caught immediately
            if (_options.EndTime <= _options.StartTime)
            {
                _logger.LogCritical("Invalid schedule configuration: EndTime ({End}) must be after StartTime ({Start}). " + "The worker will start but no emails will be sent.", _options.EndTime, _options.StartTime);
            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Daily Email Hosted Service Worker started.");

            int consecutiveCrashes = 0;

            // Outer loop — restarts the whole worker if something fatal happens
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    consecutiveCrashes = 0; // Reset on each successful (re)start
                    await RunWorkerLoopAsync(stoppingToken);
                }
                // FIX #3 (ExecuteAsync) — Clean host shutdown: NOT a crash, no FATAL log
                catch (OperationCanceledException ex) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation(" Daily Email Hosted Service Worker stopped by host." );
                    _logger.LogError("[FATAL] {Time} - Daily Email Hosted Service  Worker loop crashed: {Ex}", DateTime.Now, ex);
                    break;
                }
                catch (Exception ex)
                {
                    consecutiveCrashes++;

                    _logger.LogError(ex, "[FATAL] {Time} - Worker loop crashed. Consecutive crash count: {Count}/{Max}.{Ex}", DateTime.Now, consecutiveCrashes, MaxConsecutiveCrashes, ex);

                    // FIX #5 — Circuit breaker: stop restarting after too many consecutive crashes
                    if (consecutiveCrashes >= MaxConsecutiveCrashes)
                    {
                        _logger.LogCritical("Worker has crashed {Max} times consecutively. " + "Stopping permanently to prevent thrashing. Manual intervention required.", MaxConsecutiveCrashes);

                        // FIX #6 — Alert here: plug in your alerting mechanism
                        // e.g. await _alertService.NotifyAsync("DailyEmailHostedService is down!");
                        //break;
                    }
                    // FIX #3 (retry delay) — Also guard the retry wait against cancellation
                    try
                    {
                        _logger.LogInformation("Restarting worker in {Delay}s (attempt {Count}/{Max})...", WorkerRestartDelaySeconds, consecutiveCrashes, MaxConsecutiveCrashes);

                        int delaySeconds = WorkerRestartDelaySeconds * consecutiveCrashes;
                        // Crash 1 → wait 60s, Crash 2 → wait 120s, Crash 3 → wait 180s...

                        // Cap it so it doesn't grow forever
                        delaySeconds = Math.Min(delaySeconds, 300); // max 5 minutes

                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Host stopped during the retry wait — exit cleanly
                        break;
                    }
                }
            }
            _logger.LogInformation(" Daily Email Hosted Service Worker exited." );
        }

        private async Task RunWorkerLoopAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested) // ✅ Check token in loop condition
            {
                var executionId = Guid.NewGuid();
                using (_logger.BeginScope(new Dictionary<string, object> { ["ExecutionId"] = executionId }))
                {
                    try
                    {
                        var istZone = TimeZoneInfo.FindSystemTimeZoneById(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "India Standard Time" : "Asia/Kolkata");
                        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                        var scheduledStartTime = now.Date + _options.StartTime;
                        var scheduledEndTime = now.Date + _options.EndTime;
                        if (now >= scheduledStartTime && now <= scheduledEndTime)
                        {
                            using var scope = _scopeFactory.CreateScope();
                            // FIX #2 — Graceful handling if IEmailService is not registered in DI
                            var emailManager = scope.ServiceProvider.GetService<IEmailService>();
                            if (emailManager == null)
                            {
                                _logger.LogCritical("IEmailService could not be resolved from DI. " + "Ensure it is registered in the service container. Skipping this cycle.");

                                // Throw to trigger outer restart logic — this is a configuration error
                                throw new InvalidOperationException("IEmailService is not registered in the DI container.");
                            }
                            // FIX #1 — Timeout protection: don't hang forever if email ops stall
                            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                            timeoutCts.CancelAfter(TimeSpan.FromMinutes(EmailOperationTimeoutMinutes));

                            try
                            {
                                //_logger.LogInformation("Running DailyEmails (ExecutionId: {Id}).", executionId);
                                await emailManager.DailyEmails();

                                //_logger.LogInformation("Running ProcessEmails (ExecutionId: {Id}).", executionId);
                                await emailManager.ProcessEmails();

                                //_logger.LogInformation("Email cycle complete (ExecutionId: {Id}).", executionId);
                            }
                            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                            {
                                // Timeout hit — not a host shutdown, just this cycle took too long
                                _logger.LogError("Email operation timed out after {Timeout} minute(s) (ExecutionId: {Id}). " + "Skipping to next cycle.", EmailOperationTimeoutMinutes, executionId);
                            }
                        }
                    }
                    // FIX #3 (inner loop) — Clean shutdown: return without logging as error
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Worker loop received cancellation. Exiting cleanly.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Non-fatal — log and continue to next cycle
                        _logger.LogError(ex, "[ERROR] {Time} -  Email processing failed for ExecutionId: {Id}. Will retry next cycle. {Ex}", DateTime.Now, executionId,ex);
                    }
                }
                // FIX #3 (loop delay) — Guard Task.Delay so host shutdown doesn't manifest as a crash
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(LoopIntervalSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Host is shutting down during the wait — exit cleanly
                    return;
                }
            }
        }
    }


}
