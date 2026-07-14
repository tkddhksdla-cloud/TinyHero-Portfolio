const state = {
  selectedFile: null,
  status: null
};

const elements = {
  refreshButton: document.querySelector('#refreshButton'),
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
}

function bindEvents() {
  elements.refreshButton.addEventListener('click', refreshAll);
  elements.jenkinsForm.addEventListener('submit', handleJenkinsSubmit);
  elements.uploadForm.addEventListener('submit', handleUploadSubmit);
  elements.dropzone.addEventListener('click', () => elements.packageInput.click());
  elements.packageInput.addEventListener('change', event => selectFile(event.target.files[0]));
  elements.removeFileButton.addEventListener('click', clearSelectedFile);
  elements.copyEndpointButton.addEventListener('click', copyEndpoint);

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
}

async function refreshAll() {
  elements.refreshButton.disabled = true;
  elements.refreshButton.textContent = '…';

  try {
    const [statusResponse, deploymentsResponse] = await Promise.all([
      fetch('/api/status'),
      fetch('/api/deployments')
    ]);

    if (!statusResponse.ok || !deploymentsResponse.ok) {
      throw new Error('운영 상태를 불러오지 못했습니다.');
    }

    state.status = await statusResponse.json();
    const deployments = await deploymentsResponse.json();
    renderStatus(state.status);
    renderDeployments(deployments);
  } catch (error) {
    showToast('상태 확인 실패', error.message, true);
  } finally {
    elements.refreshButton.disabled = false;
    elements.refreshButton.textContent = '↻';
    document.querySelector('#lastSyncText').textContent = `${new Date().toLocaleTimeString('ko-KR')} 갱신`;
  }
}

function renderStatus(status) {
  renderServiceStatus('jenkins', status.jenkins);
  renderServiceStatus('content', status.contentServer);
  document.querySelector('#fileCountValue').textContent = `${status.content.fileCount.toLocaleString('ko-KR')}개`;
  document.querySelector('#contentSizeValue').textContent = formatBytes(status.content.totalBytes);
  document.querySelector('#contentEndpoint').textContent = status.defaults.contentBaseUrl;
  document.querySelector('#contentStatePath').value = status.defaults.contentStatePath;

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
        requireRemoteContent: document.querySelector('#requireRemoteContent').checked
      })
    });
    const result = await response.json();

    if (!response.ok) throw new Error(result.message || 'Jenkins 작업 등록에 실패했습니다.');
    showToast('빌드 요청 완료', result.message, false);
  } catch (error) {
    showToast('빌드 요청 실패', error.message, true);
  } finally {
    setButtonBusy(submitButton, false, 'Jenkins 빌드 시작');
    refreshAll();
  }
}

async function handleUploadSubmit(event) {
  event.preventDefault();
  if (!state.selectedFile) return;

  const confirmed = await confirmAction(
    '콘텐츠 패키지 배포',
    '현재 Windows 콘텐츠가 백업된 뒤 새 패키지로 교체됩니다.'
  );

  if (!confirmed) return;

  const formData = new FormData();
  formData.append('package', state.selectedFile);
  formData.append('releaseNote', document.querySelector('#uploadReleaseNote').value);
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

function escapeHtml(value) {
  const element = document.createElement('div');
  element.textContent = value ?? '';
  return element.innerHTML;
}
