namespace TinyHero.OperationsPortal.Models;

public sealed record PortalStatusResponse(
    ServiceStatus Jenkins,
    ServiceStatus ContentServer,
    ContentStatus Content,
    PortalDefaults Defaults);

public sealed record ServiceStatus(bool IsOnline, string Label, string Detail);

public sealed record ContentStatus(
    bool IsReady,
    string RootPath,
    int FileCount,
    long TotalBytes,
    DateTimeOffset? LastPublishedAtUtc,
    DeploymentRecord? LastDeployment);

public sealed record PortalDefaults(
    string JenkinsJobName,
    string JenkinsUrl,
    string ContentBaseUrl,
    string ContentStatePath,
    string PublishPath,
    string LocalContentRoot,
    string GameVersion,
    string BuildOutputPath);

public enum eBuildPlatform
{
    WINDOWS,
    ANDROID,
    IOS
}

public sealed record JenkinsPlayerBuildRequest(
    string? GameVersion,
    string? BuildOutputPath,
    bool RequireRemoteContent = false,
    eBuildPlatform Platform = eBuildPlatform.WINDOWS);

public sealed record JenkinsBuildRequest(
    string? ContentStatePath,
    bool RequireRemoteContent = false,
    eBuildPlatform Platform = eBuildPlatform.WINDOWS);

public sealed record JenkinsTriggerResult(bool IsTriggered, string Message, string? QueueUrl);

public sealed record JenkinsCredentialRequest(string? UserName, string? ApiToken);

public sealed record JenkinsCredentialStatus(bool IsConfigured, string? UserName);

public sealed record JenkinsCredential(string UserName, string ApiToken);

public sealed record JenkinsBuildHistoryItem(
    int BuildNumber,
    string GameVersion,
    string BuildMode,
    string State,
    DateTimeOffset? StartedAtUtc,
    long DurationMilliseconds,
    string? BuildUrl);

public sealed record JenkinsBuildStatus(
    bool IsAvailable,
    bool IsQueued,
    bool IsBuilding,
    int? BuildNumber,
    string State,
    string Detail,
    string? BuildUrl,
    int ProgressPercent,
    long ElapsedMilliseconds,
    long EstimatedDurationMilliseconds,
    DateTimeOffset? StartedAtUtc,
    IReadOnlyList<JenkinsBuildHistoryItem> RecentBuilds);

public sealed record DeploymentRecord(
    string Id,
    DateTimeOffset PublishedAtUtc,
    string PackageName,
    string ReleaseNote,
    string Sha256,
    int FileCount,
    long TotalBytes,
    string TargetPath,
    eBuildPlatform Platform = eBuildPlatform.WINDOWS);

public sealed record ContentPackageUploadResult(bool IsPublished, string Message, DeploymentRecord? Deployment);
