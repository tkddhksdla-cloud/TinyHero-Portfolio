pipeline {
    agent { label 'windows-unity' }

    options {
        timestamps()
        disableConcurrentBuilds()
    }

    parameters {
        string(name: 'UNITY_EXE', defaultValue: 'C:\\Program Files\\Unity\\Hub\\Editor\\6000.3.15f1\\Editor\\Unity.exe', description: 'Unity Editor executable path')
        string(name: 'BUILD_OUTPUT_PATH', defaultValue: 'Builds/Windows/TinyHero.exe', description: 'Windows player output path')
    }

    stages {
        stage('Custom Build') {
            steps {
                powershell """
                    \$env:UNITY_EXE = '${params.UNITY_EXE}'
                    ./Tools/CI/Invoke-TinyHeroCustomBuild.ps1 -BuildOutputPath '${params.BUILD_OUTPUT_PATH}'
                """
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: 'Logs/TinyHeroCustomBuild.log', allowEmptyArchive: true
            archiveArtifacts artifacts: 'Builds/Windows/**', allowEmptyArchive: true
        }
    }
}
