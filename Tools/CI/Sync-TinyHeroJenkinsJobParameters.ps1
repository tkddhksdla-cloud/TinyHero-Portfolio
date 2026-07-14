param(
    [string]$JenkinsHome = (Join-Path $env:USERPROFILE ".jenkins"),
    [string]$JobName = "TinyHero-Build-Windows"
)

$ErrorActionPreference = "Stop"
$jobConfigPath = Join-Path $JenkinsHome "jobs\$JobName\config.xml"

if ((Test-Path -LiteralPath $jobConfigPath -PathType Leaf) -eq $false) {
    Write-Warning "Jenkins job config was not found. Path: $jobConfigPath"
    return
}

$parameterDefinitions = @'
<parameterDefinitions>
  <hudson.model.ChoiceParameterDefinition>
    <name>BUILD_MODE</name>
    <description>Build a Windows player or update Addressables content for an existing player</description>
    <choices class="java.util.Arrays$ArrayList">
      <a class="string-array">
        <string>PLAYER_BUILD</string>
        <string>CONTENT_UPDATE</string>
      </a>
    </choices>
  </hudson.model.ChoiceParameterDefinition>
  <hudson.model.StringParameterDefinition>
    <name>UNITY_EXE</name>
    <description>Unity Editor executable path</description>
    <defaultValue>C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe</defaultValue>
    <trim>false</trim>
  </hudson.model.StringParameterDefinition>
  <hudson.model.StringParameterDefinition>
    <name>BUILD_OUTPUT_PATH</name>
    <description>Optional Windows player output path. Empty value uses Builds/Windows/&lt;BUILD_NUMBER&gt;/TinyHero.exe</description>
    <defaultValue></defaultValue>
    <trim>false</trim>
  </hudson.model.StringParameterDefinition>
  <hudson.model.StringParameterDefinition>
    <name>CONTENT_STATE_PATH</name>
    <description>Content state file belonging to the player release being updated</description>
    <defaultValue>Assets/AddressableAssetsData/Windows/addressables_content_state.bin</defaultValue>
    <trim>false</trim>
  </hudson.model.StringParameterDefinition>
  <hudson.model.StringParameterDefinition>
    <name>CONTENT_PUBLISH_PATH</name>
    <description>Workspace path used to stage Addressables server files</description>
    <defaultValue>PublishedContent</defaultValue>
    <trim>false</trim>
  </hudson.model.StringParameterDefinition>
  <hudson.model.StringParameterDefinition>
    <name>LOCAL_CONTENT_SERVER_PATH</name>
    <description>Local server TinyHeroContent root</description>
    <defaultValue>C:\TinyHeroLocalServer\TinyHeroContent</defaultValue>
    <trim>false</trim>
  </hudson.model.StringParameterDefinition>
  <hudson.model.StringParameterDefinition>
    <name>CONTENT_BASE_URL</name>
    <description>Addressables base URL written into a player build</description>
    <defaultValue>http://127.0.0.1:8082/TinyHeroContent</defaultValue>
    <trim>false</trim>
  </hudson.model.StringParameterDefinition>
  <hudson.model.BooleanParameterDefinition>
    <name>REQUIRE_REMOTE_CONTENT</name>
    <description>Block gameplay when remote content is unavailable</description>
    <defaultValue>false</defaultValue>
  </hudson.model.BooleanParameterDefinition>
</parameterDefinitions>
'@

$trackedParameters = @'
<parameters>
  <string>BUILD_MODE</string>
  <string>UNITY_EXE</string>
  <string>BUILD_OUTPUT_PATH</string>
  <string>CONTENT_STATE_PATH</string>
  <string>CONTENT_PUBLISH_PATH</string>
  <string>LOCAL_CONTENT_SERVER_PATH</string>
  <string>CONTENT_BASE_URL</string>
  <string>REQUIRE_REMOTE_CONTENT</string>
</parameters>
'@

$configText = Get-Content -LiteralPath $jobConfigPath -Raw
$parameterPattern = '(?s)<parameterDefinitions>.*?</parameterDefinitions>'
$trackerPattern = '(?s)(<org\.jenkinsci\.plugins\.pipeline\.modeldefinition\.actions\.DeclarativeJobPropertyTrackerAction[^>]*>.*?)(<parameters>.*?</parameters>)'

if ([regex]::IsMatch($configText, $parameterPattern) -eq $false) {
    throw "Jenkins parameterDefinitions section was not found. Path: $jobConfigPath"
}

if ([regex]::IsMatch($configText, $trackerPattern) -eq $false) {
    throw "Jenkins declarative parameter tracker was not found. Path: $jobConfigPath"
}

$updatedConfigText = [regex]::Replace($configText, $parameterPattern, $parameterDefinitions, 1)
$updatedConfigText = [regex]::Replace(
    $updatedConfigText,
    $trackerPattern,
    [System.Text.RegularExpressions.MatchEvaluator]{
        param($match)
        return $match.Groups[1].Value + $trackedParameters
    },
    1)

if ($updatedConfigText -eq $configText) {
    Write-Host "Jenkins job parameters are already synchronized. Job: $JobName"
    return
}

$backupPath = "$jobConfigPath.before-parameter-sync.bak"
Copy-Item -LiteralPath $jobConfigPath -Destination $backupPath -Force
Set-Content -LiteralPath $jobConfigPath -Value $updatedConfigText -Encoding UTF8
Write-Host "Jenkins job parameters synchronized. Job: $JobName"
