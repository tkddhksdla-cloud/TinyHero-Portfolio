using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyHero.Tools
{
    ///<summary>
    /// 스프라이트 리사이즈 저장 에디터 창
    ///</summary>
    public sealed class SpriteRegionPngExporterWindow : EditorWindow
    {
        private const float DefaultResizeScalePercent = 50.0f;
        private const int DefaultMaxResizeWidth = 128;
        private const int DefaultMaxResizeHeight = 128;

        private enum eResizeMode
        {
            PERCENT,
            VALUE
        }

        [SerializeField] private Sprite sourceSprite;
        [SerializeField] private eResizeMode resizeMode = eResizeMode.PERCENT;
        [SerializeField] private float resizeScalePercent = DefaultResizeScalePercent;
        [SerializeField] private int maxResizeWidth = DefaultMaxResizeWidth;
        [SerializeField] private int maxResizeHeight = DefaultMaxResizeHeight;
        [SerializeField] private bool allowUpscale;

        private Vector2 scrollPosition;
        private string statusMessage = "원본 스프라이트를 지정한 뒤 리사이즈 저장을 실행하세요.";
        private MessageType statusMessageType = MessageType.Info;

        ///<summary>
        /// 에디터 창 표시
        ///</summary>
        [MenuItem( "Tools/TinyHero/Sprite Resize PNG Exporter" )]
        private static void ShowWindow()
        {
            SpriteRegionPngExporterWindow window = GetWindow<SpriteRegionPngExporterWindow>();
            window.titleContent = new GUIContent( "Sprite Resize Exporter" );
            window.minSize = new Vector2( 460.0f, 360.0f );
            window.Show();
        }

        ///<summary>
        /// 에디터 GUI 렌더링
        ///</summary>
        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField( "Sprite Resize Exporter", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( "원본 스프라이트를 선택하면 현재 파일은 같은 폴더에 '_Origin' 백업으로 보관하고, 리사이즈된 PNG는 원본 경로에 다시 저장합니다.", MessageType.None );
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
            sourceSprite = ( Sprite )EditorGUILayout.ObjectField( "Sprite", sourceSprite, typeof( Sprite ), false );

            if ( sourceSprite == null )
            {
                EditorGUILayout.HelpBox( "리사이즈할 원본 스프라이트를 지정하세요.", MessageType.Info );
                return;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath( sourceSprite );
            string originAssetPath = GetOriginAssetPath( sourceAssetPath );
            EditorGUILayout.LabelField( "Asset Path", sourceAssetPath );
            EditorGUILayout.LabelField( "Backup Path", originAssetPath );
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

            if ( sourceSprite == null )
            {
                EditorGUILayout.HelpBox( "원본 스프라이트를 지정하면 리사이즈 결과를 미리 확인할 수 있습니다.", MessageType.None );
                return;
            }

            if ( IsSingleSpriteAsset( sourceSprite ) == false )
            {
                EditorGUILayout.HelpBox( "스프라이트 시트의 일부 영역은 현재 원본 덮어쓰기를 지원하지 않습니다. 단일 스프라이트 파일을 사용하세요.", MessageType.Warning );
                return;
            }

            int sourceWidth = Mathf.RoundToInt( sourceSprite.rect.width );
            int sourceHeight = Mathf.RoundToInt( sourceSprite.rect.height );
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

            if ( GUILayout.Button( "리사이즈 저장" ) )
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
            if ( sourceSprite == null )
            {
                string result = "원본 스프라이트를 먼저 지정하세요.";
                return result;
            }

            string sourceAssetPath = AssetDatabase.GetAssetPath( sourceSprite );

            if ( string.IsNullOrWhiteSpace( sourceAssetPath ) )
            {
                string result = "원본 스프라이트의 에셋 경로를 찾을 수 없습니다.";
                return result;
            }

            bool isAssetsFolder = sourceAssetPath.StartsWith( "Assets", StringComparison.Ordinal );

            if ( isAssetsFolder == false )
            {
                string result = "원본 스프라이트는 Assets 하위 경로에 있어야 합니다.";
                return result;
            }

            if ( IsSingleSpriteAsset( sourceSprite ) == false )
            {
                string result = "스프라이트 시트의 일부 영역은 현재 덮어쓰기를 지원하지 않습니다.";
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

            Texture2D readableTexture = CreateReadableTexture( sourceSprite );

            if ( readableTexture == null )
            {
                SetStatus( "원본 스프라이트를 읽을 수 없습니다.", MessageType.Error );
                return;
            }

            Texture2D resizedTexture = null;

            try
            {
                resizedTexture = CreateResizedTextureIfNeeded( readableTexture );
                Texture2D exportTexture = resizedTexture != null ? resizedTexture : readableTexture;
                string sourceAssetPath = AssetDatabase.GetAssetPath( sourceSprite );
                HandleOriginTextureBackup( sourceAssetPath );
                byte[] pngBytes = exportTexture.EncodeToPNG();
                string fullSourcePath = GetFullAssetPath( sourceAssetPath );
                File.WriteAllBytes( fullSourcePath, pngBytes );
                AssetDatabase.ImportAsset( sourceAssetPath, ImportAssetOptions.ForceUpdate );
                AssetDatabase.Refresh();
                SetStatus( $"리사이즈 스프라이트 저장 완료: {sourceAssetPath}", MessageType.Info );
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
        /// 스프라이트 기준 읽기 가능 텍스처 생성
        ///</summary>
        private Texture2D CreateReadableTexture( Sprite _sourceSprite )
        {
            if ( _sourceSprite == null )
            {
                return null;
            }

            Texture2D sourceTexture = _sourceSprite.texture;
            Texture2D readableSourceTexture = CreateReadableTextureFromTexture( sourceTexture );

            if ( readableSourceTexture == null )
            {
                return null;
            }

            Texture2D croppedTexture = null;

            try
            {
                croppedTexture = ExtractSpriteTexture( readableSourceTexture, _sourceSprite );
            }
            finally
            {
                DestroyImmediate( readableSourceTexture );
            }

            return croppedTexture;
        }

        ///<summary>
        /// 텍스처 기준 읽기 가능 텍스처 생성
        ///</summary>
        private Texture2D CreateReadableTextureFromTexture( Texture2D _texture )
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
        /// 스프라이트 영역 텍스처 추출
        ///</summary>
        private Texture2D ExtractSpriteTexture( Texture2D _readableSourceTexture, Sprite _sourceSprite )
        {
            if ( _readableSourceTexture == null || _sourceSprite == null )
            {
                return null;
            }

            Rect textureRect = _sourceSprite.textureRect;
            int sourceX = Mathf.RoundToInt( textureRect.x );
            int sourceY = Mathf.RoundToInt( textureRect.y );
            int sourceWidth = Mathf.RoundToInt( textureRect.width );
            int sourceHeight = Mathf.RoundToInt( textureRect.height );
            Color[] sourcePixelArray = _readableSourceTexture.GetPixels( sourceX, sourceY, sourceWidth, sourceHeight );
            Texture2D extractedTexture = new Texture2D( sourceWidth, sourceHeight, TextureFormat.RGBA32, false );
            extractedTexture.SetPixels( sourcePixelArray );
            extractedTexture.Apply();
            return extractedTexture;
        }

        ///<summary>
        /// 리사이즈 텍스처 조건부 생성
        ///</summary>
        private Texture2D CreateResizedTextureIfNeeded( Texture2D _sourceTextureToResize )
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
        private Vector2Int CalculateResizeTargetSize( int _sourceWidth, int _sourceHeight )
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
        private Texture2D ResizeTexture( Texture2D _sourceTextureToResize, int _targetWidth, int _targetHeight )
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
        /// 원본 백업 PNG 생성
        ///</summary>
        private void HandleOriginTextureBackup( string _sourceAssetPath )
        {
            if ( string.IsNullOrWhiteSpace( _sourceAssetPath ) )
            {
                return;
            }

            string originAssetPath = GetOriginAssetPath( _sourceAssetPath );
            string fullOriginPath = GetFullAssetPath( originAssetPath );

            if ( File.Exists( fullOriginPath ) )
            {
                return;
            }

            string fullSourcePath = GetFullAssetPath( _sourceAssetPath );
            File.Copy( fullSourcePath, fullOriginPath );
            AssetDatabase.ImportAsset( originAssetPath, ImportAssetOptions.ForceUpdate );
        }

        ///<summary>
        /// 원본 백업 경로 구성
        ///</summary>
        private string GetOriginAssetPath( string _sourceAssetPath )
        {
            string directoryPath = Path.GetDirectoryName( _sourceAssetPath );
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension( _sourceAssetPath );
            string extension = Path.GetExtension( _sourceAssetPath );
            string originFileName = $"{fileNameWithoutExtension}_Origin{extension}";
            string result = string.IsNullOrWhiteSpace( directoryPath ) ? originFileName : Path.Combine( directoryPath, originFileName );
            return result.Replace( '\\', '/' );
        }

        ///<summary>
        /// 에셋 절대 경로 구성
        ///</summary>
        private string GetFullAssetPath( string _assetPath )
        {
            string result = Path.GetFullPath( _assetPath );
            return result;
        }

        ///<summary>
        /// 단일 스프라이트 에셋 여부 판정
        ///</summary>
        private bool IsSingleSpriteAsset( Sprite _sourceSprite )
        {
            if ( _sourceSprite == null || _sourceSprite.texture == null )
            {
                return false;
            }

            int spriteWidth = Mathf.RoundToInt( _sourceSprite.rect.width );
            int spriteHeight = Mathf.RoundToInt( _sourceSprite.rect.height );
            bool isFullWidth = spriteWidth == _sourceSprite.texture.width;
            bool isFullHeight = spriteHeight == _sourceSprite.texture.height;
            bool result = isFullWidth && isFullHeight;
            return result;
        }

        ///<summary>
        /// 상태 메시지 설정
        ///</summary>
        private void SetStatus( string _message, MessageType _messageType )
        {
            statusMessage = _message;
            statusMessageType = _messageType;
        }
    }
}
