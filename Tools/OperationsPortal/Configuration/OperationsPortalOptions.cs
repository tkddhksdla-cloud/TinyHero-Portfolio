namespace TinyHero.OperationsPortal.Configuration;

public sealed class OperationsPortalOptions
{
    public const string SectionName = "OperationsPortal";

    public string JenkinsBaseUrl { get; set; } = "http://127.0.0.1:8081";

    public string JenkinsJobName { get; set; } = "TinyHero-Build-Windows";

    public string ProjectRoot { get; set; } = "../..";

    public string LocalContentRoot { get; set; } = "C:/TinyHeroLocalServer/TinyHeroContent";

    public string ContentBaseUrl { get; set; } = "http://127.0.0.1:8082/TinyHeroContent";

    public string DefaultContentStatePath { get; set; } = "Assets/AddressableAssetsData/Windows/addressables_content_state.bin";

    public string DefaultPublishPath { get; set; } = "PublishedContent";

    public string DefaultGameVersion { get; set; } = "0.0.01";

    public string DefaultBuildOutputPath { get; set; } = string.Empty;

    public long MaximumUploadBytes { get; set; } = 5L * 1024L * 1024L * 1024L;
}
