pipeline {
    agent none

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    parameters {
        choice(name: 'BUILD_MODE', choices: ['PLAYER_BUILD', 'CONTENT_UPDATE'], description: 'Build a player or update Addressables content for an existing player')
        choice(name: 'BUILD_PLATFORM', choices: ['WINDOWS', 'ANDROID', 'IOS'], description: 'Target platform. Android and iOS require their dedicated Jenkins agent.')
        choice(name: 'ANDROID_ARTIFACT_TYPE', choices: ['ALL', 'APK', 'AAB'], description: 'Android player artifact. ALL creates both APK for device testing and AAB for store distribution.')
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
        stage('Windows Player Build') {
            when {
                beforeAgent true
                expression { params.BUILD_MODE == 'PLAYER_BUILD' && params.BUILD_PLATFORM == 'WINDOWS' }
            }
            agent { label 'windows-unity' }
            steps {
                powershell """
                    \$env:UNITY_EXE = '${params.UNITY_EXE}'
                    \$buildOutputPath = '${params.BUILD_OUTPUT_PATH}'

                    if ([string]::IsNullOrWhiteSpace(\$buildOutputPath)) {
                        \$buildOutputPath = 'Builds/Windows/${env.BUILD_NUMBER}/TinyHero.exe'
                    }

                    ./Tools/CI/Invoke-TinyHeroCustomBuild.ps1 -BuildOutputPath \$buildOutputPath -GameVersion '${params.GAME_VERSION}'

                    \$resolvedBuildOutputPath = [System.IO.Path]::GetFullPath((Join-Path \$env:WORKSPACE \$buildOutputPath))
                    \$resolvedBuildOutputDirectory = Split-Path -Path \$resolvedBuildOutputPath -Parent
                    ./Tools/Addressables/Set-TinyHeroBuildContentEndpoint.ps1 -BuildPath \$resolvedBuildOutputDirectory -RemoteBaseUrl '${params.CONTENT_BASE_URL}' -RequireRemoteContent \$${params.REQUIRE_REMOTE_CONTENT}
                    ./Tools/Addressables/Publish-TinyHeroAddressablesContent.ps1 -PublishPath '${params.CONTENT_PUBLISH_PATH}' -LocalServerPath '${params.LOCAL_CONTENT_SERVER_PATH}'
                """
            }
        }

        stage('Windows Content Update') {
            when {
                beforeAgent true
                expression { params.BUILD_MODE == 'CONTENT_UPDATE' && params.BUILD_PLATFORM == 'WINDOWS' }
            }
            agent { label 'windows-unity' }
            steps {
                powershell """
                    \$env:UNITY_EXE = '${params.UNITY_EXE}'
                    ./Tools/Addressables/Invoke-TinyHeroContentUpdate.ps1 -ContentStatePath '${params.CONTENT_STATE_PATH}' -PublishPath '${params.CONTENT_PUBLISH_PATH}' -LocalServerPath '${params.LOCAL_CONTENT_SERVER_PATH}'
                """
            }
        }

        stage('Android Player Build') {
            when {
                beforeAgent true
                expression { params.BUILD_MODE == 'PLAYER_BUILD' && params.BUILD_PLATFORM == 'ANDROID' }
            }
            agent { label 'android-unity' }
            steps {
                bat '"%UNITY_EXE%" -batchmode -quit -projectPath "%WORKSPACE%" -executeMethod TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildAndroidPlayer -tinyHeroGameVersion %GAME_VERSION% -tinyHeroAndroidArtifactType %ANDROID_ARTIFACT_TYPE% -tinyHeroBuildOutputPath "%WORKSPACE%\\Builds\\Android\\%BUILD_NUMBER%\\TinyHero.aab" -logFile "%WORKSPACE%\\Builds\\Android\\%BUILD_NUMBER%\\Unity.log"'
            }
        }

        stage('Android Content Update') {
            when {
                beforeAgent true
                expression { params.BUILD_MODE == 'CONTENT_UPDATE' && params.BUILD_PLATFORM == 'ANDROID' }
            }
            agent { label 'android-unity' }
            steps {
                bat '"%UNITY_EXE%" -batchmode -quit -projectPath "%WORKSPACE%" -executeMethod TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildAndroidContentUpdate -tinyHeroContentStatePath "%CONTENT_STATE_PATH%" -logFile "%WORKSPACE%\\Builds\\Android\\%BUILD_NUMBER%\\ContentUpdate.log"'
            }
        }

        stage('iOS Player Build') {
            when {
                beforeAgent true
                expression { params.BUILD_MODE == 'PLAYER_BUILD' && params.BUILD_PLATFORM == 'IOS' }
            }
            agent { label 'ios-unity' }
            steps {
                sh '"$UNITY_EXE" -batchmode -quit -projectPath "$WORKSPACE" -executeMethod TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildIosPlayer -tinyHeroGameVersion "$GAME_VERSION" -tinyHeroBuildOutputPath "$WORKSPACE/Builds/iOS/$BUILD_NUMBER" -logFile "$WORKSPACE/Builds/iOS/$BUILD_NUMBER/Unity.log"'
            }
        }

        stage('iOS Content Update') {
            when {
                beforeAgent true
                expression { params.BUILD_MODE == 'CONTENT_UPDATE' && params.BUILD_PLATFORM == 'IOS' }
            }
            agent { label 'ios-unity' }
            steps {
                sh '"$UNITY_EXE" -batchmode -quit -projectPath "$WORKSPACE" -executeMethod TinyHero.Tools.CTinyHeroCustomBuildCommandLine.BuildIosContentUpdate -tinyHeroContentStatePath "$CONTENT_STATE_PATH" -logFile "$WORKSPACE/Builds/iOS/$BUILD_NUMBER/ContentUpdate.log"'
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'Logs/*.log', allowEmptyArchive: true
            archiveArtifacts artifacts: 'Builds/**', allowEmptyArchive: true
            archiveArtifacts artifacts: 'ServerData/**', allowEmptyArchive: true
            archiveArtifacts artifacts: 'PublishedContent/**', allowEmptyArchive: true
        }
    }
}
