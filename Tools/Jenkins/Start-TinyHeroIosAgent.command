#!/bin/zsh

set -u

readonly AGENT_NAME="TinyHero-iOS"
readonly CONTROLLER_URL="http://192.168.0.32:8081/"
readonly AGENT_DIRECTORY="$HOME/JenkinsAgent"
readonly AGENT_JAR_PATH="$AGENT_DIRECTORY/agent.jar"
readonly KEYCHAIN_SERVICE_NAME="TinyHeroJenkinsAgentSecret"

print ""
print "=============================================="
print " TinyHero iOS Jenkins Agent"
print "=============================================="

if ! command -v curl >/dev/null 2>&1; then
    print -u2 "curl을 찾을 수 없습니다. macOS 기본 curl 설치 상태를 확인하세요."
    exit 1
fi

java_home_path="$( /usr/libexec/java_home -v 21 2>/dev/null )"

if [[ -z "$java_home_path" || ! -x "$java_home_path/bin/java" ]]; then
    print -u2 "Java 21을 찾을 수 없습니다. java -version을 확인하세요."
    exit 1
fi

mkdir -p "$AGENT_DIRECTORY"

if [[ ! -f "$AGENT_JAR_PATH" ]]; then
    print "Jenkins agent.jar를 다운로드합니다."
    curl -fsSL "$CONTROLLER_URL"jnlpJars/agent.jar -o "$AGENT_JAR_PATH"
fi

agent_secret="$( /usr/bin/security find-generic-password -a "$AGENT_NAME" -s "$KEYCHAIN_SERVICE_NAME" -w 2>/dev/null )"

if [[ -z "$agent_secret" ]]; then
    print "Jenkins Agent secret이 아직 Keychain에 없습니다."
    print "Jenkins의 TinyHero-iOS 노드 Agent 연결 화면에서 secret을 확인해 입력하세요."
    read -r -s "agent_secret?Agent secret: "
    print ""

    if [[ -z "$agent_secret" ]]; then
        print -u2 "Secret이 비어 있습니다. 실행을 중단합니다."
        exit 1
    fi

    /usr/bin/security add-generic-password -U -a "$AGENT_NAME" -s "$KEYCHAIN_SERVICE_NAME" -w "$agent_secret"
fi

print "Jenkins 에이전트를 연결합니다. 이 창을 닫으면 에이전트가 Offline 상태가 됩니다."
exec "$java_home_path/bin/java" -jar "$AGENT_JAR_PATH" \
    -url "$CONTROLLER_URL" \
    -secret "$agent_secret" \
    -name "$AGENT_NAME" \
    -webSocket \
    -workDir "$AGENT_DIRECTORY"
