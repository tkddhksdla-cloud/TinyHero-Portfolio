using TMPro;
using TinyHero.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.Tools.Editor
{
    ///<summary>
    /// 콘텐츠 다운로드 진행 팝업 프리팹 생성 도구
    ///</summary>
    public static class CContentDownloadPopupBootstrapper
    {
        private const string MenuPath = "TinyHero/UI/Build Content Download Popup";
        private const string SourcePrefabPath = "Assets/Resources/Prefabs/UI/Popup/PopupCommonNotice.prefab";
        private const string TargetPrefabPath = "Assets/Resources/Prefabs/UI/Popup/PopupContentDownload.prefab";

        [MenuItem( MenuPath )]
        public static void BuildFromMenu()
        {
            bool isBuilt = Build();

            if ( isBuilt )
            {
                Debug.Log( $"[ UI ] Content download popup built: {TargetPrefabPath}" );
            }
        }

        public static bool Build()
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>( SourcePrefabPath );

            if ( sourcePrefab == null )
            {
                Debug.LogError( $"[ UI ] Source popup prefab was not found: {SourcePrefabPath}" );
                return false;
            }

            if ( AssetDatabase.LoadAssetAtPath<GameObject>( TargetPrefabPath ) == null )
            {
                bool isCopied = AssetDatabase.CopyAsset( SourcePrefabPath, TargetPrefabPath );

                if ( isCopied == false )
                {
                    Debug.LogError( $"[ UI ] Content download popup copy failed: {TargetPrefabPath}" );
                    return false;
                }
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents( TargetPrefabPath );

            if ( prefabRoot == null )
            {
                return false;
            }

            try
            {
                ConfigurePrefab( prefabRoot );
                PrefabUtility.SaveAsPrefabAsset( prefabRoot, TargetPrefabPath );
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents( prefabRoot );
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static void ConfigurePrefab( GameObject _prefabRoot )
        {
            _prefabRoot.name = "PopupContentDownload";
            CPopupCommonNotice commonNotice = _prefabRoot.GetComponent<CPopupCommonNotice>();

            if ( commonNotice != null )
            {
                Object.DestroyImmediate( commonNotice, true );
            }

            PopupContentDownload contentDownloadPopup = _prefabRoot.GetComponent<PopupContentDownload>();

            if ( contentDownloadPopup == null )
            {
                contentDownloadPopup = _prefabRoot.AddComponent<PopupContentDownload>();
            }

            Transform popupTransform = _prefabRoot.transform.Find( "Popup" );
            Transform windowTransform = popupTransform != null ? popupTransform.Find( "BG" ) : null;

            if ( popupTransform == null || windowTransform == null )
            {
                Debug.LogError( "[ UI ] PopupCommonNotice hierarchy is invalid." );
                return;
            }

            Transform descriptionTransform = windowTransform.Find( "Desc" );
            Transform dragHandleTransform = windowTransform.Find( "WindowDragHandle" );
            Transform buttonAreaTransform = windowTransform.Find( "ButtonArea" );
            Transform closeButtonTransform = popupTransform.Find( "ButtonClose" );

            if ( buttonAreaTransform != null )
            {
                buttonAreaTransform.gameObject.SetActive( false );
            }

            if ( closeButtonTransform != null )
            {
                closeButtonTransform.gameObject.SetActive( false );
            }

            TMP_Text descriptionText = descriptionTransform != null ? descriptionTransform.GetComponent<TMP_Text>() : null;
            ConfigureDescriptionText( descriptionText );
            Image progressFillImage = BuildProgressGauge( windowTransform );
            TMP_Text progressText = BuildProgressText( windowTransform );
            SerializedObject serializedPopup = new SerializedObject( contentDownloadPopup );
            serializedPopup.FindProperty( "popupRootRectTransform" ).objectReferenceValue = _prefabRoot.transform as RectTransform;
            serializedPopup.FindProperty( "windowRootRectTransform" ).objectReferenceValue = windowTransform as RectTransform;
            serializedPopup.FindProperty( "windowDragHandleRectTransform" ).objectReferenceValue = dragHandleTransform as RectTransform;
            serializedPopup.FindProperty( "descriptionText" ).objectReferenceValue = descriptionText;
            serializedPopup.FindProperty( "progressText" ).objectReferenceValue = progressText;
            serializedPopup.FindProperty( "progressFillImage" ).objectReferenceValue = progressFillImage;
            serializedPopup.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( contentDownloadPopup );
        }

        private static void ConfigureDescriptionText( TMP_Text _descriptionText )
        {
            if ( _descriptionText == null )
            {
                return;
            }

            _descriptionText.text = "업데이트 파일을 다운로드하고 있습니다.";
            _descriptionText.alignment = TextAlignmentOptions.Center;
            _descriptionText.enableWordWrapping = true;
            RectTransform descriptionRectTransform = _descriptionText.rectTransform;
            descriptionRectTransform.anchorMin = new Vector2( 0.1f, 0.55f );
            descriptionRectTransform.anchorMax = new Vector2( 0.9f, 0.78f );
            descriptionRectTransform.offsetMin = Vector2.zero;
            descriptionRectTransform.offsetMax = Vector2.zero;
            ContentSizeFitter contentSizeFitter = _descriptionText.GetComponent<ContentSizeFitter>();

            if ( contentSizeFitter != null )
            {
                Object.DestroyImmediate( contentSizeFitter, true );
            }
        }

        private static Image BuildProgressGauge( Transform _windowTransform )
        {
            Transform existingGaugeTransform = _windowTransform.Find( "ProgressGauge" );
            GameObject gaugeObject = existingGaugeTransform != null ? existingGaugeTransform.gameObject : new GameObject( "ProgressGauge", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform gaugeRectTransform = gaugeObject.transform as RectTransform;
            gaugeRectTransform.SetParent( _windowTransform, false );
            gaugeRectTransform.anchorMin = new Vector2( 0.12f, 0.34f );
            gaugeRectTransform.anchorMax = new Vector2( 0.88f, 0.44f );
            gaugeRectTransform.offsetMin = Vector2.zero;
            gaugeRectTransform.offsetMax = Vector2.zero;
            Image gaugeBackgroundImage = gaugeObject.GetComponent<Image>();
            gaugeBackgroundImage.color = new Color( 0.05f, 0.08f, 0.12f, 0.9f );

            Transform existingFillTransform = gaugeObject.transform.Find( "Fill" );
            GameObject fillObject = existingFillTransform != null ? existingFillTransform.gameObject : new GameObject( "Fill", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform fillRectTransform = fillObject.transform as RectTransform;
            fillRectTransform.SetParent( gaugeObject.transform, false );
            fillRectTransform.anchorMin = new Vector2( 0.02f, 0.15f );
            fillRectTransform.anchorMax = new Vector2( 0.98f, 0.85f );
            fillRectTransform.offsetMin = Vector2.zero;
            fillRectTransform.offsetMax = Vector2.zero;
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = new Color( 0.2f, 0.75f, 1.0f, 1.0f );
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0.0f;
            return fillImage;
        }

        private static TMP_Text BuildProgressText( Transform _windowTransform )
        {
            Transform existingProgressTextTransform = _windowTransform.Find( "ProgressText" );
            GameObject progressTextObject = existingProgressTextTransform != null ? existingProgressTextTransform.gameObject : new GameObject( "ProgressText", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( TextMeshProUGUI ) );
            RectTransform progressTextRectTransform = progressTextObject.transform as RectTransform;
            progressTextRectTransform.SetParent( _windowTransform, false );
            progressTextRectTransform.anchorMin = new Vector2( 0.1f, 0.18f );
            progressTextRectTransform.anchorMax = new Vector2( 0.9f, 0.3f );
            progressTextRectTransform.offsetMin = Vector2.zero;
            progressTextRectTransform.offsetMax = Vector2.zero;
            TMP_Text progressText = progressTextObject.GetComponent<TMP_Text>();
            progressText.font = TMP_Settings.defaultFontAsset;
            progressText.fontSize = 26.0f;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.white;
            progressText.text = "0 B / 0 B  (0%)";
            return progressText;
        }
    }
}
