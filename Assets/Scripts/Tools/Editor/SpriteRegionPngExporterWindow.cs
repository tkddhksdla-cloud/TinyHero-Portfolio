using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 스프라이트 영역 PNG 내보내기 에디터 창
    ///</summary>
    public sealed class SpriteRegionPngExporterWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/ResizedSprites";
        private const string DefaultFileName = "ResizedSprite";
        private const float DefaultResizeScalePercent = 50.0f;
        private const int DefaultMaxResizeWidth = 128;
        private const int DefaultMaxResizeHeight = 128;

        private enum eResizeMode
        {
            PERCENT,
            VALUE
        }

        [SerializeField] private Texture2D sourceTexture;
        [SerializeField] private string outputFolder = DefaultOutputFolder;
        [SerializeField] private string fileName = string.Empty;
        [SerializeField] private eResizeMode resizeMode = eResizeMode.PERCENT;
        [SerializeField] private float resizeScalePercent = DefaultResizeScalePercent;
        [SerializeField] private int maxResizeWidth = DefaultMaxResizeWidth;
        [SerializeField] private int maxResizeHeight = DefaultMaxResizeHeight;
        [SerializeField] private bool allowUpscale;

        private Vector2 scrollPosition;
        private string statusMessage = "?뚯뒪 ?대?吏瑜?吏?뺥븳 ????μ쓣 ?ㅽ뻾?섏꽭??";
        private MessageType statusMessageType = MessageType.Info;
        private bool useAutoFileName = true;
        private string lastSuggestedFileName = string.Empty;

        ///<summary>
        /// 에디터 창 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Sprite Resize PNG Exporter" )]
        private static void ShowWindow()
        {
            SpriteRegionPngExporterWindow window = GetWindow<SpriteRegionPngExporterWindow>();
            window.titleContent = new GUIContent( "Sprite Resize Exporter" );
            window.minSize = new Vector2( 420.0f, 340.0f );
            window.Show();
        }

        ///<summary>
        /// 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            RefreshSuggestedFileName( true );
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Sprite Resize PNG Exporter", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "?먮낯 ?ㅽ봽?쇱씠?몃? 鍮꾩쑉 ?좎? 由ъ궗?댁쫰 ??PNG濡???ν빀?덈떎.", MessageType.None );
            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView( scrollPosition );

            DrawSourceSection();
            EditorGUILayout.Space();
            DrawOptionSection();
            EditorGUILayout.Space();
            DrawPreviewSection();
            EditorGUILayout.Space();
            DrawActionSection();

            EditorGUILayout.EndScrollView();
        }

        ///<summary>
        /// 원본 섹션 렌더링
        ///</summary>
        private void DrawSourceSection()
        {
            EditorGUILayout.LabelField( "Source", EditorStyles.boldLabel );
            sourceTexture = ( Texture2D )EditorGUILayout.ObjectField( "Texture", sourceTexture, typeof( Texture2D ), false );
            string newOutputFolder = EditorGUILayout.TextField( "Output Folder", outputFolder );

            if ( string.Equals( newOutputFolder, outputFolder, StringComparison.Ordinal ) == false )
            {
                outputFolder = newOutputFolder;
                bool shouldApplySuggestedName = useAutoFileName || string.IsNullOrWhiteSpace( fileName ) || string.Equals( fileName, lastSuggestedFileName, StringComparison.Ordinal );
                RefreshSuggestedFileName( shouldApplySuggestedName );
            }

            string newFileName = EditorGUILayout.TextField( "File Name", fileName );

            if ( string.Equals( newFileName, fileName, StringComparison.Ordinal ) == false )
            {
                HandleFileNameChanged( newFileName );
            }
        }

        ///<summary>
        /// 옵션 섹션 렌더링
        ///</summary>
        private void DrawOptionSection()
        {
            EditorGUILayout.LabelField( "Options", EditorStyles.boldLabel );
            resizeMode = ( eResizeMode )EditorGUILayout.EnumPopup( "Resize Mode", resizeMode );

            if ( resizeMode == eResizeMode.PERCENT )
            {
                resizeScalePercent = EditorGUILayout.FloatField( "Scale Percent", resizeScalePercent );
            }

            if ( resizeMode == eResizeMode.VALUE )
            {
                maxResizeWidth = EditorGUILayout.IntField( "Max Width", maxResizeWidth );
                maxResizeHeight = EditorGUILayout.IntField( "Max Height", maxResizeHeight );
            }

            allowUpscale = EditorGUILayout.Toggle( "Allow Upscale", allowUpscale );
        }

        ///<summary>
        /// 프리뷰 섹션 렌더링
        ///</summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField( "Preview", EditorStyles.boldLabel );

            if ( sourceTexture == null )
            {
                EditorGUILayout.HelpBox( "?뚯뒪 ?띿뒪泥섎? 吏?뺥븯?몄슂.", MessageType.None );
                return;
            }

            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;
            Vector2Int targetSize = CalculateResizeTargetSize( sourceWidth, sourceHeight );
            EditorGUILayout.LabelField( $"Source Size: {sourceWidth} x {sourceHeight}" );
            EditorGUILayout.LabelField( $"Target Size: {targetSize.x} x {targetSize.y}" );
        }

        ///<summary>
        /// 동작 섹션 렌더링
        ///</summary>
        private void DrawActionSection()
        {
            EditorGUILayout.LabelField( "Actions", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Save Resized PNG" ) )
            {
                SaveResizedTexture();
            }

            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
        }

        ///<summary>
        /// 입력 검증
        ///</summary>
        private string ValidateInputs()
        {
            if ( sourceTexture == null )
            {
                string result = "?뚯뒪 ?띿뒪泥섎? 癒쇱? 吏?뺥븯?몄슂.";
                return result;
            }

            if ( string.IsNullOrWhiteSpace( outputFolder ) )
            {
                string result = "異쒕젰 ?대뜑瑜??낅젰?섏꽭??";
                return result;
            }

            bool isAssetsFolder = outputFolder.StartsWith( "Assets", StringComparison.Ordinal );

            if ( isAssetsFolder == false )
            {
                string result = "異쒕젰 ?대뜑??Assets ?섏쐞 寃쎈줈?ъ빞 ?⑸땲??";
                return result;
            }

            if ( resizeMode == eResizeMode.PERCENT && resizeScalePercent <= 0.0f )
            {
                string result = "Scale Percent??0蹂대떎 而ㅼ빞 ?⑸땲??";
                return result;
            }

            if ( resizeMode == eResizeMode.VALUE && ( maxResizeWidth < 1 || maxResizeHeight < 1 ) )
            {
                string result = "Max Width? Max Height??1 ?댁긽?댁뼱???⑸땲??";
                return result;
            }

            string resultMessage = string.Empty;
            return resultMessage;
        }

        ///<summary>
        /// 리사이즈 PNG 저장
        ///</summary>
        private void SaveResizedTexture()
        {
            string validationError = ValidateInputs();

            if ( string.IsNullOrEmpty( validationError ) == false )
            {
                SetStatus( validationError, MessageType.Error );
                return;
            }

            Texture2D readableTexture = CreateReadableTexture( sourceTexture );

            if ( readableTexture == null )
            {
                SetStatus( "?띿뒪泥섎? ?쎌쓣 ???놁뒿?덈떎.", MessageType.Error );
                return;
            }

            Texture2D resizedTexture = null;

            try
            {
                resizedTexture = CreateResizedTextureIfNeeded( readableTexture );
                Texture2D exportTexture = resizedTexture != null ? resizedTexture : readableTexture;
                EnsureOutputFolderExists();

                byte[] pngBytes = exportTexture.EncodeToPNG();
                string safeFileName = SanitizeFileName( fileName );
                string assetPath = Path.Combine( outputFolder, $"{safeFileName}.png" );
                File.WriteAllBytes( assetPath, pngBytes );
                AssetDatabase.Refresh();
                SetStatus( $"PNG ????꾨즺: {assetPath}", MessageType.Info );
                RefreshSuggestedFileName( useAutoFileName );
            }
            finally
            {
                if ( resizedTexture != null )
                {
                    DestroyImmediate( resizedTexture );
                }

                DestroyImmediate( readableTexture );
            }
        }

        ///<summary>
        /// 읽기 가능 텍스처 생성
        ///</summary>
        private Texture2D CreateReadableTexture(Texture2D _texture)
        {
            if ( _texture == null )
            {
                return null;
            }

            int width = _texture.width;
            int height = _texture.height;
            RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary( width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default );
            RenderTexture previousRenderTexture = RenderTexture.active;
            Texture2D readableTexture = null;

            try
            {
                Graphics.Blit( _texture, temporaryRenderTexture );
                RenderTexture.active = temporaryRenderTexture;
                readableTexture = new Texture2D( width, height, TextureFormat.RGBA32, false );
                Rect sourceRect = new Rect( 0.0f, 0.0f, width, height );
                readableTexture.ReadPixels( sourceRect, 0, 0 );
                readableTexture.Apply();
            }
            finally
            {
                RenderTexture.active = previousRenderTexture;
                RenderTexture.ReleaseTemporary( temporaryRenderTexture );
            }

            return readableTexture;
        }

        ///<summary>
        /// 리사이즈 텍스처 조건부 필요 생성
        ///</summary>
        private Texture2D CreateResizedTextureIfNeeded(Texture2D _sourceTextureToResize)
        {
            if ( _sourceTextureToResize == null )
            {
                return null;
            }

            Vector2Int targetSize = CalculateResizeTargetSize( _sourceTextureToResize.width, _sourceTextureToResize.height );

            if ( targetSize.x == _sourceTextureToResize.width && targetSize.y == _sourceTextureToResize.height )
            {
                return null;
            }

            Texture2D resizedTexture = ResizeTexture( _sourceTextureToResize, targetSize.x, targetSize.y );
            return resizedTexture;
        }

        ///<summary>
        /// 리사이즈 대상 크기 계산
        ///</summary>
        private Vector2Int CalculateResizeTargetSize(int _sourceWidth, int _sourceHeight)
        {
            if ( resizeMode == eResizeMode.PERCENT )
            {
                float scale = resizeScalePercent / 100.0f;

                if ( allowUpscale == false )
                {
                    scale = Mathf.Min( 1.0f, scale );
                }

                int targetWidth = Mathf.Max( 1, Mathf.RoundToInt( _sourceWidth * scale ) );
                int targetHeight = Mathf.Max( 1, Mathf.RoundToInt( _sourceHeight * scale ) );
                Vector2Int scaledSize = new Vector2Int( targetWidth, targetHeight );
                return scaledSize;
            }

            float widthRatio = ( float )maxResizeWidth / _sourceWidth;
            float heightRatio = ( float )maxResizeHeight / _sourceHeight;
            float fitScale = Mathf.Min( widthRatio, heightRatio );

            if ( allowUpscale == false )
            {
                fitScale = Mathf.Min( 1.0f, fitScale );
            }

            int fitWidth = Mathf.Max( 1, Mathf.RoundToInt( _sourceWidth * fitScale ) );
            int fitHeight = Mathf.Max( 1, Mathf.RoundToInt( _sourceHeight * fitScale ) );
            Vector2Int fitSize = new Vector2Int( fitWidth, fitHeight );
            return fitSize;
        }

        ///<summary>
        /// 리사이즈 텍스처 처리
        ///</summary>
        private Texture2D ResizeTexture(Texture2D _sourceTextureToResize, int _targetWidth, int _targetHeight)
        {
            Texture2D resizedTexture = new Texture2D( _targetWidth, _targetHeight, TextureFormat.RGBA32, false );
            Color[] resizedPixels = new Color[ _targetWidth * _targetHeight ];
            int sourceWidth = _sourceTextureToResize.width;
            int sourceHeight = _sourceTextureToResize.height;

            for ( int y = 0; y < _targetHeight; y++ )
            {
                float normalizedY = ( y + 0.5f ) / _targetHeight;
                int sourceY = Mathf.Clamp( Mathf.FloorToInt( normalizedY * sourceHeight ), 0, sourceHeight - 1 );

                for ( int x = 0; x < _targetWidth; x++ )
                {
                    float normalizedX = ( x + 0.5f ) / _targetWidth;
                    int sourceX = Mathf.Clamp( Mathf.FloorToInt( normalizedX * sourceWidth ), 0, sourceWidth - 1 );
                    int pixelIndex = y * _targetWidth + x;
                    Color sourceColor = _sourceTextureToResize.GetPixel( sourceX, sourceY );
                    resizedPixels[ pixelIndex ] = sourceColor;
                }
            }

            resizedTexture.SetPixels( resizedPixels );
            resizedTexture.Apply();
            return resizedTexture;
        }

        ///<summary>
        /// 출력 폴더 존재 보장
        ///</summary>
        private void EnsureOutputFolderExists()
        {
            string fullPath = Path.GetFullPath( outputFolder );

            if ( Directory.Exists( fullPath ) )
            {
                return;
            }

            Directory.CreateDirectory( fullPath );
        }

        ///<summary>
        /// 파일 이름 변경 처리
        ///</summary>
        private void HandleFileNameChanged(string _newFileName)
        {
            if ( string.IsNullOrWhiteSpace( _newFileName ) )
            {
                useAutoFileName = true;
                RefreshSuggestedFileName( true );
                return;
            }

            string sanitizedFileName = SanitizeFileName( _newFileName );
            string currentSuggestedFileName = BuildSuggestedFileName();
            bool isSuggestedFileName = string.Equals( sanitizedFileName, currentSuggestedFileName, StringComparison.Ordinal );

            useAutoFileName = isSuggestedFileName;
            fileName = sanitizedFileName;
            lastSuggestedFileName = currentSuggestedFileName;

            if ( useAutoFileName )
            {
                RefreshSuggestedFileName( true );
            }
        }

        ///<summary>
        /// 권장 파일 이름 갱신
        ///</summary>
        private void RefreshSuggestedFileName(bool _applyToField)
        {
            string suggestedFileName = BuildSuggestedFileName();
            lastSuggestedFileName = suggestedFileName;

            if ( _applyToField )
            {
                fileName = suggestedFileName;
            }
        }

        ///<summary>
        /// 권장 파일 이름 구성
        ///</summary>
        private string BuildSuggestedFileName()
        {
            string fullPath = Path.GetFullPath( outputFolder );
            int nextIndex = 1;

            if ( Directory.Exists( fullPath ) )
            {
                string[] existingFiles = Directory.GetFiles( fullPath );
                int fileCount = existingFiles.Length;
                nextIndex = fileCount + 1;
            }

            string suggestedFileName = $"{DefaultFileName}_{nextIndex:D3}";
            return suggestedFileName;
        }

        ///<summary>
        /// 파일 이름 정리 처리
        ///</summary>
        private string SanitizeFileName(string _rawName)
        {
            string candidate = string.IsNullOrWhiteSpace( _rawName ) ? DefaultFileName : _rawName.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();

            foreach ( char invalidCharacter in invalidCharacters )
            {
                candidate = candidate.Replace( invalidCharacter.ToString(), string.Empty );
            }

            bool isEmpty = string.IsNullOrWhiteSpace( candidate );

            if ( isEmpty )
            {
                string fallbackName = DefaultFileName;
                return fallbackName;
            }

            string result = candidate;
            return result;
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus(string _message, MessageType _messageType)
        {
            statusMessage = _message;
            statusMessageType = _messageType;
        }
    }
}


