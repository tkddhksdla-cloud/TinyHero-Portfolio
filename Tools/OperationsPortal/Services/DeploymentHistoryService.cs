using System.Text.Json;
using TinyHero.OperationsPortal.Models;

namespace TinyHero.OperationsPortal.Services;

public sealed class DeploymentHistoryService
{
    private const int MaximumHistoryCount = 30;
    private readonly string historyFilePath;
    private readonly SemaphoreSlim historyLock = new(1, 1);
    private readonly JsonSerializerOptions serializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public DeploymentHistoryService(IWebHostEnvironment _environment)
    {
        string dataDirectoryPath = Path.Combine(_environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectoryPath);
        historyFilePath = Path.Combine(dataDirectoryPath, "deployments.json");
    }

    public async Task<IReadOnlyList<DeploymentRecord>> GetRecentAsync(CancellationToken _cancellationToken)
    {
        await historyLock.WaitAsync(_cancellationToken);

        try
        {
            return await LoadUnsafeAsync(_cancellationToken);
        }
        finally
        {
            historyLock.Release();
        }
    }

    public async Task AddAsync(DeploymentRecord _record, CancellationToken _cancellationToken)
    {
        await historyLock.WaitAsync(_cancellationToken);

        try
        {
            IReadOnlyList<DeploymentRecord> currentHistory = await LoadUnsafeAsync(_cancellationToken);
            List<DeploymentRecord> nextHistory = new() { _record };
            nextHistory.AddRange(currentHistory.Where(_item => _item.Id != _record.Id));
            List<DeploymentRecord> trimmedHistory = nextHistory.Take(MaximumHistoryCount).ToList();
            string jsonText = JsonSerializer.Serialize(trimmedHistory, serializerOptions);
            await File.WriteAllTextAsync(historyFilePath, jsonText, _cancellationToken);
        }
        finally
        {
            historyLock.Release();
        }
    }

    private async Task<IReadOnlyList<DeploymentRecord>> LoadUnsafeAsync(CancellationToken _cancellationToken)
    {
        if (File.Exists(historyFilePath) == false)
        {
            return Array.Empty<DeploymentRecord>();
        }

        string jsonText = await File.ReadAllTextAsync(historyFilePath, _cancellationToken);
        List<DeploymentRecord>? history = JsonSerializer.Deserialize<List<DeploymentRecord>>(jsonText, serializerOptions);
        IReadOnlyList<DeploymentRecord> result = history != null ? history : Array.Empty<DeploymentRecord>();
        return result;
    }
}
