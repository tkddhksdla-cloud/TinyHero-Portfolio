using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    /// <summary>
    /// 단일 스프라이트를 비율 유지 리사이즈 후 PNG로 저장하는 에디터 창이다.
    /// </summary>
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
        private string statusMessage = "소스 이미지를 지정한 뒤 저장을 실행하세요.";
        private MessageType statusMessageType = MessageType.Info;
        private bool useAutoFileName = true;
        private string lastSuggestedFileName = string.Empty;

        /// <summary>
        /// 리사이즈 전용 PNG 저장 도구 창을 연다.
        /// </summary>
        [MenuItem( "Tools/TinyHero/Sprite Resize PNG Exporter" )]
        private static void ShowWindow()
        {
            SpriteRegionPngExporterWindow window = GetWindow<SpriteRegionPngExporterWindow>();
            window.titleContent = new GUIContent( "Sprite Resize Exporter" );
            window.minSize = new Vector2( 420.0f, 340.0f );
            window.Show();
        }

        /// <summary>
        /// 창이 활성화될 때 기본 파일명 상태를 초기화한다.
        /// </summary>
        private void OnEnable()
        {
            RefreshSuggestedFileName( true );
        }

        /// <summary>
        /// 에디터 창의 입력 UI와 저장 버튼을 그린다.
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Sprite Resize PNG Exporter", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "원본 스프라이트를 비율 유지 리사이즈 후 PNG로 저장합니다.", MessageType.None );
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

        /// <summary>
        /// 소스 텍스처와 출력 경로 입력 영역을 그린다.
        /// </summary>
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

        /// <summary>
        /// 리사이즈 옵션 입력 영역을 그린다.
        /// </summary>
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

        /// <summary>
        /// 현재 원본과 결과 크기 정보를 미리 보여준다.
        /// </summary>
        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField( "Preview", EditorStyles.boldLabel );

            if ( sourceTexture == null )
            {
                EditorGUILayout.HelpBox( "소스 텍스처를 지정하세요.", MessageType.None );
                return;
            }

            int sourceWidth = sourceTexture.width;
            int sourceHeight = sourceTexture.height;
            Vector2Int targetSize = CalculateResizeTargetSize( sourceWidth, sourceHeight );
            EditorGUILayout.LabelField( $"Source Size: {sourceWidth} x {sourceHeight}" );
            EditorGUILayout.LabelField( $"Target Size: {targetSize.x} x {targetSize.y}" );
        }

        /// <summary>
        /// 저장 버튼과 상태 메시지를 그린다.
        /// </summary>
        private void DrawActionSection()
        {
            EditorGUILayout.LabelField( "Actions", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Save Resized PNG" ) )
            {
                SaveResizedTexture();
            }

            EditorGUILayout.HelpBox( statusMessage, statusMessageType );
        }

        /// <summary>
        /// 현재 입력값이 저장 가능한 상태인지 검사한다.
        /// </summary>
        private string ValidateInputs()
        {
            if ( sourceTexture == null )
            {
                string result = "소스 텍스처를 먼저 지정하세요.";
                return result;
            }

            if ( string.IsNullOrWhiteSpace( outputFolder ) )
            {
                string result = "출력 폴더를 입력하세요.";
                return result;
            }

            bool isAssetsFolder = outputFolder.StartsWith( "Assets", StringComparison.Ordinal );

            if ( isAssetsFolder == false )
            {
                string result = "출력 폴더는 Assets 하위 경로여야 합니다.";
                return result;
            }

            if ( resizeMode == eResizeMode.PERCENT && resizeScalePercent <= 0.0f )
            {
                string result = "Scale Percent는 0보다 커야 합니다.";
                return result;
            }

            if ( resizeMode == eResizeMode.VALUE && ( maxResizeWidth < 1 || maxResizeHeight < 1 ) )
            {
                string result = "Max Width와 Max Height는 1 이상이어야 합니다.";
                return result;
            }

            string resultMessage = string.Empty;
            return resultMessage;
        }

        /// <summary>
        /// 원본 텍스처를 리사이즈하여 PNG 파일로 저장한다.
        /// </summary>
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
                SetStatus( "텍스처를 읽을 수 없습니다.", MessageType.Error );
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
                SetStatus( $"PNG 저장 완료: {assetPath}", MessageType.Info );
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

        /// <summary>
        /// GPU 복사를 이용해 읽기 가능한 텍스처 사본을 만든다.
        /// </summary>
        private Texture2D CreateReadableTexture( Texture2D texture )
        {
            if ( texture == null )
            {
                return null;
            }

            int width = texture.width;
            int height = texture.height;
            RenderTexture temporaryRenderTexture = RenderTexture.GetTemporary( width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default );
            RenderTexture previousRenderTexture = RenderTexture.active;
            Texture2D readableTexture = null;

            try
            {
                Graphics.Blit( texture, temporaryRenderTexture );
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

        /// <summary>
        /// 현재 설정에 따라 저장용 텍스처를 비율 유지 리사이즈한다.
        /// </summary>
        private Texture2D CreateResizedTextureIfNeeded( Texture2D sourceTextureToResize )
        {
            if ( sourceTextureToResize == null )
            {
                return null;
            }

            Vector2Int targetSize = CalculateResizeTargetSize( sourceTextureToResize.width, sourceTextureToResize.height );

            if ( targetSize.x == sourceTextureToResize.width && targetSize.y == sourceTextureToResize.height )
            {
                return null;
            }

            Texture2D resizedTexture = ResizeTexture( sourceTextureToResize, targetSize.x, targetSize.y );
            return resizedTexture;
        }

        /// <summary>
        /// 현재 리사이즈 설정에 맞는 목표 크기를 계산한다.
        /// </summary>
        private Vector2Int CalculateResizeTargetSize( int sourceWidth, int sourceHeight )
        {
            if ( resizeMode == eResizeMode.PERCENT )
            {
                float scale = resizeScalePercent / 100.0f;

                if ( allowUpscale == false )
                {
                    scale = Mathf.Min( 1.0f, scale );
                }

                int targetWidth = Mathf.Max( 1, Mathf.RoundToInt( sourceWidth * scale ) );
                int targetHeight = Mathf.Max( 1, Mathf.RoundToInt( sourceHeight * scale ) );
                Vector2Int scaledSize = new Vector2Int( targetWidth, targetHeight );
                return scaledSize;
            }

            float widthRatio = ( float )maxResizeWidth / sourceWidth;
            float heightRatio = ( float )maxResizeHeight / sourceHeight;
            float fitScale = Mathf.Min( widthRatio, heightRatio );

            if ( allowUpscale == false )
            {
                fitScale = Mathf.Min( 1.0f, fitScale );
            }

            int fitWidth = Mathf.Max( 1, Mathf.RoundToInt( sourceWidth * fitScale ) );
            int fitHeight = Mathf.Max( 1, Mathf.RoundToInt( sourceHeight * fitScale ) );
            Vector2Int fitSize = new Vector2Int( fitWidth, fitHeight );
            return fitSize;
        }

        /// <summary>
        /// 최근접 샘플링으로 새 크기의 텍스처를 생성한다.
        /// </summary>
        private Texture2D ResizeTexture( Texture2D sourceTextureToResize, int targetWidth, int targetHeight )
        {
            Texture2D resizedTexture = new Texture2D( targetWidth, targetHeight, TextureFormat.RGBA32, false );
            Color[] resizedPixels = new Color[ targetWidth * targetHeight ];
            int sourceWidth = sourceTextureToResize.width;
            int sourceHeight = sourceTextureToResize.height;

            for ( int y = 0; y < targetHeight; y++ )
            {
                float normalizedY = ( y + 0.5f ) / targetHeight;
                int sourceY = Mathf.Clamp( Mathf.FloorToInt( normalizedY * sourceHeight ), 0, sourceHeight - 1 );

                for ( int x = 0; x < targetWidth; x++ )
                {
                    float normalizedX = ( x + 0.5f ) / targetWidth;
                    int sourceX = Mathf.Clamp( Mathf.FloorToInt( normalizedX * sourceWidth ), 0, sourceWidth - 1 );
                    int pixelIndex = y * targetWidth + x;
                    Color sourceColor = sourceTextureToResize.GetPixel( sourceX, sourceY );
                    resizedPixels[ pixelIndex ] = sourceColor;
                }
            }

            resizedTexture.SetPixels( resizedPixels );
            resizedTexture.Apply();
            return resizedTexture;
        }

        /// <summary>
        /// 출력 폴더 경로를 프로젝트 내부에 생성한다.
        /// </summary>
        private void EnsureOutputFolderExists()
        {
            string fullPath = Path.GetFullPath( outputFolder );

            if ( Directory.Exists( fullPath ) )
            {
                return;
            }

            Directory.CreateDirectory( fullPath );
        }

        /// <summary>
        /// 파일명 수동 입력 여부에 따라 자동 채번 상태를 갱신한다.
        /// </summary>
        private void HandleFileNameChanged( string newFileName )
        {
            if ( string.IsNullOrWhiteSpace( newFileName ) )
            {
                useAutoFileName = true;
                RefreshSuggestedFileName( true );
                return;
            }

            string sanitizedFileName = SanitizeFileName( newFileName );
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

        /// <summary>
        /// 현재 폴더 상태에 맞는 기본 파일명을 계산하고 필요 시 적용한다.
        /// </summary>
        private void RefreshSuggestedFileName( bool applyToField )
        {
            string suggestedFileName = BuildSuggestedFileName();
            lastSuggestedFileName = suggestedFileName;

            if ( applyToField )
            {
                fileName = suggestedFileName;
            }
        }

        /// <summary>
        /// 출력 폴더의 파일 개수를 기준으로 다음 기본 파일명을 생성한다.
        /// </summary>
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

        /// <summary>
        /// 파일명에 사용할 수 없는 문자를 제거한다.
        /// </summary>
        private string SanitizeFileName( string rawName )
        {
            string candidate = string.IsNullOrWhiteSpace( rawName ) ? DefaultFileName : rawName.Trim();
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

        /// <summary>
        /// 상태 메시지와 표시 유형을 갱신한다.
        /// </summary>
        private void SetStatus( string message, MessageType messageType )
        {
            statusMessage = message;
            statusMessageType = messageType;
        }
    }
}
