pipeline {
    agent { label 'windows-unity' }

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    parameters {
        string(name: 'UNITY_EXE', defaultValue: 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.15f1\\Editor\\Unity.exe', description: 'Unity Editor executable path')
        string(name: 'BUILD_OUTPUT_PATH', defaultValue: '', description: 'Optional Windows player output path. Empty value uses Builds/Windows/<BUILD_NUMBER>/TinyHero.exe')
    }

    stages {
        stage('Custom Build') {
            steps {
                powershell """
                    \$env:UNITY_EXE = '${params.UNITY_EXE}'
                    \$buildOutputPath = '${params.BUILD_OUTPUT_PATH}'

                    if ([string]::IsNullOrWhiteSpace(\$buildOutputPath)) {
                        \$buildOutputPath = 'Builds/Windows/${env.BUILD_NUMBER}/TinyHero.exe'
                    }

                    ./Tools/CI/Invoke-TinyHeroCustomBuild.ps1 -BuildOutputPath \$buildOutputPath
                """
            }
        }

        stage('Expose Build Output') {
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
                    Write-Host "Build EXE: \$resolvedBuildOutputPath"
                    Write-Host "Build Folder: \$resolvedBuildOutputDirectory"
                    Write-Host '==========================================='
                    Write-Host ''

                    Set-Content -Path 'BuildOutputPath.txt' -Value @(
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
            archiveArtifacts artifacts: 'Logs/TinyHeroCustomBuild.log', allowEmptyArchive: true
            archiveArtifacts artifacts: 'Builds/Windows/**', allowEmptyArchive: true
            archiveArtifacts artifacts: 'BuildOutputPath.txt,Open-Build-Folder.ps1', allowEmptyArchive: true
        }
    }
}
