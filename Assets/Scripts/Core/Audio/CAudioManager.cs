using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace TinyHero.Core
{
    ///<summary>
    /// 루프 SFX 재생 핸들
    ///</summary>
    public sealed class CAudioLoopSfxHandle
    {
        private AudioSource audioSource;
        private bool isStopped;

        ///<summary>
        /// 정지 여부 반환
        ///</summary>
        public bool IsStopped()
        {
            bool result = isStopped;
            return result;
        }

        ///<summary>
        /// 오디오 소스 연결
        ///</summary>
        internal void SetAudioSource( AudioSource _audioSource )
        {
            audioSource = _audioSource;

            if ( isStopped )
            {
                Stop();
            }
        }

        ///<summary>
        /// 루프 SFX 정지
        ///</summary>
        public void Stop()
        {
            isStopped = true;

            if ( audioSource == null )
            {
                return;
            }

            GameObject sourceObject = audioSource.gameObject;
            audioSource.Stop();
            audioSource.clip = null;
            Object.Destroy( sourceObject );
            audioSource = null;
        }
    }

    ///<summary>
    /// 전역 오디오 재생 관리 컴포넌트
    ///</summary>
    public sealed class CAudioManager : CSingleTon<CAudioManager>
    {
        private const string DefaultAudioMixerResourcePath = "Audio/Mixers/TinyHeroAudioMixer";
        private const string BgmClipResourceFolderPath = "Audio/BGM";
        private const string SfxClipResourceFolderPath = "Audio/SFX";
        private const string MasterMixerGroupName = "Master";
        private const string BgmMixerGroupName = "BGM";
        private const string SfxMixerGroupName = "SFX";
        private const string MasterVolumeParameterName = "MasterVolume";
        private const string BgmVolumeParameterName = "BGMVolume";
        private const string SfxVolumeParameterName = "SFXVolume";
        private const string BgmSourceObjectNamePrefix = "BGMSource_";
        private const string SfxSourceObjectName = "SFXSource";
        private const float DefaultFadeDuration = 0.75f;
        private const float DefaultVolume = 1.0f;
        private const float MinDecibel = -80.0f;
        private const float MaxDecibel = 0.0f;
        private const float VolumeEpsilon = 0.0001f;
        private const int BgmSourceCount = 2;

        [Header( "믹서" )]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup masterMixerGroup;
        [SerializeField] private AudioMixerGroup bgmMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;

        [Header( "설정값" )]
        [SerializeField] private float masterVolume = DefaultVolume;
        [SerializeField] private float bgmVolume = DefaultVolume;
        [SerializeField] private float sfxVolume = DefaultVolume;
        [SerializeField] private float defaultBgmFadeDuration = DefaultFadeDuration;

        private readonly AudioSource[] bgmSourceArray = new AudioSource[ BgmSourceCount ];
        private readonly Dictionary<string, AudioClip> cachedBgmClipDictionary = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip> cachedSfxClipDictionary = new Dictionary<string, AudioClip>();
        private AudioSource sfxSource;
        private Coroutine bgmFadeCoroutine;
        private string currentBgmClipName = string.Empty;
        private string requestedBgmClipName = string.Empty;
        private int activeBgmSourceIndex;

        ///<summary>
        /// 오디오 매니저 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            EnsureAudioSources();
            LoadDefaultAudioMixerIfNeeded();
            ResolveMixerGroupsIfNeeded();
            ApplyMixerGroups();
            ApplyAllVolumes();
        }

        ///<summary>
        /// 현재 BGM 클립 이름 반환
        ///</summary>
        public string GetCurrentBgmClipName()
        {
            string result = currentBgmClipName;
            return result;
        }

        ///<summary>
        /// BGM 재생 요청 처리
        ///</summary>
        public void PlayBgm( string _clipName )
        {
            PlayBgm( _clipName, defaultBgmFadeDuration );
        }

        ///<summary>
        /// BGM 페이드 재생 요청 처리
        ///</summary>
        public void PlayBgm( string _clipName, float _fadeDuration )
        {
            string normalizedClipName = NormalizeClipName( _clipName );

            if ( string.IsNullOrWhiteSpace( normalizedClipName ) )
            {
                StopBgm( _fadeDuration );
                return;
            }

            if ( IsSameBgmRequest( normalizedClipName ) )
            {
                return;
            }

            requestedBgmClipName = normalizedClipName;
            LoadAudioClipAsync( BgmClipResourceFolderPath, normalizedClipName, cachedBgmClipDictionary, delegate( AudioClip _loadedClip )
            {
                HandleBgmClipLoaded( normalizedClipName, _loadedClip, _fadeDuration );
            } );
        }

        ///<summary>
        /// BGM 정지 요청 처리
        ///</summary>
        public void StopBgm()
        {
            StopBgm( defaultBgmFadeDuration );
        }

        ///<summary>
        /// BGM 페이드 정지 요청 처리
        ///</summary>
        public void StopBgm( float _fadeDuration )
        {
            requestedBgmClipName = string.Empty;

            if ( bgmFadeCoroutine != null )
            {
                StopCoroutine( bgmFadeCoroutine );
                bgmFadeCoroutine = null;
            }

            bgmFadeCoroutine = StartCoroutine( IE_StopBgmWithFade( _fadeDuration ) );
        }

        ///<summary>
        /// SFX 재생 요청 처리
        ///</summary>
        public void PlaySfx( string _clipName )
        {
            PlaySfx( _clipName, DefaultVolume );
        }

        ///<summary>
        /// SFX 볼륨 지정 재생 요청 처리
        ///</summary>
        public void PlaySfx( string _clipName, float _volumeScale )
        {
            string normalizedClipName = NormalizeClipName( _clipName );

            if ( string.IsNullOrWhiteSpace( normalizedClipName ) )
            {
                return;
            }

            AudioClip immediateClip = LoadSfxClipImmediately( normalizedClipName );

            if ( immediateClip != null )
            {
                PlaySfx( immediateClip, _volumeScale );
                return;
            }

            LoadAudioClipAsync( SfxClipResourceFolderPath, normalizedClipName, cachedSfxClipDictionary, delegate( AudioClip _loadedClip )
            {
                PlaySfx( _loadedClip, _volumeScale );
            } );
        }

        ///<summary>
        /// SFX 클립 선로딩 요청 처리
        ///</summary>
        public void PreloadSfx( string _clipName )
        {
            string normalizedClipName = NormalizeClipName( _clipName );

            if ( string.IsNullOrWhiteSpace( normalizedClipName ) )
            {
                return;
            }

            AudioClip immediateClip = LoadSfxClipImmediately( normalizedClipName );

            if ( immediateClip != null )
            {
                return;
            }
        }

        ///<summary>
        /// SFX 클립 직접 재생 처리
        ///</summary>
        public void PlaySfx( AudioClip _audioClip )
        {
            PlaySfx( _audioClip, DefaultVolume );
        }

        ///<summary>
        /// SFX 클립 볼륨 지정 직접 재생 처리
        ///</summary>
        public void PlaySfx( AudioClip _audioClip, float _volumeScale )
        {
            if ( _audioClip == null )
            {
                return;
            }

            EnsureAudioSources();

            if ( sfxSource == null )
            {
                return;
            }

            float clampedVolumeScale = Mathf.Clamp01( _volumeScale );
            sfxSource.PlayOneShot( _audioClip, clampedVolumeScale );
        }

        ///<summary>
        /// 루프 SFX 재생 요청 처리
        ///</summary>
        public CAudioLoopSfxHandle PlayLoopSfx( string _clipName, Transform _parentTransform, float _volumeScale = DefaultVolume )
        {
            string normalizedClipName = NormalizeClipName( _clipName );

            if ( string.IsNullOrWhiteSpace( normalizedClipName ) )
            {
                return null;
            }

            CAudioLoopSfxHandle loopSfxHandle = new CAudioLoopSfxHandle();
            AudioClip immediateClip = LoadSfxClipImmediately( normalizedClipName );

            if ( immediateClip != null )
            {
                StartLoopSfx( loopSfxHandle, immediateClip, _parentTransform, _volumeScale );
                return loopSfxHandle;
            }

            LoadAudioClipAsync( SfxClipResourceFolderPath, normalizedClipName, cachedSfxClipDictionary, delegate( AudioClip _loadedClip )
            {
                StartLoopSfx( loopSfxHandle, _loadedClip, _parentTransform, _volumeScale );
            } );

            return loopSfxHandle;
        }

        ///<summary>
        /// SFX 클립 즉시 로드 반환
        ///</summary>
        private AudioClip LoadSfxClipImmediately( string _clipName )
        {
            if ( string.IsNullOrWhiteSpace( _clipName ) )
            {
                return null;
            }

            if ( cachedSfxClipDictionary.TryGetValue( _clipName, out AudioClip cachedClip ) && cachedClip != null )
            {
                return cachedClip;
            }

            string resourcePath = SfxClipResourceFolderPath + "/" + _clipName;
            AudioClip loadedClip = Resources.Load<AudioClip>( resourcePath );

            if ( loadedClip != null )
            {
                cachedSfxClipDictionary[ _clipName ] = loadedClip;
            }

            return loadedClip;
        }

        ///<summary>
        /// 루프 SFX 소스 생성 및 재생
        ///</summary>
        private void StartLoopSfx( CAudioLoopSfxHandle _loopSfxHandle, AudioClip _audioClip, Transform _parentTransform, float _volumeScale )
        {
            if ( _loopSfxHandle == null || _loopSfxHandle.IsStopped() || _audioClip == null )
            {
                return;
            }

            EnsureAudioSources();

            GameObject loopSourceObject = new GameObject( "LoopSFXSource_" + _audioClip.name );
            Transform parentTransform = _parentTransform != null ? _parentTransform : transform;
            loopSourceObject.transform.SetParent( parentTransform, false );

            AudioSource loopSource = loopSourceObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.clip = _audioClip;
            loopSource.volume = Mathf.Clamp01( _volumeScale );
            loopSource.outputAudioMixerGroup = sfxMixerGroup;
            _loopSfxHandle.SetAudioSource( loopSource );
            loopSource.Play();
        }

        ///<summary>
        /// 마스터 볼륨 설정
        ///</summary>
        public void SetMasterVolume( float _volume )
        {
            masterVolume = Mathf.Clamp01( _volume );
            ApplyVolumeToMixer( MasterVolumeParameterName, masterVolume );
        }

        ///<summary>
        /// BGM 볼륨 설정
        ///</summary>
        public void SetBgmVolume( float _volume )
        {
            bgmVolume = Mathf.Clamp01( _volume );
            ApplyVolumeToMixer( BgmVolumeParameterName, bgmVolume );
        }

        ///<summary>
        /// SFX 볼륨 설정
        ///</summary>
        public void SetSfxVolume( float _volume )
        {
            sfxVolume = Mathf.Clamp01( _volume );
            ApplyVolumeToMixer( SfxVolumeParameterName, sfxVolume );
        }

        ///<summary>
        /// BGM 일시정지 처리
        ///</summary>
        public void PauseBgm()
        {
            for ( int index = 0; index < bgmSourceArray.Length; index++ )
            {
                AudioSource bgmSource = bgmSourceArray[ index ];

                if ( bgmSource == null )
                {
                    continue;
                }

                bgmSource.Pause();
            }
        }

        ///<summary>
        /// BGM 재개 처리
        ///</summary>
        public void ResumeBgm()
        {
            for ( int index = 0; index < bgmSourceArray.Length; index++ )
            {
                AudioSource bgmSource = bgmSourceArray[ index ];

                if ( bgmSource == null )
                {
                    continue;
                }

                bgmSource.UnPause();
            }
        }

        ///<summary>
        /// 캐시된 오디오 클립 정리
        ///</summary>
        public void ClearAudioClipCache()
        {
            cachedBgmClipDictionary.Clear();
            cachedSfxClipDictionary.Clear();
        }

        ///<summary>
        /// 오디오 소스 존재 보장
        ///</summary>
        private void EnsureAudioSources()
        {
            for ( int index = 0; index < bgmSourceArray.Length; index++ )
            {
                if ( bgmSourceArray[ index ] != null )
                {
                    continue;
                }

                string sourceObjectName = BgmSourceObjectNamePrefix + index;
                AudioSource createdSource = CreateChildAudioSource( sourceObjectName, true );
                createdSource.playOnAwake = false;
                createdSource.loop = true;
                bgmSourceArray[ index ] = createdSource;
            }

            if ( sfxSource == null )
            {
                sfxSource = CreateChildAudioSource( SfxSourceObjectName, false );
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
        }

        ///<summary>
        /// 자식 오디오 소스 생성
        ///</summary>
        private AudioSource CreateChildAudioSource( string _objectName, bool _isBgmSource )
        {
            Transform existingTransform = transform.Find( _objectName );

            if ( existingTransform != null )
            {
                AudioSource existingSource = existingTransform.GetComponent<AudioSource>();

                if ( existingSource != null )
                {
                    return existingSource;
                }
            }

            GameObject sourceObject = new GameObject( _objectName );
            sourceObject.transform.SetParent( transform, false );
            AudioSource audioSource = sourceObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = _isBgmSource ? bgmMixerGroup : sfxMixerGroup;
            return audioSource;
        }

        ///<summary>
        /// 기본 오디오 믹서 로드
        ///</summary>
        private void LoadDefaultAudioMixerIfNeeded()
        {
            if ( audioMixer != null )
            {
                return;
            }

            audioMixer = Resources.Load<AudioMixer>( DefaultAudioMixerResourcePath );
        }

        ///<summary>
        /// 오디오 믹서 그룹 참조 결정
        ///</summary>
        private void ResolveMixerGroupsIfNeeded()
        {
            if ( audioMixer == null )
            {
                return;
            }

            if ( masterMixerGroup == null )
            {
                masterMixerGroup = ResolveMixerGroup( MasterMixerGroupName );
            }

            if ( bgmMixerGroup == null )
            {
                bgmMixerGroup = ResolveMixerGroup( BgmMixerGroupName );
            }

            if ( sfxMixerGroup == null )
            {
                sfxMixerGroup = ResolveMixerGroup( SfxMixerGroupName );
            }
        }

        ///<summary>
        /// 이름 기반 오디오 믹서 그룹 결정
        ///</summary>
        private AudioMixerGroup ResolveMixerGroup( string _groupName )
        {
            if ( audioMixer == null || string.IsNullOrWhiteSpace( _groupName ) )
            {
                return null;
            }

            AudioMixerGroup[] mixerGroupArray = audioMixer.FindMatchingGroups( _groupName );

            if ( mixerGroupArray == null || mixerGroupArray.Length == 0 )
            {
                return null;
            }

            AudioMixerGroup result = mixerGroupArray[ 0 ];
            return result;
        }

        ///<summary>
        /// 오디오 소스 믹서 그룹 적용
        ///</summary>
        private void ApplyMixerGroups()
        {
            for ( int index = 0; index < bgmSourceArray.Length; index++ )
            {
                AudioSource bgmSource = bgmSourceArray[ index ];

                if ( bgmSource == null )
                {
                    continue;
                }

                bgmSource.outputAudioMixerGroup = bgmMixerGroup;
            }

            if ( sfxSource != null )
            {
                sfxSource.outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        ///<summary>
        /// 전체 볼륨 설정 적용
        ///</summary>
        private void ApplyAllVolumes()
        {
            SetMasterVolume( masterVolume );
            SetBgmVolume( bgmVolume );
            SetSfxVolume( sfxVolume );
        }

        ///<summary>
        /// 믹서 볼륨 파라미터 적용
        ///</summary>
        private void ApplyVolumeToMixer( string _parameterName, float _volume )
        {
            if ( audioMixer == null || string.IsNullOrWhiteSpace( _parameterName ) )
            {
                return;
            }

            float decibel = ConvertVolumeToDecibel( _volume );
            audioMixer.SetFloat( _parameterName, decibel );
        }

        ///<summary>
        /// 볼륨 값을 데시벨 값으로 변환
        ///</summary>
        private float ConvertVolumeToDecibel( float _volume )
        {
            float clampedVolume = Mathf.Clamp01( _volume );

            if ( clampedVolume <= VolumeEpsilon )
            {
                return MinDecibel;
            }

            float result = Mathf.Clamp( Mathf.Log10( clampedVolume ) * 20.0f, MinDecibel, MaxDecibel );
            return result;
        }

        ///<summary>
        /// 오디오 클립 비동기 로드 요청
        ///</summary>
        private void LoadAudioClipAsync( string _resourceFolderPath, string _clipName, Dictionary<string, AudioClip> _cacheDictionary, System.Action<AudioClip> _onCompleted )
        {
            if ( string.IsNullOrWhiteSpace( _resourceFolderPath ) || string.IsNullOrWhiteSpace( _clipName ) )
            {
                InvokeAudioClipLoadedHandler( _onCompleted, null );
                return;
            }

            if ( _cacheDictionary.TryGetValue( _clipName, out AudioClip cachedClip ) && cachedClip != null )
            {
                InvokeAudioClipLoadedHandler( _onCompleted, cachedClip );
                return;
            }

            string resourcePath = _resourceFolderPath + "/" + _clipName;
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                AudioClip fallbackClip = Resources.Load<AudioClip>( resourcePath );
                CacheLoadedAudioClip( _clipName, fallbackClip, _cacheDictionary );
                InvokeAudioClipLoadedHandler( _onCompleted, fallbackClip );
                return;
            }

            resourceManager.LoadAssetAsync<AudioClip>( resourcePath, resourcePath, delegate( AudioClip _loadedClip )
            {
                CacheLoadedAudioClip( _clipName, _loadedClip, _cacheDictionary );
                InvokeAudioClipLoadedHandler( _onCompleted, _loadedClip );
            } );
        }

        ///<summary>
        /// 로드된 오디오 클립 캐시 처리
        ///</summary>
        private void CacheLoadedAudioClip( string _clipName, AudioClip _audioClip, Dictionary<string, AudioClip> _cacheDictionary )
        {
            if ( string.IsNullOrWhiteSpace( _clipName ) || _audioClip == null || _cacheDictionary == null )
            {
                return;
            }

            _cacheDictionary[ _clipName ] = _audioClip;
        }

        ///<summary>
        /// 오디오 클립 로드 콜백 호출
        ///</summary>
        private void InvokeAudioClipLoadedHandler( System.Action<AudioClip> _onCompleted, AudioClip _audioClip )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _audioClip );
        }

        ///<summary>
        /// BGM 클립 로드 완료 처리
        ///</summary>
        private void HandleBgmClipLoaded( string _clipName, AudioClip _loadedClip, float _fadeDuration )
        {
            if ( string.Equals( requestedBgmClipName, _clipName, System.StringComparison.Ordinal ) == false )
            {
                return;
            }

            if ( _loadedClip == null )
            {
                Debug.LogWarning( $"[ AudioManager ] BGM clip load failed: {_clipName}" );
                requestedBgmClipName = string.Empty;
                return;
            }

            if ( bgmFadeCoroutine != null )
            {
                StopCoroutine( bgmFadeCoroutine );
                bgmFadeCoroutine = null;
            }

            bgmFadeCoroutine = StartCoroutine( IE_ChangeBgmWithFade( _clipName, _loadedClip, _fadeDuration ) );
        }

        ///<summary>
        /// 동일 BGM 요청 여부 확인
        ///</summary>
        private bool IsSameBgmRequest( string _clipName )
        {
            bool isCurrentSame = string.Equals( currentBgmClipName, _clipName, System.StringComparison.Ordinal );
            bool isRequestedSame = string.Equals( requestedBgmClipName, _clipName, System.StringComparison.Ordinal );
            bool result = isCurrentSame || isRequestedSame;
            return result;
        }

        ///<summary>
        /// BGM 페이드 전환 코루틴 처리
        ///</summary>
        private IEnumerator IE_ChangeBgmWithFade( string _clipName, AudioClip _audioClip, float _fadeDuration )
        {
            EnsureAudioSources();
            int previousSourceIndex = activeBgmSourceIndex;
            int nextSourceIndex = ResolveNextBgmSourceIndex();
            AudioSource previousSource = bgmSourceArray[ previousSourceIndex ];
            AudioSource nextSource = bgmSourceArray[ nextSourceIndex ];

            if ( nextSource == null )
            {
                yield break;
            }

            activeBgmSourceIndex = nextSourceIndex;
            currentBgmClipName = _clipName;
            requestedBgmClipName = string.Empty;
            nextSource.clip = _audioClip;
            nextSource.volume = 0.0f;
            nextSource.Play();

            float duration = Mathf.Max( 0.0f, _fadeDuration );

            if ( duration <= 0.0f )
            {
                StopBgmSourceImmediately( previousSource );
                nextSource.volume = DefaultVolume;
                bgmFadeCoroutine = null;
                yield break;
            }

            float elapsedTime = 0.0f;
            float previousStartVolume = previousSource != null ? previousSource.volume : 0.0f;

            while ( elapsedTime < duration )
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01( elapsedTime / duration );

                if ( previousSource != null )
                {
                    previousSource.volume = Mathf.Lerp( previousStartVolume, 0.0f, normalizedTime );
                }

                nextSource.volume = Mathf.Lerp( 0.0f, DefaultVolume, normalizedTime );
                yield return null;
            }

            StopBgmSourceImmediately( previousSource );
            nextSource.volume = DefaultVolume;
            bgmFadeCoroutine = null;
        }

        ///<summary>
        /// BGM 페이드 정지 코루틴 처리
        ///</summary>
        private IEnumerator IE_StopBgmWithFade( float _fadeDuration )
        {
            float duration = Mathf.Max( 0.0f, _fadeDuration );
            AudioSource activeSource = bgmSourceArray[ activeBgmSourceIndex ];

            if ( activeSource == null )
            {
                currentBgmClipName = string.Empty;
                bgmFadeCoroutine = null;
                yield break;
            }

            if ( duration <= 0.0f )
            {
                StopBgmSourceImmediately( activeSource );
                currentBgmClipName = string.Empty;
                bgmFadeCoroutine = null;
                yield break;
            }

            float elapsedTime = 0.0f;
            float startVolume = activeSource.volume;

            while ( elapsedTime < duration )
            {
                elapsedTime += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01( elapsedTime / duration );
                activeSource.volume = Mathf.Lerp( startVolume, 0.0f, normalizedTime );
                yield return null;
            }

            StopBgmSourceImmediately( activeSource );
            currentBgmClipName = string.Empty;
            bgmFadeCoroutine = null;
        }

        ///<summary>
        /// 다음 BGM 소스 인덱스 반환
        ///</summary>
        private int ResolveNextBgmSourceIndex()
        {
            int result = activeBgmSourceIndex == 0 ? 1 : 0;
            return result;
        }

        ///<summary>
        /// BGM 소스 즉시 정지 처리
        ///</summary>
        private void StopBgmSourceImmediately( AudioSource _audioSource )
        {
            if ( _audioSource == null )
            {
                return;
            }

            _audioSource.Stop();
            _audioSource.clip = null;
            _audioSource.volume = DefaultVolume;
        }

        ///<summary>
        /// 클립 이름 정규화
        ///</summary>
        private string NormalizeClipName( string _clipName )
        {
            string result = string.IsNullOrWhiteSpace( _clipName ) ? string.Empty : _clipName.Trim();
            return result;
        }

        ///<summary>
        /// 인스턴스 참조 정리
        ///</summary>
        protected override void OnDestroy()
        {
            if ( bgmFadeCoroutine != null )
            {
                StopCoroutine( bgmFadeCoroutine );
                bgmFadeCoroutine = null;
            }

            base.OnDestroy();
        }
    }
}
