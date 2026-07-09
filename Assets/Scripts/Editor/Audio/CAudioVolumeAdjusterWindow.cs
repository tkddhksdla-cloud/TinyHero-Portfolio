using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools.Editor
{
    ///<summary>
    /// 오디오 볼륨 조절 대상 정보
    ///</summary>
    [Serializable]
    public sealed class CAudioVolumeAdjusterItem
    {
        public string assetPath = string.Empty;
        public string displayName = string.Empty;
        public string category = string.Empty;
        public bool isSupportedWav;
        public float peakDb;
        public float rmsDb;
        public float durationSeconds;
        public int sampleRate;
        public int channelCount;
        public int bitsPerSample;
    }

    ///<summary>
    /// 원본 오디오 볼륨 조절 에디터 윈도우
    ///</summary>
    public sealed class CAudioVolumeAdjusterWindow : EditorWindow
    {
        private const string MenuPath = "Tools/TinyHero/Audio/Audio Volume Adjuster";
        private const string BgmRootPath = "Assets/Resources/Audio/BGM";
        private const string SfxRootPath = "Assets/Resources/Audio/SFX";
        private const string BackupRootRelativePath = "Temp/AudioVolumeBackups";
        private const string TemporaryPreviewFolderPath = "Assets/_AudioPreviewTemp";
        private const string TemporaryPreviewAssetPath = "Assets/_AudioPreviewTemp/AudioVolumePreview.wav";
        private const float ListWidth = 360.0f;
        private const float ListItemHeight = 48.0f;
        private const float MinGainDb = -24.0f;
        private const float MaxGainDb = 24.0f;
        private const float DefaultTargetPeakDb = -1.0f;
        private const float SilenceDb = -80.0f;
        private const int RiffHeaderSize = 12;
        private const int ChunkHeaderSize = 8;
        private const ushort WaveFormatPcm = 1;
        private const ushort WaveFormatFloat = 3;

        [SerializeField] private List<CAudioVolumeAdjusterItem> audioItemList = new List<CAudioVolumeAdjusterItem>();
        [SerializeField] private string searchText = string.Empty;
        [SerializeField] private int selectedItemIndex = -1;
        [SerializeField] private float previewGainDb;
        [SerializeField] private float targetPeakDb = DefaultTargetPeakDb;
        [SerializeField] private bool createBackup = true;

        private Vector2 listScrollPosition;
        private Vector2 detailScrollPosition;
        private string statusMessage = "Refresh 버튼으로 오디오 파일을 검색하세요.";
        private MessageType statusMessageType = MessageType.Info;
        private AudioClip currentPreviewClip;
        private AudioClip currentOriginalPreviewClip;

        ///<summary>
        /// 오디오 볼륨 조절 창 표시
        ///</summary>
        [MenuItem( MenuPath )]
        private static void ShowWindow()
        {
            CAudioVolumeAdjusterWindow window = GetWindow<CAudioVolumeAdjusterWindow>();
            window.titleContent = new GUIContent( "Audio Volume Adjuster" );
            window.minSize = new Vector2( 980.0f, 640.0f );
            window.Show();
        }

        ///<summary>
        /// 에디터 윈도우 초기화
        ///</summary>
        private void OnEnable()
        {
            RefreshAudioItemList();
        }

        ///<summary>
        /// 에디터 윈도우 비활성화 정리
        ///</summary>
        private void OnDisable()
        {
            StopPreview();
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Audio Volume Adjuster", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "BGM/SFX 원본 WAV 파일을 들어보고 gain을 적용합니다. Apply는 파일 샘플 데이터를 직접 수정하므로 기본 백업을 유지하세요.", MessageType.None );
            DrawToolbarSection();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            DrawAudioListSection();
            DrawDetailSection();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
        }

        ///<summary>
        /// 상단 도구 영역 렌더링
        ///</summary>
        private void DrawToolbarSection()
        {
            EditorGUILayout.BeginHorizontal();
            searchText = EditorGUILayout.TextField( "Search", searchText );

            if ( GUILayout.Button( "Refresh", GUILayout.Width( 100.0f ) ) )
            {
                RefreshAudioItemList();
            }

            if ( GUILayout.Button( "Stop", GUILayout.Width( 80.0f ) ) )
            {
                StopPreview();
            }

            EditorGUILayout.EndHorizontal();
        }

        ///<summary>
        /// 오디오 목록 영역 렌더링
        ///</summary>
        private void DrawAudioListSection()
        {
            EditorGUILayout.BeginVertical( GUILayout.Width( ListWidth ) );
            EditorGUILayout.LabelField( "Audio Files", EditorStyles.boldLabel );
            EditorGUILayout.LabelField( $"Total: {audioItemList.Count}", EditorStyles.miniLabel );
            listScrollPosition = EditorGUILayout.BeginScrollView( listScrollPosition );

            for ( int index = 0; index < audioItemList.Count; index++ )
            {
                CAudioVolumeAdjusterItem audioItem = audioItemList[ index ];

                if ( IsMatchedSearch( audioItem ) == false )
                {
                    continue;
                }

                DrawAudioListItem( audioItem, index );
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 오디오 목록 항목 렌더링
        ///</summary>
        private void DrawAudioListItem( CAudioVolumeAdjusterItem _audioItem, int _index )
        {
            if ( _audioItem == null )
            {
                return;
            }

            bool isSelected = selectedItemIndex == _index;
            GUIStyle buttonStyle = new GUIStyle( EditorStyles.miniButton );
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.fixedHeight = ListItemHeight;
            string supportLabel = _audioItem.isSupportedWav ? "WAV" : "Unsupported";
            string buttonLabel = $"[ {_audioItem.category} / {supportLabel} ] {_audioItem.displayName}\nPeak {_audioItem.peakDb:0.0} dB / RMS {_audioItem.rmsDb:0.0} dB";

            if ( GUILayout.Button( buttonLabel, buttonStyle ) )
            {
                selectedItemIndex = _index;
                previewGainDb = 0.0f;
                GUI.FocusControl( null );
            }

            if ( isSelected )
            {
                Rect itemRect = GUILayoutUtility.GetLastRect();
                EditorGUI.DrawRect( itemRect, new Color( 0.2f, 0.5f, 0.85f, 0.18f ) );
            }
        }

        ///<summary>
        /// 상세 편집 영역 렌더링
        ///</summary>
        private void DrawDetailSection()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField( "Editor", EditorStyles.boldLabel );
            detailScrollPosition = EditorGUILayout.BeginScrollView( detailScrollPosition );
            CAudioVolumeAdjusterItem selectedItem = GetSelectedItem();

            if ( selectedItem == null )
            {
                EditorGUILayout.HelpBox( "왼쪽 목록에서 오디오 파일을 선택하세요.", MessageType.Info );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawSelectedItemInfo( selectedItem );

            if ( selectedItem.isSupportedWav == false )
            {
                EditorGUILayout.HelpBox( "현재 툴은 WAV 원본 파일만 직접 수정합니다. mp3/ogg는 DAW 또는 외부 변환 후 WAV로 관리하는 방식을 권장합니다.", MessageType.Warning );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            DrawGainControlSection( selectedItem );
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 선택 오디오 정보 렌더링
        ///</summary>
        private void DrawSelectedItemInfo( CAudioVolumeAdjusterItem _audioItem )
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( _audioItem.displayName, EditorStyles.boldLabel );
            EditorGUILayout.LabelField( "Path", _audioItem.assetPath );
            EditorGUILayout.LabelField( "Category", _audioItem.category );
            EditorGUILayout.LabelField( "Format", $"{_audioItem.channelCount}ch / {_audioItem.sampleRate}Hz / {_audioItem.bitsPerSample}bit" );
            EditorGUILayout.LabelField( "Duration", $"{_audioItem.durationSeconds:0.00}s" );
            EditorGUILayout.LabelField( "Peak", $"{_audioItem.peakDb:0.00} dBFS" );
            EditorGUILayout.LabelField( "RMS", $"{_audioItem.rmsDb:0.00} dBFS" );
            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 게인 조절 영역 렌더링
        ///</summary>
        private void DrawGainControlSection( CAudioVolumeAdjusterItem _audioItem )
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            EditorGUILayout.LabelField( "Gain", EditorStyles.boldLabel );
            previewGainDb = EditorGUILayout.Slider( "Gain dB", previewGainDb, MinGainDb, MaxGainDb );
            targetPeakDb = EditorGUILayout.Slider( "Normalize Target Peak", targetPeakDb, -12.0f, 0.0f );
            createBackup = EditorGUILayout.Toggle( "Create Backup", createBackup );

            float expectedPeakDb = _audioItem.peakDb <= SilenceDb ? SilenceDb : _audioItem.peakDb + previewGainDb;
            EditorGUILayout.LabelField( "Expected Peak", $"{expectedPeakDb:0.00} dBFS" );

            EditorGUILayout.BeginHorizontal();

            if ( GUILayout.Button( "Preview Original", GUILayout.Height( 28.0f ) ) )
            {
                PreviewOriginalAudio( _audioItem );
            }

            if ( GUILayout.Button( "Preview Gain", GUILayout.Height( 28.0f ) ) )
            {
                PreviewAudio( _audioItem, previewGainDb );
            }

            if ( GUILayout.Button( "Set Normalize Gain", GUILayout.Height( 28.0f ) ) )
            {
                SetNormalizeGain( _audioItem );
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            using ( new EditorGUI.DisabledScope( Mathf.Abs( previewGainDb ) <= 0.001f ) )
            {
                if ( GUILayout.Button( "Apply Gain To WAV", GUILayout.Height( 34.0f ) ) )
                {
                    ApplyGainToSelectedWav( _audioItem );
                }
            }

            EditorGUILayout.EndVertical();
        }

        ///<summary>
        /// 오디오 항목 목록 갱신
        ///</summary>
        private void RefreshAudioItemList()
        {
            audioItemList.Clear();
            AddAudioItemsFromRoot( BgmRootPath, "BGM" );
            AddAudioItemsFromRoot( SfxRootPath, "SFX" );
            audioItemList.Sort( CompareAudioItem );

            if ( selectedItemIndex >= audioItemList.Count )
            {
                selectedItemIndex = audioItemList.Count - 1;
            }

            statusMessage = $"오디오 파일 {audioItemList.Count}개를 검색했습니다.";
            statusMessageType = MessageType.Info;
        }

        ///<summary>
        /// 루트 경로의 오디오 항목 추가
        ///</summary>
        private void AddAudioItemsFromRoot( string _rootPath, string _category )
        {
            if ( AssetDatabase.IsValidFolder( _rootPath ) == false )
            {
                return;
            }

            string[] audioGuidArray = AssetDatabase.FindAssets( "t:AudioClip", new string[] { _rootPath } );

            for ( int index = 0; index < audioGuidArray.Length; index++ )
            {
                string audioGuid = audioGuidArray[ index ];
                string assetPath = AssetDatabase.GUIDToAssetPath( audioGuid );
                CAudioVolumeAdjusterItem audioItem = CreateAudioItem( assetPath, _category );

                if ( audioItem == null )
                {
                    continue;
                }

                audioItemList.Add( audioItem );
            }
        }

        ///<summary>
        /// 오디오 항목 생성
        ///</summary>
        private CAudioVolumeAdjusterItem CreateAudioItem( string _assetPath, string _category )
        {
            AudioClip audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>( _assetPath );

            if ( audioClip == null )
            {
                return null;
            }

            CAudioVolumeAdjusterItem audioItem = new CAudioVolumeAdjusterItem();
            audioItem.assetPath = _assetPath;
            audioItem.displayName = Path.GetFileNameWithoutExtension( _assetPath );
            audioItem.category = _category;
            audioItem.durationSeconds = audioClip.length;
            audioItem.sampleRate = audioClip.frequency;
            audioItem.channelCount = audioClip.channels;
            audioItem.isSupportedWav = string.Equals( Path.GetExtension( _assetPath ), ".wav", StringComparison.OrdinalIgnoreCase );
            string errorMessage = string.Empty;

            if ( audioItem.isSupportedWav && TryReadWavFile( _assetPath, out CWavAudioData wavAudioData, out errorMessage ) )
            {
                audioItem.bitsPerSample = wavAudioData.bitsPerSample;
                audioItem.peakDb = ConvertLinearToDb( CalculatePeak( wavAudioData.sampleArray ) );
                audioItem.rmsDb = ConvertLinearToDb( CalculateRms( wavAudioData.sampleArray ) );
                return audioItem;
            }

            audioItem.bitsPerSample = 0;
            audioItem.peakDb = SilenceDb;
            audioItem.rmsDb = SilenceDb;

            if ( audioItem.isSupportedWav && string.IsNullOrWhiteSpace( errorMessage ) == false )
            {
                Debug.LogWarning( $"[Audio Volume Adjuster] WAV 분석 실패: {_assetPath} / {errorMessage}" );
            }

            return audioItem;
        }

        ///<summary>
        /// 검색어 일치 여부 반환
        ///</summary>
        private bool IsMatchedSearch( CAudioVolumeAdjusterItem _audioItem )
        {
            if ( _audioItem == null )
            {
                return false;
            }

            if ( string.IsNullOrWhiteSpace( searchText ) )
            {
                return true;
            }

            string loweredSearchText = searchText.Trim().ToLowerInvariant();
            bool result = _audioItem.displayName.ToLowerInvariant().Contains( loweredSearchText )
                || _audioItem.assetPath.ToLowerInvariant().Contains( loweredSearchText )
                || _audioItem.category.ToLowerInvariant().Contains( loweredSearchText );
            return result;
        }

        ///<summary>
        /// 오디오 항목 정렬 비교
        ///</summary>
        private int CompareAudioItem( CAudioVolumeAdjusterItem _left, CAudioVolumeAdjusterItem _right )
        {
            string leftCategory = _left != null ? _left.category : string.Empty;
            string rightCategory = _right != null ? _right.category : string.Empty;
            int categoryCompareResult = string.Compare( leftCategory, rightCategory, StringComparison.OrdinalIgnoreCase );

            if ( categoryCompareResult != 0 )
            {
                return categoryCompareResult;
            }

            string leftName = _left != null ? _left.displayName : string.Empty;
            string rightName = _right != null ? _right.displayName : string.Empty;
            int result = string.Compare( leftName, rightName, StringComparison.OrdinalIgnoreCase );
            return result;
        }

        ///<summary>
        /// 선택 항목 반환
        ///</summary>
        private CAudioVolumeAdjusterItem GetSelectedItem()
        {
            if ( selectedItemIndex < 0 || selectedItemIndex >= audioItemList.Count )
            {
                return null;
            }

            CAudioVolumeAdjusterItem result = audioItemList[ selectedItemIndex ];
            return result;
        }

        ///<summary>
        /// 정규화 게인 설정
        ///</summary>
        private void SetNormalizeGain( CAudioVolumeAdjusterItem _audioItem )
        {
            if ( _audioItem == null || _audioItem.peakDb <= SilenceDb )
            {
                previewGainDb = 0.0f;
                return;
            }

            previewGainDb = Mathf.Clamp( targetPeakDb - _audioItem.peakDb, MinGainDb, MaxGainDb );
        }

        ///<summary>
        /// 선택 WAV에 게인 적용
        ///</summary>
        private void ApplyGainToSelectedWav( CAudioVolumeAdjusterItem _audioItem )
        {
            if ( _audioItem == null || _audioItem.isSupportedWav == false )
            {
                return;
            }

            string dialogMessage = $"{_audioItem.displayName} 원본 WAV에 {previewGainDb:0.00} dB gain을 적용합니다.";
            bool isConfirmed = EditorUtility.DisplayDialog( "Apply Audio Gain", dialogMessage, "Apply", "Cancel" );

            if ( isConfirmed == false )
            {
                return;
            }

            bool didApply = TryApplyGainToWavFile( _audioItem.assetPath, previewGainDb, createBackup, out string resultMessage );

            if ( didApply )
            {
                AssetDatabase.ImportAsset( _audioItem.assetPath, ImportAssetOptions.ForceUpdate );
                RefreshAudioItemList();
                statusMessage = resultMessage;
                statusMessageType = MessageType.Info;
                return;
            }

            statusMessage = resultMessage;
            statusMessageType = MessageType.Error;
        }

        ///<summary>
        /// 오디오 미리듣기 재생
        ///</summary>
        private void PreviewAudio( CAudioVolumeAdjusterItem _audioItem, float _gainDb )
        {
            if ( _audioItem == null )
            {
                return;
            }

            StopPreview();

            if ( TryReadWavFile( _audioItem.assetPath, out CWavAudioData wavAudioData, out string errorMessage ) == false )
            {
                statusMessage = errorMessage;
                statusMessageType = MessageType.Error;
                return;
            }

            float[] previewSampleArray = CreateGainAppliedSamples( wavAudioData.sampleArray, _gainDb );
            bool didCreatePreviewAsset = TryCreateTemporaryPreviewAsset( wavAudioData, previewSampleArray, out string previewErrorMessage );

            if ( didCreatePreviewAsset == false )
            {
                statusMessage = previewErrorMessage;
                statusMessageType = MessageType.Error;
                return;
            }

            currentPreviewClip = AssetDatabase.LoadAssetAtPath<AudioClip>( TemporaryPreviewAssetPath );

            if ( currentPreviewClip == null )
            {
                statusMessage = "Gain preview 임시 AudioClip 에셋을 로드하지 못했습니다.";
                statusMessageType = MessageType.Error;
                return;
            }

            PlayPreviewClip( currentPreviewClip );
            statusMessage = $"{_audioItem.displayName} preview gain {_gainDb:0.00} dB";
            statusMessageType = MessageType.Info;
        }

        ///<summary>
        /// 원본 오디오 에셋 미리듣기 재생
        ///</summary>
        private void PreviewOriginalAudio( CAudioVolumeAdjusterItem _audioItem )
        {
            if ( _audioItem == null )
            {
                return;
            }

            StopPreview();
            currentOriginalPreviewClip = AssetDatabase.LoadAssetAtPath<AudioClip>( _audioItem.assetPath );

            if ( currentOriginalPreviewClip == null )
            {
                statusMessage = "원본 AudioClip 에셋을 로드하지 못했습니다.";
                statusMessageType = MessageType.Error;
                return;
            }

            PlayPreviewClip( currentOriginalPreviewClip );
            statusMessage = $"{_audioItem.displayName} original preview";
            statusMessageType = MessageType.Info;
        }

        ///<summary>
        /// 미리듣기 정지
        ///</summary>
        private void StopPreview()
        {
            Type audioUtilType = ResolveAudioUtilType();

            if ( audioUtilType == null )
            {
                return;
            }

            MethodInfo stopMethod = FindPreviewStopMethod( audioUtilType );

            if ( stopMethod != null )
            {
                try
                {
                    stopMethod.Invoke( null, null );
                }
                catch ( Exception exception )
                {
                    Debug.LogWarning( $"[Audio Volume Adjuster] Preview stop failed: {exception.Message}" );
                }
            }

            if ( currentPreviewClip != null )
            {
                currentPreviewClip = null;
            }

            currentOriginalPreviewClip = null;
            DeleteTemporaryPreviewAsset();
        }

        ///<summary>
        /// 임시 Gain Preview 에셋 생성
        ///</summary>
        private bool TryCreateTemporaryPreviewAsset( CWavAudioData _sourceWavAudioData, float[] _previewSampleArray, out string _errorMessage )
        {
            _errorMessage = string.Empty;

            if ( _sourceWavAudioData == null || _previewSampleArray == null )
            {
                _errorMessage = "Gain preview 샘플 데이터가 유효하지 않습니다.";
                return false;
            }

            try
            {
                DeleteTemporaryPreviewAsset();
                Directory.CreateDirectory( ConvertAssetPathToAbsolutePath( TemporaryPreviewFolderPath ) );

                CWavAudioData previewWavAudioData = new CWavAudioData();
                previewWavAudioData.fileByteArray = new byte[ _sourceWavAudioData.fileByteArray.Length ];
                Buffer.BlockCopy( _sourceWavAudioData.fileByteArray, 0, previewWavAudioData.fileByteArray, 0, _sourceWavAudioData.fileByteArray.Length );
                previewWavAudioData.sampleArray = _previewSampleArray;
                previewWavAudioData.dataOffset = _sourceWavAudioData.dataOffset;
                previewWavAudioData.dataSize = _sourceWavAudioData.dataSize;
                previewWavAudioData.frameCount = _sourceWavAudioData.frameCount;
                previewWavAudioData.sampleRate = _sourceWavAudioData.sampleRate;
                previewWavAudioData.channelCount = _sourceWavAudioData.channelCount;
                previewWavAudioData.bitsPerSample = _sourceWavAudioData.bitsPerSample;
                previewWavAudioData.audioFormat = _sourceWavAudioData.audioFormat;
                WriteSamplesToWavBytes( previewWavAudioData );

                string absolutePreviewPath = ConvertAssetPathToAbsolutePath( TemporaryPreviewAssetPath );
                File.WriteAllBytes( absolutePreviewPath, previewWavAudioData.fileByteArray );
                AssetDatabase.ImportAsset( TemporaryPreviewAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate );
                return true;
            }
            catch ( Exception exception )
            {
                _errorMessage = $"Gain preview 임시 에셋 생성 실패: {exception.Message}";
                return false;
            }
        }

        ///<summary>
        /// 임시 Gain Preview 에셋 삭제
        ///</summary>
        private void DeleteTemporaryPreviewAsset()
        {
            if ( string.IsNullOrWhiteSpace( TemporaryPreviewAssetPath ) )
            {
                return;
            }

            if ( File.Exists( ConvertAssetPathToAbsolutePath( TemporaryPreviewAssetPath ) ) )
            {
                AssetDatabase.DeleteAsset( TemporaryPreviewAssetPath );
            }

            string absolutePreviewFolderPath = ConvertAssetPathToAbsolutePath( TemporaryPreviewFolderPath );

            if ( Directory.Exists( absolutePreviewFolderPath ) && Directory.GetFiles( absolutePreviewFolderPath ).Length == 0 && Directory.GetDirectories( absolutePreviewFolderPath ).Length == 0 )
            {
                AssetDatabase.DeleteAsset( TemporaryPreviewFolderPath );
            }
        }

        ///<summary>
        /// 미리듣기 클립 재생
        ///</summary>
        private void PlayPreviewClip( AudioClip _audioClip )
        {
            if ( _audioClip == null )
            {
                return;
            }

            Type audioUtilType = ResolveAudioUtilType();

            if ( audioUtilType == null )
            {
                statusMessage = "UnityEditor.AudioUtil 타입을 찾을 수 없습니다.";
                statusMessageType = MessageType.Warning;
                return;
            }

            MethodInfo playMethod = FindPreviewPlayMethod( audioUtilType );

            if ( playMethod == null )
            {
                statusMessage = "현재 Unity 버전의 AudioUtil preview API를 찾을 수 없습니다.";
                statusMessageType = MessageType.Warning;
                return;
            }

            ParameterInfo[] parameterArray = playMethod.GetParameters();
            object[] argumentArray = BuildPreviewPlayArguments( parameterArray, _audioClip );

            try
            {
                playMethod.Invoke( null, argumentArray );
            }
            catch ( Exception exception )
            {
                statusMessage = $"Preview 재생 실패: {exception.Message}";
                statusMessageType = MessageType.Error;
                Debug.LogError( $"[Audio Volume Adjuster] Preview play failed: {exception}" );
            }
        }

        ///<summary>
        /// AudioUtil 타입 반환
        ///</summary>
        private Type ResolveAudioUtilType()
        {
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();

            for ( int index = 0; index < assemblyArray.Length; index++ )
            {
                Assembly assembly = assemblyArray[ index ];

                if ( assembly == null )
                {
                    continue;
                }

                Type audioUtilType = assembly.GetType( "UnityEditor.AudioUtil" );

                if ( audioUtilType != null )
                {
                    return audioUtilType;
                }
            }

            return null;
        }

        ///<summary>
        /// 미리듣기 재생 메서드 반환
        ///</summary>
        private MethodInfo FindPreviewPlayMethod( Type _audioUtilType )
        {
            MethodInfo[] methodArray = _audioUtilType.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );
            MethodInfo fallbackMethod = null;

            for ( int index = 0; index < methodArray.Length; index++ )
            {
                MethodInfo methodInfo = methodArray[ index ];

                if ( string.Equals( methodInfo.Name, "PlayPreviewClip", StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                ParameterInfo[] parameterArray = methodInfo.GetParameters();

                if ( parameterArray.Length <= 0 || parameterArray[ 0 ].ParameterType != typeof( AudioClip ) )
                {
                    continue;
                }

                if ( parameterArray.Length == 3 && parameterArray[ 1 ].ParameterType == typeof( int ) && parameterArray[ 2 ].ParameterType == typeof( bool ) )
                {
                    return methodInfo;
                }

                fallbackMethod = methodInfo;
            }

            if ( fallbackMethod != null )
            {
                return fallbackMethod;
            }

            for ( int index = 0; index < methodArray.Length; index++ )
            {
                MethodInfo methodInfo = methodArray[ index ];

                if ( string.Equals( methodInfo.Name, "PlayClip", StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                ParameterInfo[] parameterArray = methodInfo.GetParameters();

                if ( parameterArray.Length <= 0 || parameterArray[ 0 ].ParameterType != typeof( AudioClip ) )
                {
                    continue;
                }

                return methodInfo;
            }

            return null;
        }

        ///<summary>
        /// 미리듣기 정지 메서드 반환
        ///</summary>
        private MethodInfo FindPreviewStopMethod( Type _audioUtilType )
        {
            MethodInfo stopAllPreviewClipsMethod = _audioUtilType.GetMethod( "StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );

            if ( stopAllPreviewClipsMethod != null )
            {
                return stopAllPreviewClipsMethod;
            }

            MethodInfo stopAllClipsMethod = _audioUtilType.GetMethod( "StopAllClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic );
            return stopAllClipsMethod;
        }

        ///<summary>
        /// 미리듣기 재생 인자 생성
        ///</summary>
        private object[] BuildPreviewPlayArguments( ParameterInfo[] _parameterArray, AudioClip _audioClip )
        {
            object[] argumentArray = new object[ _parameterArray.Length ];
            argumentArray[ 0 ] = _audioClip;

            for ( int index = 1; index < _parameterArray.Length; index++ )
            {
                Type parameterType = _parameterArray[ index ].ParameterType;

                if ( parameterType == typeof( int ) )
                {
                    argumentArray[ index ] = 0;
                    continue;
                }

                if ( parameterType == typeof( bool ) )
                {
                    argumentArray[ index ] = false;
                    continue;
                }

                if ( parameterType == typeof( float ) )
                {
                    argumentArray[ index ] = 1.0f;
                    continue;
                }

                if ( parameterType == typeof( double ) )
                {
                    argumentArray[ index ] = 1.0d;
                    continue;
                }

                if ( parameterType.IsEnum )
                {
                    argumentArray[ index ] = Activator.CreateInstance( parameterType );
                    continue;
                }

                argumentArray[ index ] = parameterType.IsValueType ? Activator.CreateInstance( parameterType ) : null;
            }

            return argumentArray;
        }

        ///<summary>
        /// WAV 파일에 게인 적용
        ///</summary>
        private bool TryApplyGainToWavFile( string _assetPath, float _gainDb, bool _createBackup, out string _resultMessage )
        {
            _resultMessage = string.Empty;

            if ( TryReadWavFile( _assetPath, out CWavAudioData wavAudioData, out string errorMessage ) == false )
            {
                _resultMessage = errorMessage;
                return false;
            }

            string absolutePath = ConvertAssetPathToAbsolutePath( _assetPath );

            if ( _createBackup )
            {
                CreateBackupFile( absolutePath );
            }

            int clippedSampleCount = ApplyGainToSampleArray( wavAudioData.sampleArray, _gainDb );
            WriteSamplesToWavBytes( wavAudioData );
            File.WriteAllBytes( absolutePath, wavAudioData.fileByteArray );
            _resultMessage = $"Gain 적용 완료: {_assetPath} / {_gainDb:0.00} dB / Clipped Samples: {clippedSampleCount}";
            return true;
        }

        ///<summary>
        /// 백업 파일 생성
        ///</summary>
        private void CreateBackupFile( string _absolutePath )
        {
            string projectRootPath = Directory.GetParent( Application.dataPath ).FullName;
            string backupRootPath = Path.Combine( projectRootPath, BackupRootRelativePath );
            string timestamp = DateTime.Now.ToString( "yyyyMMdd_HHmmss" );
            string backupFolderPath = Path.Combine( backupRootPath, timestamp );
            Directory.CreateDirectory( backupFolderPath );
            string backupFilePath = Path.Combine( backupFolderPath, Path.GetFileName( _absolutePath ) );
            File.Copy( _absolutePath, backupFilePath, true );
        }

        ///<summary>
        /// 에셋 경로를 절대 경로로 변환
        ///</summary>
        private string ConvertAssetPathToAbsolutePath( string _assetPath )
        {
            string projectRootPath = Directory.GetParent( Application.dataPath ).FullName;
            string result = Path.Combine( projectRootPath, _assetPath );
            return result;
        }

        ///<summary>
        /// 게인 적용 샘플 배열 생성
        ///</summary>
        private float[] CreateGainAppliedSamples( float[] _sampleArray, float _gainDb )
        {
            if ( _sampleArray == null )
            {
                return Array.Empty<float>();
            }

            float gainLinear = ConvertDbToLinear( _gainDb );
            float[] result = new float[ _sampleArray.Length ];

            for ( int index = 0; index < _sampleArray.Length; index++ )
            {
                result[ index ] = Mathf.Clamp( _sampleArray[ index ] * gainLinear, -1.0f, 1.0f );
            }

            return result;
        }

        ///<summary>
        /// 샘플 배열에 게인 적용
        ///</summary>
        private int ApplyGainToSampleArray( float[] _sampleArray, float _gainDb )
        {
            float gainLinear = ConvertDbToLinear( _gainDb );
            int clippedSampleCount = 0;

            for ( int index = 0; index < _sampleArray.Length; index++ )
            {
                float rawSample = _sampleArray[ index ] * gainLinear;

                if ( rawSample > 1.0f || rawSample < -1.0f )
                {
                    clippedSampleCount++;
                }

                _sampleArray[ index ] = Mathf.Clamp( rawSample, -1.0f, 1.0f );
            }

            return clippedSampleCount;
        }

        ///<summary>
        /// WAV 파일 읽기
        ///</summary>
        private bool TryReadWavFile( string _assetPath, out CWavAudioData _wavAudioData, out string _errorMessage )
        {
            _wavAudioData = null;
            _errorMessage = string.Empty;
            string absolutePath = ConvertAssetPathToAbsolutePath( _assetPath );

            if ( File.Exists( absolutePath ) == false )
            {
                _errorMessage = "파일이 존재하지 않습니다.";
                return false;
            }

            byte[] fileByteArray = File.ReadAllBytes( absolutePath );

            if ( fileByteArray.Length < RiffHeaderSize || ReadAscii( fileByteArray, 0, 4 ) != "RIFF" || ReadAscii( fileByteArray, 8, 4 ) != "WAVE" )
            {
                _errorMessage = "RIFF/WAVE 파일이 아닙니다.";
                return false;
            }

            bool hasFormatChunk = false;
            bool hasDataChunk = false;
            CWavAudioData wavAudioData = new CWavAudioData();
            wavAudioData.fileByteArray = fileByteArray;
            int offset = RiffHeaderSize;

            while ( offset + ChunkHeaderSize <= fileByteArray.Length )
            {
                string chunkId = ReadAscii( fileByteArray, offset, 4 );
                int chunkSize = ReadInt32LittleEndian( fileByteArray, offset + 4 );
                int chunkDataOffset = offset + ChunkHeaderSize;

                if ( chunkSize < 0 || chunkDataOffset + chunkSize > fileByteArray.Length )
                {
                    _errorMessage = "WAV chunk 크기가 올바르지 않습니다.";
                    return false;
                }

                if ( chunkId == "fmt " )
                {
                    ReadFormatChunk( wavAudioData, fileByteArray, chunkDataOffset, chunkSize );
                    hasFormatChunk = true;
                }
                else if ( chunkId == "data" )
                {
                    wavAudioData.dataOffset = chunkDataOffset;
                    wavAudioData.dataSize = chunkSize;
                    hasDataChunk = true;
                }

                offset = chunkDataOffset + chunkSize + chunkSize % 2;
            }

            if ( hasFormatChunk == false || hasDataChunk == false )
            {
                _errorMessage = "fmt 또는 data chunk를 찾지 못했습니다.";
                return false;
            }

            if ( IsSupportedWavFormat( wavAudioData ) == false )
            {
                _errorMessage = $"지원하지 않는 WAV 포맷입니다. Format={wavAudioData.audioFormat}, Bits={wavAudioData.bitsPerSample}";
                return false;
            }

            DecodeSamples( wavAudioData );
            _wavAudioData = wavAudioData;
            return true;
        }

        ///<summary>
        /// WAV 포맷 chunk 읽기
        ///</summary>
        private void ReadFormatChunk( CWavAudioData _wavAudioData, byte[] _fileByteArray, int _offset, int _chunkSize )
        {
            if ( _chunkSize < 16 )
            {
                return;
            }

            _wavAudioData.audioFormat = ReadUInt16LittleEndian( _fileByteArray, _offset );
            _wavAudioData.channelCount = ReadUInt16LittleEndian( _fileByteArray, _offset + 2 );
            _wavAudioData.sampleRate = ReadInt32LittleEndian( _fileByteArray, _offset + 4 );
            _wavAudioData.bitsPerSample = ReadUInt16LittleEndian( _fileByteArray, _offset + 14 );
        }

        ///<summary>
        /// 지원 WAV 포맷 여부 반환
        ///</summary>
        private bool IsSupportedWavFormat( CWavAudioData _wavAudioData )
        {
            if ( _wavAudioData == null )
            {
                return false;
            }

            if ( _wavAudioData.audioFormat == WaveFormatFloat )
            {
                bool isFloatSupported = _wavAudioData.bitsPerSample == 32;
                return isFloatSupported;
            }

            if ( _wavAudioData.audioFormat != WaveFormatPcm )
            {
                return false;
            }

            bool result = _wavAudioData.bitsPerSample == 8 || _wavAudioData.bitsPerSample == 16 || _wavAudioData.bitsPerSample == 24 || _wavAudioData.bitsPerSample == 32;
            return result;
        }

        ///<summary>
        /// WAV 샘플 디코딩
        ///</summary>
        private void DecodeSamples( CWavAudioData _wavAudioData )
        {
            int bytesPerSample = _wavAudioData.bitsPerSample / 8;
            int sampleCount = _wavAudioData.dataSize / bytesPerSample;
            _wavAudioData.frameCount = _wavAudioData.channelCount > 0 ? sampleCount / _wavAudioData.channelCount : 0;
            _wavAudioData.sampleArray = new float[ sampleCount ];

            for ( int index = 0; index < sampleCount; index++ )
            {
                int sampleOffset = _wavAudioData.dataOffset + index * bytesPerSample;
                _wavAudioData.sampleArray[ index ] = DecodeSample( _wavAudioData, sampleOffset );
            }
        }

        ///<summary>
        /// 단일 샘플 디코딩
        ///</summary>
        private float DecodeSample( CWavAudioData _wavAudioData, int _sampleOffset )
        {
            byte[] fileByteArray = _wavAudioData.fileByteArray;

            if ( _wavAudioData.audioFormat == WaveFormatFloat )
            {
                float floatResult = BitConverter.ToSingle( fileByteArray, _sampleOffset );
                return Mathf.Clamp( floatResult, -1.0f, 1.0f );
            }

            switch ( _wavAudioData.bitsPerSample )
            {
                case 8:
                    return ( fileByteArray[ _sampleOffset ] - 128 ) / 128.0f;

                case 16:
                    short sample16 = BitConverter.ToInt16( fileByteArray, _sampleOffset );
                    return sample16 / 32768.0f;

                case 24:
                    int sample24 = ReadSigned24LittleEndian( fileByteArray, _sampleOffset );
                    return sample24 / 8388608.0f;

                case 32:
                    int sample32 = BitConverter.ToInt32( fileByteArray, _sampleOffset );
                    return sample32 / 2147483648.0f;

                default:
                    return 0.0f;
            }
        }

        ///<summary>
        /// WAV 샘플 데이터 쓰기
        ///</summary>
        private void WriteSamplesToWavBytes( CWavAudioData _wavAudioData )
        {
            int bytesPerSample = _wavAudioData.bitsPerSample / 8;

            for ( int index = 0; index < _wavAudioData.sampleArray.Length; index++ )
            {
                int sampleOffset = _wavAudioData.dataOffset + index * bytesPerSample;
                WriteSample( _wavAudioData, sampleOffset, _wavAudioData.sampleArray[ index ] );
            }
        }

        ///<summary>
        /// 단일 샘플 쓰기
        ///</summary>
        private void WriteSample( CWavAudioData _wavAudioData, int _sampleOffset, float _sampleValue )
        {
            byte[] fileByteArray = _wavAudioData.fileByteArray;
            float clampedSampleValue = Mathf.Clamp( _sampleValue, -1.0f, 1.0f );

            if ( _wavAudioData.audioFormat == WaveFormatFloat )
            {
                byte[] floatByteArray = BitConverter.GetBytes( clampedSampleValue );
                Buffer.BlockCopy( floatByteArray, 0, fileByteArray, _sampleOffset, floatByteArray.Length );
                return;
            }

            switch ( _wavAudioData.bitsPerSample )
            {
                case 8:
                    fileByteArray[ _sampleOffset ] = ( byte )Mathf.Clamp( Mathf.RoundToInt( clampedSampleValue * 127.0f + 128.0f ), 0, 255 );
                    break;

                case 16:
                    short sample16 = ( short )Mathf.Clamp( Mathf.RoundToInt( clampedSampleValue * 32767.0f ), short.MinValue, short.MaxValue );
                    WriteBytes( fileByteArray, _sampleOffset, BitConverter.GetBytes( sample16 ) );
                    break;

                case 24:
                    int sample24 = Mathf.Clamp( Mathf.RoundToInt( clampedSampleValue * 8388607.0f ), -8388608, 8388607 );
                    WriteSigned24LittleEndian( fileByteArray, _sampleOffset, sample24 );
                    break;

                case 32:
                    int sample32 = Mathf.RoundToInt( clampedSampleValue * 2147483647.0f );
                    WriteBytes( fileByteArray, _sampleOffset, BitConverter.GetBytes( sample32 ) );
                    break;
            }
        }

        ///<summary>
        /// 바이트 배열 쓰기
        ///</summary>
        private void WriteBytes( byte[] _targetByteArray, int _offset, byte[] _sourceByteArray )
        {
            Buffer.BlockCopy( _sourceByteArray, 0, _targetByteArray, _offset, _sourceByteArray.Length );
        }

        ///<summary>
        /// ASCII 문자열 읽기
        ///</summary>
        private string ReadAscii( byte[] _byteArray, int _offset, int _length )
        {
            string result = System.Text.Encoding.ASCII.GetString( _byteArray, _offset, _length );
            return result;
        }

        ///<summary>
        /// 리틀엔디언 Int32 읽기
        ///</summary>
        private int ReadInt32LittleEndian( byte[] _byteArray, int _offset )
        {
            int result = BitConverter.ToInt32( _byteArray, _offset );
            return result;
        }

        ///<summary>
        /// 리틀엔디언 UInt16 읽기
        ///</summary>
        private ushort ReadUInt16LittleEndian( byte[] _byteArray, int _offset )
        {
            ushort result = BitConverter.ToUInt16( _byteArray, _offset );
            return result;
        }

        ///<summary>
        /// 리틀엔디언 24비트 정수 읽기
        ///</summary>
        private int ReadSigned24LittleEndian( byte[] _byteArray, int _offset )
        {
            int value = _byteArray[ _offset ] | ( _byteArray[ _offset + 1 ] << 8 ) | ( _byteArray[ _offset + 2 ] << 16 );

            if ( ( value & 0x800000 ) != 0 )
            {
                value |= unchecked( ( int )0xFF000000 );
            }

            return value;
        }

        ///<summary>
        /// 리틀엔디언 24비트 정수 쓰기
        ///</summary>
        private void WriteSigned24LittleEndian( byte[] _byteArray, int _offset, int _value )
        {
            _byteArray[ _offset ] = ( byte )( _value & 0xFF );
            _byteArray[ _offset + 1 ] = ( byte )( ( _value >> 8 ) & 0xFF );
            _byteArray[ _offset + 2 ] = ( byte )( ( _value >> 16 ) & 0xFF );
        }

        ///<summary>
        /// 피크 값 계산
        ///</summary>
        private float CalculatePeak( float[] _sampleArray )
        {
            float peak = 0.0f;

            for ( int index = 0; index < _sampleArray.Length; index++ )
            {
                float absoluteSample = Mathf.Abs( _sampleArray[ index ] );

                if ( absoluteSample > peak )
                {
                    peak = absoluteSample;
                }
            }

            return peak;
        }

        ///<summary>
        /// RMS 값 계산
        ///</summary>
        private float CalculateRms( float[] _sampleArray )
        {
            if ( _sampleArray == null || _sampleArray.Length == 0 )
            {
                return 0.0f;
            }

            double squareSum = 0.0d;

            for ( int index = 0; index < _sampleArray.Length; index++ )
            {
                float sample = _sampleArray[ index ];
                squareSum += sample * sample;
            }

            float result = Mathf.Sqrt( ( float )( squareSum / _sampleArray.Length ) );
            return result;
        }

        ///<summary>
        /// dB 값을 선형 값으로 변환
        ///</summary>
        private float ConvertDbToLinear( float _db )
        {
            float result = Mathf.Pow( 10.0f, _db / 20.0f );
            return result;
        }

        ///<summary>
        /// 선형 값을 dB 값으로 변환
        ///</summary>
        private float ConvertLinearToDb( float _linear )
        {
            if ( _linear <= 0.0001f )
            {
                return SilenceDb;
            }

            float result = 20.0f * Mathf.Log10( _linear );
            return result;
        }

        ///<summary>
        /// WAV 오디오 데이터
        ///</summary>
        private sealed class CWavAudioData
        {
            public byte[] fileByteArray;
            public float[] sampleArray;
            public int dataOffset;
            public int dataSize;
            public int frameCount;
            public int sampleRate;
            public int channelCount;
            public int bitsPerSample;
            public ushort audioFormat;
        }
    }
}
