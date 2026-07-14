using Microsoft.Extensions.Options;
using TinyHero.OperationsPortal.Configuration;
using TinyHero.OperationsPortal.Models;

namespace TinyHero.OperationsPortal.Services;

public sealed class OperationsStatusService
{
    private readonly OperationsPortalOptions options;
    private readonly JenkinsService jenkinsService;
    private readonly DeploymentHistoryService deploymentHistoryService;
    private readonly IHttpClientFactory httpClientFactory;

    public OperationsStatusService(
        IOptions<OperationsPortalOptions> _options,
        JenkinsService _jenkinsService,
        DeploymentHistoryService _deploymentHistoryService,
        IHttpClientFactory _httpClientFactory)
    {
        options = _options.Value;
        jenkinsService = _jenkinsService;
        deploymentHistoryService = _deploymentHistoryService;
        httpClientFactory = _httpClientFactory;
    }

    public async Task<PortalStatusResponse> GetStatusAsync(CancellationToken _cancellationToken)
    {
        Task<ServiceStatus> jenkinsStatusTask = jenkinsService.GetStatusAsync(_cancellationToken);
        Task<IReadOnlyList<DeploymentRecord>> historyTask = deploymentHistoryService.GetRecentAsync(_cancellationToken);
        string contentRootPath = Path.GetFullPath(options.LocalContentRoot);
        string windowsContentPath = Path.Combine(contentRootPath, "Windows");
        FileInfo[] contentFileArray = Directory.Exists(windowsContentPath)
            ? new DirectoryInfo(windowsContentPath).GetFiles("*", SearchOption.AllDirectories)
            : Array.Empty<FileInfo>();
        FileInfo? catalogHashFile = contentFileArray.FirstOrDefault(_file =>
            _file.Name.StartsWith("catalog", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_file.Extension, ".hash", StringComparison.OrdinalIgnoreCase));
        bool isContentReady = catalogHashFile != null;
        bool isContentEndpointOnline = isContentReady && await CheckContentEndpointAsync(catalogHashFile!.Name, _cancellationToken);
        ServiceStatus contentServerStatus = new(
            isContentEndpointOnline,
            "콘텐츠 서버",
            isContentEndpointOnline
                ? $"{contentFileArray.Length:N0}개 파일 제공 중"
                : isContentReady
                    ? "파일은 있으나 HTTP 서버에 연결할 수 없음"
                    : "배포된 Windows 콘텐츠 없음");
        ServiceStatus jenkinsStatus = await jenkinsStatusTask;
        IReadOnlyList<DeploymentRecord> deploymentHistory = await historyTask;
        DeploymentRecord? lastDeployment = deploymentHistory.FirstOrDefault();
        ContentStatus contentStatus = new(
            isContentReady,
            windowsContentPath,
            contentFileArray.Length,
            contentFileArray.Sum(_file => _file.Length),
            lastDeployment?.PublishedAtUtc,
            lastDeployment);
        PortalDefaults defaults = new(
            options.JenkinsJobName,
            options.ContentBaseUrl,
            options.DefaultContentStatePath,
            options.DefaultPublishPath,
            contentRootPath);
        return new PortalStatusResponse(jenkinsStatus, contentServerStatus, contentStatus, defaults);
    }

    private async Task<bool> CheckContentEndpointAsync(string _catalogHashFileName, CancellationToken _cancellationToken)
    {
        try
        {
            HttpClient httpClient = httpClientFactory.CreateClient("ContentServer");
            string contentUrl = $"{options.ContentBaseUrl.TrimEnd('/')}/Windows/{Uri.EscapeDataString(_catalogHashFileName)}";
            using HttpResponseMessage response = await httpClient.GetAsync(contentUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
