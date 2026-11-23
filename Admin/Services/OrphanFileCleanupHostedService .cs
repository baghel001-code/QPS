namespace Admin.Services
{

    public class OrphanFileCleanupHostedService : BackgroundService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<OrphanFileCleanupHostedService> _logger;
        private static readonly TimeSpan OrphanAge = TimeSpan.FromHours(2);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

        public OrphanFileCleanupHostedService(IWebHostEnvironment env, ILogger<OrphanFileCleanupHostedService> logger)
        {
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrphanFileCleanupHostedService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupStagingFolders();
                }
                catch (Exception ex)
                {
                    // Caught HERE, inside the loop — the service itself survives.
                    // Without this try/catch, an exception here would propagate out of
                    // ExecuteAsync and permanently kill the service for the app's lifetime.
                    _logger.LogError(ex, "Orphan file cleanup iteration failed. Will retry next cycle.");
                }

                try
                {
                    await Task.Delay(CheckInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected on app shutdown — exit the loop cleanly, don't log as an error
                    break;
                }
            }

            _logger.LogInformation("OrphanFileCleanupService stopping.");
        }


        private void CleanupStagingFolders()
        {
            var stagingRoot = Path.Combine(_env.WebRootPath, "uploads", "asn", "_staging");
            if (!Directory.Exists(stagingRoot)) return;

            var cutoff = DateTime.Now - OrphanAge;

            foreach (var file in Directory.GetFiles(stagingRoot, "*", SearchOption.AllDirectories))
            {
                if (File.GetCreationTimeUtc(file) < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation("Deleted orphaned staged file: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete orphaned file: {File}", file);
                    }
                }
            }
        }
    }
}
