using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;
using TinyHero.OperationsPortal.Configuration;
using TinyHero.OperationsPortal.Models;
using TinyHero.OperationsPortal.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
long configuredMaximumUploadBytes = builder.Configuration.GetValue<long>($"{OperationsPortalOptions.SectionName}:MaximumUploadBytes");
long maximumUploadBytes = configuredMaximumUploadBytes > 0L ? configuredMaximumUploadBytes : 5L * 1024L * 1024L * 1024L;
builder.WebHost.ConfigureKestrel(_options => _options.Limits.MaxRequestBodySize = maximumUploadBytes);
builder.Services.Configure<OperationsPortalOptions>(builder.Configuration.GetSection(OperationsPortalOptions.SectionName));
string dataProtectionKeyPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath));
builder.Services.ConfigureHttpJsonOptions(_options =>
{
    _options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddSingleton<DeploymentHistoryService>();
builder.Services.AddSingleton<ContentPackageService>();
builder.Services.AddSingleton<OperationsStatusService>();
builder.Services.AddSingleton<JenkinsCredentialService>();
builder.Services.AddHttpClient<JenkinsService>(_client => _client.Timeout = TimeSpan.FromSeconds(5.0));
builder.Services.AddHttpClient("ContentServer", _client => _client.Timeout = TimeSpan.FromSeconds(3.0));
builder.Services.Configure<FormOptions>(_options =>
{
    _options.MultipartBodyLengthLimit = maximumUploadBytes;
});

WebApplication app = builder.Build();
OperationsPortalOptions runtimeOptions = app.Services.GetRequiredService<IOptions<OperationsPortalOptions>>().Value;
string localContentRootPath = Path.GetFullPath(runtimeOptions.LocalContentRoot);
Directory.CreateDirectory(localContentRootPath);
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", async (OperationsStatusService _statusService, CancellationToken _cancellationToken) =>
{
    PortalStatusResponse response = await _statusService.GetStatusAsync(_cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/api/deployments", async (DeploymentHistoryService _historyService, CancellationToken _cancellationToken) =>
{
    IReadOnlyList<DeploymentRecord> response = await _historyService.GetRecentAsync(_cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/api/jenkins/build-status", async (JenkinsService _jenkinsService, CancellationToken _cancellationToken) =>
{
    JenkinsBuildStatus response = await _jenkinsService.GetBuildStatusAsync(_cancellationToken);
    return Results.Ok(response);
});

app.MapGet("/api/jenkins/credentials", (JenkinsCredentialService _credentialService) =>
{
    JenkinsCredentialStatus response = _credentialService.GetStatus();
    return Results.Ok(response);
});

app.MapPost("/api/jenkins/credentials", async (
    JenkinsCredentialRequest _request,
    JenkinsCredentialService _credentialService,
    JenkinsService _jenkinsService,
    CancellationToken _cancellationToken) =>
{
    try
    {
        JenkinsCredentialStatus credentialStatus = _credentialService.Save(_request);
        ServiceStatus connectionStatus = await _jenkinsService.GetStatusAsync(_cancellationToken);

        if (connectionStatus.IsOnline == false)
        {
            _credentialService.Clear();
            return Results.BadRequest(new { message = "Jenkins 인증에 실패했습니다. 사용자 이름과 API 토큰 또는 비밀번호를 확인하세요." });
        }

        return Results.Ok(credentialStatus);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { message = exception.Message });
    }
});

app.MapDelete("/api/jenkins/credentials", (JenkinsCredentialService _credentialService) =>
{
    _credentialService.Clear();
    return Results.NoContent();
});

app.MapPost("/api/jenkins/player-build", async (
    JenkinsPlayerBuildRequest _request,
    JenkinsService _jenkinsService,
    CancellationToken _cancellationToken) =>
{
    JenkinsTriggerResult result = await _jenkinsService.TriggerPlayerBuildAsync(_request, _cancellationToken);
    return result.IsTriggered ? Results.Accepted(result.QueueUrl, result) : Results.BadRequest(result);
});

app.MapPost("/api/jenkins/content-update", async (
    JenkinsBuildRequest _request,
    JenkinsService _jenkinsService,
    CancellationToken _cancellationToken) =>
{
    JenkinsTriggerResult result = await _jenkinsService.TriggerContentUpdateAsync(_request, _cancellationToken);
    return result.IsTriggered ? Results.Accepted(result.QueueUrl, result) : Results.BadRequest(result);
});

app.MapPost("/api/jenkins/builds/{_buildNumber:int}/cancel", async (
    int _buildNumber,
    JenkinsService _jenkinsService,
    CancellationToken _cancellationToken) =>
{
    JenkinsCancelResult result = await _jenkinsService.CancelBuildAsync(_buildNumber, _cancellationToken);
    return result.IsCancelled ? Results.Accepted(value: result) : Results.BadRequest(result);
});

app.MapPost("/api/content/upload", async (
    HttpRequest _request,
    ContentPackageService _contentPackageService,
    CancellationToken _cancellationToken) =>
{
    if (_request.HasFormContentType == false)
    {
        return Results.BadRequest(new { message = "multipart/form-data 요청이 필요합니다." });
    }

    IFormCollection form = await _request.ReadFormAsync(_cancellationToken);
    IFormFile? packageFile = form.Files.GetFile("package");

    if (packageFile == null)
    {
        return Results.BadRequest(new { message = "업로드할 package 파일이 필요합니다." });
    }

    string? releaseNote = form["releaseNote"].FirstOrDefault();
    string? platformText = form["platform"].FirstOrDefault();
    eBuildPlatform platform = eBuildPlatform.WINDOWS;
    bool isPlatformValid = string.IsNullOrWhiteSpace(platformText) || Enum.TryParse(platformText, true, out platform);

    if (isPlatformValid == false)
    {
        return Results.BadRequest(new { message = "지원하지 않는 콘텐츠 플랫폼입니다." });
    }

    ContentPackageUploadResult result = await _contentPackageService.PublishAsync(packageFile, platform, releaseNote, _cancellationToken);
    return result.IsPublished ? Results.Ok(result) : Results.BadRequest(result);
}).DisableAntiforgery();

app.MapMethods("/TinyHeroContent/{**path}", new[] { "GET", "HEAD" }, ([FromRoute(Name = "path")] string? _path, HttpContext _context) =>
{
    if (string.IsNullOrWhiteSpace(_path))
    {
        return Results.NotFound();
    }

    string normalizedRootPath = localContentRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    string relativePath = _path.Replace('/', Path.DirectorySeparatorChar);
    string requestedFilePath = Path.GetFullPath(Path.Combine(localContentRootPath, relativePath));

    if (requestedFilePath.StartsWith(normalizedRootPath, StringComparison.OrdinalIgnoreCase) == false || File.Exists(requestedFilePath) == false)
    {
        return Results.NotFound();
    }

    string extension = Path.GetExtension(requestedFilePath);
    bool isCatalogMetadata = string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(extension, ".hash", StringComparison.OrdinalIgnoreCase);
    _context.Response.Headers.CacheControl = isCatalogMetadata
        ? "no-cache, no-store, must-revalidate"
        : "public, max-age=31536000, immutable";
    string contentType = string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
        ? "application/json; charset=utf-8"
        : "application/octet-stream";
    return Results.File(requestedFilePath, contentType, enableRangeProcessing: true);
});
app.MapFallbackToFile("index.html");
app.Run();
