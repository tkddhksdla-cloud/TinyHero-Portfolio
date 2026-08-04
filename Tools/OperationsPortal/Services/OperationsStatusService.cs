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
        string windowsContentDirectoryName = ContentPackageService.GetAddressablesBuildTargetDirectoryName(eBuildPlatform.WINDOWS);
        string windowsContentPath = Path.Combine(contentRootPath, windowsContentDirectoryName);
        eBuildPlatform[] platformArray = { eBuildPlatform.WINDOWS, eBuildPlatform.ANDROID, eBuildPlatform.IOS };
        FileInfo[] contentFileArray = platformArray
            .SelectMany(_platform =>
            {
                string platformDirectoryName = ContentPackageService.GetAddressablesBuildTargetDirectoryName(_platform);
                string platformContentPath = Path.Combine(contentRootPath, platformDirectoryName);
                return Directory.Exists(platformContentPath)
                    ? new DirectoryInfo(platformContentPath).GetFiles("*", SearchOption.AllDirectories)
                    : Array.Empty<FileInfo>();
            })
            .ToArray();
        FileInfo? catalogHashFile = contentFileArray.FirstOrDefault(_file =>
            _file.Name.StartsWith("catalog", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(_file.Extension, ".hash", StringComparison.OrdinalIgnoreCase));
        bool isContentReady = catalogHashFile != null;
        bool isContentServerOnline = await CheckContentServerAsync(_cancellationToken);
        ServiceStatus contentServerStatus = new(
            isContentServerOnline,
            "콘텐츠 서버",
            isContentServerOnline
                ? isContentReady
                    ? $"정상 실행 중 · {contentFileArray.Length:N0}개 파일 제공"
                    : "정상 실행 중 · 아직 배포된 콘텐츠 없음"
                : "HTTP 서버에 연결할 수 없음");
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
            options.JenkinsBaseUrl,
            options.ContentBaseUrl,
            options.DefaultContentStatePath,
            options.DefaultPublishPath,
            contentRootPath,
            options.DefaultGameVersion,
            options.DefaultBuildOutputPath);
        return new PortalStatusResponse(jenkinsStatus, contentServerStatus, contentStatus, defaults);
    }

    /// <summary>
    /// 게임 콘텐츠 HTTP 서버가 응답 가능한지 확인한다.
    /// </summary>
    private async Task<bool> CheckContentServerAsync(CancellationToken _cancellationToken)
    {
        try
        {
            HttpClient httpClient = httpClientFactory.CreateClient("ContentServer");
            string contentUrl = options.ContentBaseUrl.TrimEnd('/');
            using HttpResponseMessage response = await httpClient.GetAsync(contentUrl, HttpCompletionOption.ResponseHeadersRead, _cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
