const state = {
  selectedFile: null,
  status: null,
  jenkinsConfigured: false
};

const contentStatePathByPlatform = {
  WINDOWS: 'Assets/AddressableAssetsData/Windows/addressables_content_state.bin',
  ANDROID: 'Assets/AddressableAssetsData/Android/addressables_content_state.bin',
  IOS: 'Assets/AddressableAssetsData/iOS/addressables_content_state.bin'
};

const elements = {
  refreshButton: document.querySelector('#refreshButton'),
  jenkinsCredentialButton: document.querySelector('#jenkinsCredentialButton'),
  jenkinsCredentialDialog: document.querySelector('#jenkinsCredentialDialog'),
  jenkinsCredentialForm: document.querySelector('#jenkinsCredentialForm'),
  jenkinsCredentialCancelButton: document.querySelector('#jenkinsCredentialCancelButton'),
  playerBuildForm: document.querySelector('#playerBuildForm'),
  jenkinsForm: document.querySelector('#jenkinsForm'),
  uploadForm: document.querySelector('#uploadForm'),
  packageInput: document.querySelector('#packageInput'),
  dropzone: document.querySelector('#dropzone'),
  uploadButton: document.querySelector('#uploadButton'),
  removeFileButton: document.querySelector('#removeFileButton'),
  copyEndpointButton: document.querySelector('#copyEndpointButton'),
  confirmDialog: document.querySelector('#confirmDialog')
};

document.addEventListener('DOMContentLoaded', initialize);

function initialize() {
  bindEvents();
  observeSections();
  refreshAll();
  window.setInterval(refreshBuildStatus, 3000);
}

function bindEvents() {
  elements.refreshButton.addEventListener('click', refreshAll);
  elements.jenkinsCredentialButton.addEventListener('click', openJenkinsCredentialDialog);
  elements.jenkinsCredentialForm.addEventListener('submit', handleJenkinsCredentialSubmit);
  elements.jenkinsCredentialCancelButton.addEventListener('click', () => elements.jenkinsCredentialDialog.close());
  elements.playerBuildForm.addEventListener('submit', handlePlayerBuildSubmit);
  document.querySelector('#playerBuildPlatform').addEventListener('change', updateAndroidArtifactTypeVisibility);
  document.querySelector('#contentBuildPlatform').addEventListener('change', updateContentStatePath);
  elements.jenkinsForm.addEventListener('submit', handleJenkinsSubmit);
  elements.uploadForm.addEventListener('submit', handleUploadSubmit);
  elements.dropzone.addEventListener('click', () => elements.packageInput.click());
  elements.packageInput.addEventListener('change', event => selectFile(event.target.files[0]));
  elements.removeFileButton.addEventListener('click', clearSelectedFile);
  elements.copyEndpointButton.addEventListener('click', copyEndpoint);
  document.querySelectorAll('[data-build-mode]').forEach(tab => {
    tab.addEventListener('click', () => switchBuildMode(tab.dataset.buildMode));
  });

  ['dragenter', 'dragover'].forEach(eventName => {
    elements.dropzone.addEventListener(eventName, event => {
      event.preventDefault();
      elements.dropzone.classList.add('dragover');
    });
  });

  ['dragleave', 'drop'].forEach(eventName => {
    elements.dropzone.addEventListener(eventName, event => {
      event.preventDefault();
      elements.dropzone.classList.remove('dragover');
    });
  });

  elements.dropzone.addEventListener('drop', event => selectFile(event.dataTransfer.files[0]));
  updateAndroidArtifactTypeVisibility();
  updateContentStatePath();
}

function updateAndroidArtifactTypeVisibility() {
  const platform = document.querySelector('#playerBuildPlatform').value;
  const artifactTypeField = document.querySelector('#androidArtifactTypeField');
  artifactTypeField.hidden = platform !== 'ANDROID' && platform !== 'ALL';
}

function updateContentStatePath() {
  const platform = document.querySelector('#contentBuildPlatform').value;
  const contentStatePath = contentStatePathByPlatform[platform];
  document.querySelector('#contentStatePath').value = contentStatePath;
}

async function refreshAll() {
  elements.refreshButton.disabled = true;
  elements.refreshButton.textContent = '…';

  try {
    const [statusResponse, deploymentsResponse, buildStatusResponse, credentialResponse] = await Promise.all([
      fetch('/api/status'),
      fetch('/api/deployments'),
      fetch('/api/jenkins/build-status'),
      fetch('/api/jenkins/credentials')
    ]);

    if (!statusResponse.ok || !deploymentsResponse.ok || !buildStatusResponse.ok || !credentialResponse.ok) {
      throw new Error('운영 상태를 불러오지 못했습니다.');
    }

    state.status = await statusResponse.json();
    const deployments = await deploymentsResponse.json();
    const buildStatus = await buildStatusResponse.json();
    const credentialStatus = await credentialResponse.json();
    renderStatus(state.status);
    renderDeployments(deployments);
    renderBuildStatus(buildStatus);
    renderCredentialStatus(credentialStatus);
  } catch (error) {
    showToast('상태 확인 실패', error.message, true);
  } finally {
    elements.refreshButton.disabled = false;
    elements.refreshButton.textContent = '↻';
    document.querySelector('#lastSyncText').textContent = `${new Date().toLocaleTimeString('ko-KR')} 갱신`;
  }
}

function renderCredentialStatus(credentialStatus) {
  state.jenkinsConfigured = credentialStatus.isConfigured;
  elements.jenkinsCredentialButton.textContent = credentialStatus.isConfigured
    ? `${credentialStatus.userName} 연결됨`
    : 'Jenkins 인증';
  elements.jenkinsCredentialButton.classList.toggle('connected', credentialStatus.isConfigured);
  document.querySelector('#jenkinsUserName').value = credentialStatus.userName || '';
  updateBuildAvailability();
}

function switchBuildMode(buildMode) {
  document.querySelectorAll('[data-build-mode]').forEach(tab => {
    const isActive = tab.dataset.buildMode === buildMode;
    tab.classList.toggle('active', isActive);
    tab.setAttribute('aria-selected', String(isActive));
  });
  document.querySelectorAll('[data-build-pane]').forEach(pane => {
    const isActive = pane.dataset.buildPane === buildMode;
    pane.classList.toggle('active', isActive);
    pane.hidden = !isActive;
  });
}

function updateBuildAvailability() {
  const isJenkinsOnline = state.status?.jenkins?.isOnline === true;
  const canTriggerBuild = state.jenkinsConfigured && isJenkinsOnline;
  [elements.playerBuildForm, elements.jenkinsForm].forEach(form => {
    const submitButton = form.querySelector('button[type="submit"]');
    submitButton.disabled = !canTriggerBuild;
    submitButton.title = canTriggerBuild ? '' : '먼저 Jenkins 인증을 연결하세요.';
  });
}

function openJenkinsCredentialDialog() {
  document.querySelector('#jenkinsApiToken').value = '';
  elements.jenkinsCredentialDialog.showModal();
}

async function handleJenkinsCredentialSubmit(event) {
  event.preventDefault();
  const submitButton = elements.jenkinsCredentialForm.querySelector('button[type="submit"]');
  setButtonBusy(submitButton, true, '인증 확인 중');

  try {
    const response = await fetch('/api/jenkins/credentials', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        userName: document.querySelector('#jenkinsUserName').value,
        apiToken: document.querySelector('#jenkinsApiToken').value
      })
    });
    const result = await response.json();

    if (!response.ok) throw new Error(result.message || 'Jenkins 인증에 실패했습니다.');
    elements.jenkinsCredentialDialog.close();
    showToast('Jenkins 연결 완료', `${result.userName} 계정으로 연결했습니다.`, false);
    await refreshAll();
  } catch (error) {
    showToast('Jenkins 인증 실패', error.message, true);
  } finally {
    setButtonBusy(submitButton, false, '인증 저장');
  }
}

function renderStatus(status) {
  renderServiceStatus('jenkins', status.jenkins);
  renderServiceStatus('content', status.contentServer);
  document.querySelector('#fileCountValue').textContent = `${status.content.fileCount.toLocaleString('ko-KR')}개`;
  document.querySelector('#contentSizeValue').textContent = formatBytes(status.content.totalBytes);
  document.querySelector('#contentEndpoint').textContent = status.defaults.contentBaseUrl;
  contentStatePathByPlatform.WINDOWS = status.defaults.contentStatePath;
  updateContentStatePath();
  document.querySelector('#gameVersion').value = status.defaults.gameVersion;
  document.querySelector('#buildOutputPath').value = status.defaults.buildOutputPath;
  document.querySelector('#buildStatusLink').href = status.defaults.jenkinsUrl;

  const lastDeployment = status.content.lastDeployment;
  document.querySelector('#lastDeploymentValue').textContent = lastDeployment
    ? formatRelativeTime(lastDeployment.publishedAtUtc)
    : '배포 기록 없음';
  document.querySelector('#lastDeploymentDetail').textContent = lastDeployment
    ? lastDeployment.packageName
    : '직접 배포 이력이 없습니다.';

  const isHealthy = status.jenkins.isOnline && status.contentServer.isOnline;
  const healthElement = document.querySelector('#overallHealth');
  healthElement.className = `health-pill ${isHealthy ? 'healthy' : 'degraded'}`;
  healthElement.innerHTML = `<span></span>${isHealthy ? '모든 시스템 정상' : '확인이 필요한 서비스 있음'}`;
  document.querySelector('#environmentDot').classList.toggle('online', status.contentServer.isOnline);
  updateBuildAvailability();
}

async function refreshBuildStatus() {
  try {
    const response = await fetch('/api/jenkins/build-status');
    if (!response.ok) return;
    renderBuildStatus(await response.json());
  } catch {
  }
}

function renderBuildStatus(buildStatus) {
  const card = document.querySelector('#buildStatusCard');
  const isActive = buildStatus.isQueued || buildStatus.isBuilding;
  const isSuccess = buildStatus.state === 'SUCCESS';
  const isFailure = ['FAILURE', 'ABORTED', 'UNSTABLE'].includes(buildStatus.state);
  const statusClass = buildStatus.isQueued ? 'queued' : buildStatus.isBuilding ? 'active' : isSuccess ? 'success' : isFailure ? 'failure' : '';
  const progressPercent = Math.max(0, Math.min(100, buildStatus.progressPercent || 0));
  card.className = `build-command-center ${statusClass}`;
  document.querySelector('#buildStatusTitle').textContent = !buildStatus.isAvailable && !state.jenkinsConfigured
    ? 'Jenkins 인증 필요'
    : isActive
    ? buildStatus.isQueued ? '빌드 대기 중' : `${formatBuildPlatform(buildStatus.buildPlatform)} · 빌드 #${buildStatus.buildNumber} 진행 중`
    : buildStatus.buildNumber ? `최근 빌드 #${buildStatus.buildNumber} · ${buildStatus.state}` : 'Jenkins 빌드 대기';
  document.querySelector('#buildStatusDetail').textContent = !buildStatus.isAvailable && !state.jenkinsConfigured
    ? 'Jenkins 계정을 한 번 연결하면 이후 자동으로 빌드를 제어할 수 있습니다.'
    : buildStatus.detail;
  document.querySelector('#buildStateBadge').textContent = resolveBuildStateLabel(buildStatus);
  document.querySelector('#buildProgressPercent').textContent = buildStatus.isQueued ? '대기 중' : `${progressPercent}%`;
  document.querySelector('#buildProgressTime').textContent = resolveBuildProgressTime(buildStatus);
  document.querySelector('#buildProgressBar').style.width = `${progressPercent}%`;
  document.querySelector('#buildProgressTrack').setAttribute('aria-valuenow', String(progressPercent));
  document.querySelector('#buildNumberValue').textContent = buildStatus.isQueued
    ? '대기열'
    : buildStatus.buildNumber ? `#${buildStatus.buildNumber}` : '—';
  document.querySelector('#buildPlatformValue').textContent = formatBuildPlatform(buildStatus.buildPlatform);
  document.querySelector('#buildStartedValue').textContent = buildStatus.startedAtUtc
    ? formatDateTime(buildStatus.startedAtUtc)
    : buildStatus.isQueued ? '대기열 등록' : '—';
  renderActiveBuilds(buildStatus.activeBuilds || []);
  renderBuildHistory(buildStatus.recentBuilds || []);

  if (buildStatus.buildUrl) {
    document.querySelector('#buildStatusLink').href = buildStatus.buildUrl;
  }
}

function renderActiveBuilds(buildList) {
  const activeBuildList = document.querySelector('#activeBuildList');

  if (!buildList.length) {
    activeBuildList.innerHTML = '<div class="active-build-empty">진행 또는 대기 중인 빌드가 없습니다.</div>';
    return;
  }

  activeBuildList.innerHTML = buildList.map(build => {
    const stateClass = String(build.state || '').toLowerCase();
    const buildLabel = build.buildNumber ? `#${build.buildNumber}` : 'QUEUE';
    const modeLabel = build.buildMode === 'PLAYER_BUILD' ? '플레이어' : build.buildMode === 'CONTENT_UPDATE' ? '콘텐츠' : '빌드';
    const progressPercent = Math.max(0, Math.min(100, build.progressPercent || 0));
    const progressLabel = build.state === 'QUEUED'
      ? '에이전트 할당 대기'
      : `${progressPercent}% · ${formatDuration(build.elapsedMilliseconds || 0)} 경과`;
    const tagName = build.buildUrl ? 'a' : 'div';
    const linkAttributes = build.buildUrl
      ? ` href="${escapeHtml(build.buildUrl)}" target="_blank" rel="noreferrer"`
      : '';
    return `<${tagName} class="active-build-item ${stateClass}"${linkAttributes}>
      <div class="active-build-top"><span>${formatBuildPlatform(build.buildPlatform)}</span><i>${escapeHtml(buildLabel)}</i></div>
      <strong>${escapeHtml(modeLabel)} · ${escapeHtml(build.state || 'UNKNOWN')}</strong>
      <small>${escapeHtml(progressLabel)}</small>
      <div class="active-build-track"><span style="width:${progressPercent}%"></span></div>
      <em>${escapeHtml(build.detail || '')}</em>
    </${tagName}>`;
  }).join('');
}

function renderBuildHistory(buildList) {
  const historyList = document.querySelector('#buildHistoryList');

  if (!buildList.length) {
    historyList.innerHTML = '<div class="build-history-empty">표시할 Jenkins 빌드 이력이 없습니다.</div>';
    return;
  }

  historyList.innerHTML = buildList.map(build => {
    const stateClass = resolveBuildHistoryStateClass(build.state);
    const modeLabel = build.buildMode === 'PLAYER_BUILD' ? '전체 빌드' : build.buildMode === 'CONTENT_UPDATE' ? '콘텐츠 업데이트' : '빌드';
    const platformLabel = formatBuildPlatform(build.buildPlatform);
    const versionLabel = build.gameVersion === '—' && build.buildMode === 'CONTENT_UPDATE' ? '기준 유지' : build.gameVersion;
    const startedLabel = build.startedAtUtc ? formatRelativeTime(build.startedAtUtc) : '시각 정보 없음';
    const tagName = build.buildUrl ? 'a' : 'div';
    const linkAttributes = build.buildUrl
      ? ` href="${escapeHtml(build.buildUrl)}" target="_blank" rel="noreferrer"`
      : '';
    return `<${tagName} class="build-history-item"${linkAttributes}>
      <div><span class="build-history-number">#${build.buildNumber}</span><i class="build-history-state ${stateClass}"></i></div>
      <strong class="build-history-version">${escapeHtml(versionLabel)}</strong>
      <small class="build-history-meta">${platformLabel} · ${modeLabel} · ${startedLabel}</small>
    </${tagName}>`;
  }).join('');
}

function formatBuildPlatform(platform) {
  const labelByPlatform = {
    WINDOWS: 'Windows',
    ANDROID: 'Android',
    IOS: 'iOS'
  };
  return labelByPlatform[platform] || '플랫폼 미확인';
}

function resolveBuildHistoryStateClass(buildState) {
  const normalizedState = String(buildState || '').toLowerCase();
  const supportedStateList = ['success', 'building', 'failure', 'aborted', 'unstable'];
  return supportedStateList.includes(normalizedState) ? normalizedState : '';
}

function resolveBuildStateLabel(buildStatus) {
  if (!buildStatus.isAvailable) return 'OFFLINE';
  if (buildStatus.isQueued) return 'QUEUED';
  if (buildStatus.isBuilding) return 'BUILDING';
  return buildStatus.state || 'IDLE';
}

function resolveBuildProgressTime(buildStatus) {
  if (buildStatus.isQueued) return 'Jenkins 실행 슬롯을 기다리고 있습니다.';
  if (buildStatus.isBuilding) {
    const elapsedText = formatDuration(buildStatus.elapsedMilliseconds);
    const estimateText = buildStatus.estimatedDurationMilliseconds > 0
      ? ` · 예상 ${formatDuration(buildStatus.estimatedDurationMilliseconds)}`
      : '';
    return `${elapsedText} 경과${estimateText}`;
  }
  if (buildStatus.progressPercent === 100 && buildStatus.elapsedMilliseconds > 0) {
    return `${formatDuration(buildStatus.elapsedMilliseconds)} 소요`;
  }
  return buildStatus.isAvailable ? '다음 빌드를 기다리고 있습니다.' : 'Jenkins 연결을 확인하세요.';
}

function renderServiceStatus(prefix, service) {
  const badge = document.querySelector(`#${prefix}Badge`);
  badge.textContent = service.isOnline ? 'ONLINE' : 'OFFLINE';
  badge.className = `status-badge ${service.isOnline ? 'online' : 'offline'}`;
  document.querySelector(`#${prefix}Value`).textContent = service.isOnline ? '정상 연결' : '연결 필요';
  document.querySelector(`#${prefix}Detail`).textContent = service.detail;
}

function renderDeployments(deployments) {
  const tableBody = document.querySelector('#deploymentTableBody');

  if (!deployments.length) {
    tableBody.innerHTML = '<tr class="empty-row"><td colspan="5">아직 직접 배포한 콘텐츠가 없습니다.</td></tr>';
    return;
  }

  tableBody.innerHTML = deployments.map(deployment => `
    <tr>
      <td>${formatDateTime(deployment.publishedAtUtc)}</td>
      <td class="package-cell"><strong>${escapeHtml(deployment.packageName)}</strong><small>${deployment.sha256.slice(0, 12)}…</small></td>
      <td>${escapeHtml(deployment.releaseNote)}</td>
      <td>${formatBytes(deployment.totalBytes)} · ${deployment.fileCount.toLocaleString('ko-KR')}개</td>
      <td><span class="verified">✓ VERIFIED</span></td>
    </tr>
  `).join('');
}

async function handlePlayerBuildSubmit(event) {
  event.preventDefault();
  const selectedPlatform = document.querySelector('#playerBuildPlatform').value;
  const platformArray = selectedPlatform === 'ALL'
    ? ['WINDOWS', 'ANDROID', 'IOS']
    : [selectedPlatform];
  const confirmed = await confirmAction(
    `${selectedPlatform} 플레이어 빌드 시작`,
    selectedPlatform === 'ALL'
      ? 'Windows, Android, iOS 전용 에이전트에 각각 빌드를 등록해 병렬로 실행합니다.'
      : '선택한 플랫폼의 새 게임 실행 파일과 Addressables 기준 상태를 생성합니다.'
  );

  if (!confirmed) return;

  const submitButton = elements.playerBuildForm.querySelector('button[type="submit"]');
  setButtonBusy(submitButton, true, 'Jenkins 요청 중');

  try {
    const requestBody = {
      gameVersion: document.querySelector('#gameVersion').value,
      buildOutputPath: document.querySelector('#buildOutputPath').value,
      requireRemoteContent: document.querySelector('#playerRequireRemoteContent').checked,
      androidArtifactType: document.querySelector('#androidArtifactType').value
    };
    const requestResultArray = await Promise.all(platformArray.map(async platform => {
      const response = await fetch('/api/jenkins/player-build', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ...requestBody, platform })
      });
      const result = await response.json();

      if (!response.ok) throw new Error(`${formatBuildPlatform(platform)}: ${result.message || '플레이어 빌드 등록에 실패했습니다.'}`);
      return result;
    }));
    const requestMessage = selectedPlatform === 'ALL'
      ? `${requestResultArray.length}개 플랫폼 빌드를 병렬 요청했습니다.`
      : requestResultArray[0].message;
    showToast('플레이어 빌드 요청 완료', requestMessage, false);
    await refreshBuildStatus();
  } catch (error) {
    showToast('빌드 요청 실패', error.message, true);
  } finally {
    setButtonBusy(submitButton, false, '전체 빌드 시작');
    updateBuildAvailability();
  }
}

async function handleJenkinsSubmit(event) {
  event.preventDefault();
  const confirmed = await confirmAction(
    '콘텐츠 업데이트 빌드 시작',
    '현재 저장소 상태를 기준으로 Jenkins CONTENT_UPDATE 작업을 실행합니다.'
  );

  if (!confirmed) return;

  const submitButton = elements.jenkinsForm.querySelector('button[type="submit"]');
  setButtonBusy(submitButton, true, 'Jenkins 요청 중');

  try {
    const response = await fetch('/api/jenkins/content-update', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        contentStatePath: document.querySelector('#contentStatePath').value,
        requireRemoteContent: document.querySelector('#requireRemoteContent').checked,
        platform: document.querySelector('#contentBuildPlatform').value
      })
    });
    const result = await response.json();

    if (!response.ok) throw new Error(result.message || 'Jenkins 작업 등록에 실패했습니다.');
    showToast('빌드 요청 완료', result.message, false);
  } catch (error) {
    showToast('빌드 요청 실패', error.message, true);
  } finally {
    setButtonBusy(submitButton, false, '콘텐츠 업데이트 시작');
    updateBuildAvailability();
    await refreshBuildStatus();
  }
}

async function handleUploadSubmit(event) {
  event.preventDefault();
  if (!state.selectedFile) return;

  const confirmed = await confirmAction(
    '콘텐츠 패키지 배포',
    `현재 ${document.querySelector('#uploadPlatform').value} 콘텐츠가 백업된 뒤 새 패키지로 교체됩니다.`
  );

  if (!confirmed) return;

  const formData = new FormData();
  formData.append('package', state.selectedFile);
  formData.append('releaseNote', document.querySelector('#uploadReleaseNote').value);
  formData.append('platform', document.querySelector('#uploadPlatform').value);
  setUploadBusy(true);

  try {
    const result = await uploadWithProgress('/api/content/upload', formData, updateUploadProgress);

    if (!result.ok) throw new Error(result.body.message || '콘텐츠 배포에 실패했습니다.');
    showToast('배포 완료', result.body.message, false);
    clearSelectedFile();
    document.querySelector('#uploadReleaseNote').value = '';
    await refreshAll();
  } catch (error) {
    showToast('배포 실패', error.message, true);
  } finally {
    setUploadBusy(false);
  }
}

function selectFile(file) {
  if (!file) return;

  if (!file.name.toLowerCase().endsWith('.zip')) {
    showToast('파일 형식 확인', 'ZIP 형식의 Addressables 패키지만 선택할 수 있습니다.', true);
    return;
  }

  state.selectedFile = file;
  document.querySelector('#fileName').textContent = file.name;
  document.querySelector('#fileSize').textContent = formatBytes(file.size);
  document.querySelector('#filePreview').classList.remove('hidden');
  document.querySelector('#dropzoneTitle').textContent = '다른 ZIP 파일 선택';
  elements.uploadButton.disabled = false;
}

function clearSelectedFile() {
  state.selectedFile = null;
  elements.packageInput.value = '';
  document.querySelector('#filePreview').classList.add('hidden');
  document.querySelector('#dropzoneTitle').textContent = 'ZIP 파일을 놓거나 선택하세요';
  elements.uploadButton.disabled = true;
}

function uploadWithProgress(url, formData, onProgress) {
  return new Promise((resolve, reject) => {
    const request = new XMLHttpRequest();
    request.open('POST', url);
    request.responseType = 'json';
    request.upload.addEventListener('progress', event => {
      if (event.lengthComputable) onProgress(event.loaded / event.total);
    });
    request.addEventListener('load', () => resolve({ ok: request.status >= 200 && request.status < 300, body: request.response || {} }));
    request.addEventListener('error', () => reject(new Error('서버와 통신할 수 없습니다.')));
    request.send(formData);
  });
}

function updateUploadProgress(progress) {
  const percent = Math.round(progress * 100);
  document.querySelector('#uploadPercent').textContent = `${percent}%`;
  document.querySelector('#uploadProgressBar').style.width = `${percent}%`;
}

function setUploadBusy(isBusy) {
  document.querySelector('#uploadProgress').classList.toggle('hidden', !isBusy);
  elements.uploadButton.disabled = isBusy || !state.selectedFile;
  elements.dropzone.disabled = isBusy;
  updateUploadProgress(isBusy ? 0 : 1);
  setButtonBusy(elements.uploadButton, isBusy, isBusy ? '검증 및 배포 중' : '로컬 서버에 배포');
}

function setButtonBusy(button, isBusy, label) {
  button.disabled = isBusy || (button === elements.uploadButton && !state.selectedFile);
  button.querySelector('span').textContent = label;
}

function confirmAction(title, message) {
  document.querySelector('#dialogTitle').textContent = title;
  document.querySelector('#dialogMessage').textContent = message;
  elements.confirmDialog.showModal();
  return new Promise(resolve => {
    elements.confirmDialog.addEventListener('close', () => resolve(elements.confirmDialog.returnValue === 'confirm'), { once: true });
  });
}

async function copyEndpoint() {
  const endpoint = document.querySelector('#contentEndpoint').textContent;
  await navigator.clipboard.writeText(endpoint);
  showToast('주소 복사 완료', endpoint, false);
}

function showToast(title, message, isError) {
  const toast = document.createElement('div');
  toast.className = `toast ${isError ? 'error' : ''}`;
  toast.innerHTML = `<span>${isError ? '!' : '✓'}</span><div><strong>${escapeHtml(title)}</strong><small>${escapeHtml(message)}</small></div>`;
  document.querySelector('#toastStack').appendChild(toast);
  window.setTimeout(() => toast.remove(), 5200);
}

function observeSections() {
  const navigationItems = [...document.querySelectorAll('.nav-item')];
  const observer = new IntersectionObserver(entries => {
    const visibleEntry = entries.filter(entry => entry.isIntersecting).sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];
    if (!visibleEntry) return;
    navigationItems.forEach(item => item.classList.toggle('active', item.dataset.section === visibleEntry.target.id));
  }, { rootMargin: '-25% 0px -60%', threshold: [0, .2, .5] });
  document.querySelectorAll('.section').forEach(section => observer.observe(section));
}

function formatBytes(bytes) {
  if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const unitIndex = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return `${(bytes / 1024 ** unitIndex).toFixed(unitIndex === 0 ? 0 : 2)} ${units[unitIndex]}`;
}

function formatDateTime(value) {
  return new Intl.DateTimeFormat('ko-KR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function formatRelativeTime(value) {
  const elapsedSeconds = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 1000));
  if (elapsedSeconds < 60) return '방금 전';
  if (elapsedSeconds < 3600) return `${Math.floor(elapsedSeconds / 60)}분 전`;
  if (elapsedSeconds < 86400) return `${Math.floor(elapsedSeconds / 3600)}시간 전`;
  return `${Math.floor(elapsedSeconds / 86400)}일 전`;
}

function formatDuration(milliseconds) {
  const totalSeconds = Math.max(0, Math.round((milliseconds || 0) / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes > 0 ? `${minutes}분 ${seconds}초` : `${seconds}초`;
}

function escapeHtml(value) {
  const element = document.createElement('div');
  element.textContent = value ?? '';
  return element.innerHTML;
}
