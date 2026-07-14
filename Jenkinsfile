pipeline {
    agent { label 'windows-unity' }

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    parameters {
        choice(name: 'BUILD_MODE', choices: ['PLAYER_BUILD', 'CONTENT_UPDATE'], description: 'Build a Windows player or update Addressables content for an existing player')
        string(name: 'UNITY_EXE', defaultValue: 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.15f1\\Editor\\Unity.exe', description: 'Unity Editor executable path')
        string(name: 'GAME_VERSION', defaultValue: '0.0.01', description: 'Player build version displayed in the title scene. Format: 0.0.01')
        string(name: 'BUILD_OUTPUT_PATH', defaultValue: '', description: 'Optional Windows player output path. Empty value uses Builds/Windows/<BUILD_NUMBER>/TinyHero.exe')
        string(name: 'CONTENT_STATE_PATH', defaultValue: 'Assets/AddressableAssetsData/Windows/addressables_content_state.bin', description: 'Content state file belonging to the player release being updated')
        string(name: 'CONTENT_PUBLISH_PATH', defaultValue: 'PublishedContent', description: 'Workspace path used to stage Addressables server files')
        string(name: 'LOCAL_CONTENT_SERVER_PATH', defaultValue: '', description: 'Optional local server TinyHeroContent root. Empty value skips direct deployment')
        string(name: 'CONTENT_BASE_URL', defaultValue: 'http://127.0.0.1:8082/TinyHeroContent', description: 'Addressables base URL written into a player build')
        booleanParam(name: 'REQUIRE_REMOTE_CONTENT', defaultValue: false, description: 'Block gameplay when remote content is unavailable')
    }

    stages {
        stage('Player Build') {
            when {
                expression { params.BUILD_MODE == 'PLAYER_BUILD' }
            }
            steps {
                powershell """
                    \$env:UNITY_EXE = '${params.UNITY_EXE}'
                    \$buildOutputPath = '${params.BUILD_OUTPUT_PATH}'

                    Write-Host '========== TinyHero Player Build =========='
                    Write-Host "Build Number: ${env.BUILD_NUMBER}"
                    Write-Host "Game Version: ${params.GAME_VERSION}"
                    Write-Host "Remote Content Required: ${params.REQUIRE_REMOTE_CONTENT}"
                    Write-Host '==========================================='

                    if ([string]::IsNullOrWhiteSpace(\$buildOutputPath)) {
                        \$buildOutputPath = 'Builds/Windows/${env.BUILD_NUMBER}/TinyHero.exe'
                    }

                    ./Tools/CI/Invoke-TinyHeroCustomBuild.ps1 `
                        -BuildOutputPath \$buildOutputPath `
                        -GameVersion '${params.GAME_VERSION}'

                    Write-Host '[ Pipeline ] Unity player build completed. Configuring runtime content endpoint.'

                    \$resolvedBuildOutputPath = [System.IO.Path]::GetFullPath((Join-Path \$env:WORKSPACE \$buildOutputPath))
                    \$resolvedBuildOutputDirectory = Split-Path -Path \$resolvedBuildOutputPath -Parent
                    ./Tools/Addressables/Set-TinyHeroBuildContentEndpoint.ps1 `
                        -BuildPath \$resolvedBuildOutputDirectory `
                        -RemoteBaseUrl '${params.CONTENT_BASE_URL}' `
                        -RequireRemoteContent \$${params.REQUIRE_REMOTE_CONTENT}

                    Write-Host '[ Pipeline ] Content endpoint configured. Publishing Addressables content.'

                    ./Tools/Addressables/Publish-TinyHeroAddressablesContent.ps1 `
                        -PublishPath '${params.CONTENT_PUBLISH_PATH}' `
                        -LocalServerPath '${params.LOCAL_CONTENT_SERVER_PATH}'
                """
            }
        }

        stage('Content Update') {
            when {
                expression { params.BUILD_MODE == 'CONTENT_UPDATE' }
            }
            steps {
                powershell """
                    \$env:UNITY_EXE = '${params.UNITY_EXE}'
                    Write-Host '======= TinyHero Content Update ======='
                    Write-Host "Build Number: ${env.BUILD_NUMBER}"
                    Write-Host "Content State: ${params.CONTENT_STATE_PATH}"
                    Write-Host "Remote Content Required: ${params.REQUIRE_REMOTE_CONTENT}"
                    Write-Host '======================================='
                    ./Tools/Addressables/Invoke-TinyHeroContentUpdate.ps1 `
                        -ContentStatePath '${params.CONTENT_STATE_PATH}' `
                        -PublishPath '${params.CONTENT_PUBLISH_PATH}' `
                        -LocalServerPath '${params.LOCAL_CONTENT_SERVER_PATH}'
                """
            }
        }

        stage('Expose Build Output') {
            when {
                expression { params.BUILD_MODE == 'PLAYER_BUILD' }
            }
            steps {
                powershell """
                    \$buildOutputPath = '${params.BUILD_OUTPUT_PATH}'

                    if ([string]::IsNullOrWhiteSpace(\$buildOutputPath)) {
                        \$buildOutputPath = 'Builds/Windows/${env.BUILD_NUMBER}/TinyHero.exe'
                    }

                    \$resolvedBuildOutputPath = [System.IO.Path]::GetFullPath((Join-Path \$env:WORKSPACE \$buildOutputPath))
                    \$resolvedBuildOutputDirectory = Split-Path -Path \$resolvedBuildOutputPath -Parent

                    Write-Host ''
                    Write-Host '========== TinyHero Build Output =========='
                    Write-Host "Game Version: ${params.GAME_VERSION}"
                    Write-Host "Build EXE: \$resolvedBuildOutputPath"
                    Write-Host "Build Folder: \$resolvedBuildOutputDirectory"
                    Write-Host '==========================================='
                    Write-Host ''

                    Set-Content -Path 'BuildOutputPath.txt' -Value @(
                        "Game Version: ${params.GAME_VERSION}",
                        "Build EXE: \$resolvedBuildOutputPath",
                        "Build Folder: \$resolvedBuildOutputDirectory"
                    ) -Encoding UTF8

                    Set-Content -Path 'Open-Build-Folder.ps1' -Value @(
                        '\$buildFolder = "' + \$resolvedBuildOutputDirectory.Replace('"', '`"') + '"',
                        'Start-Process -FilePath \$buildFolder'
                    ) -Encoding UTF8
                """
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'Logs/*.log', allowEmptyArchive: true
            archiveArtifacts artifacts: 'Builds/Windows/**', allowEmptyArchive: true
            archiveArtifacts artifacts: 'ServerData/**', allowEmptyArchive: true
            archiveArtifacts artifacts: 'PublishedContent/**', allowEmptyArchive: true
            archiveArtifacts artifacts: 'BuildOutputPath.txt,Open-Build-Folder.ps1', allowEmptyArchive: true
        }
    }
}
