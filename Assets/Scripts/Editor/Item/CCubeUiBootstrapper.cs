using TMPro;
using TinyHero.Player;
using TinyHero.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.Tools
{
    ///<summary>
    /// 큐브 UI 프리팹 생성 도구
    ///</summary>
    public static class CCubeUiBootstrapper
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/UI/Popup/PopupCube.prefab";
        private const string CubeItemId = "ITEM_CONSUMABLE_CUBE";
        private const string SampleEquipmentItemId = "ITEM_EQUIPMENT_BRONZE_SWORD";

        ///<summary>
        /// 큐브 UI 생성 메뉴 실행
        ///</summary>
        [MenuItem( "TinyHero/Item/Generate Cube UI" )]
        public static void GenerateCubeUiMenu()
        {
            string result = GenerateCubeUi();
            Debug.Log( result );
        }

        ///<summary>
        /// 큐브 UI 프리팹 및 씬 인스턴스 생성
        ///</summary>
        public static string GenerateCubeUi()
        {
            EnsureFolderStructure();
            GameObject prefabRoot = CreateCubeUiPrefabRoot();
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset( prefabRoot, PrefabPath );
            Object.DestroyImmediate( prefabRoot );
            GameObject interactionCanvasObject = FindInteractionCanvasObject();

            if ( interactionCanvasObject == null )
            {
                return "Cube UI prefab created. Loaded scenes do not contain Canvas_InteractionUI, so scene instance placement was skipped.";
            }

            Transform existingTransform = interactionCanvasObject.transform.Find( "PopupCube" );

            if ( existingTransform != null )
            {
                Object.DestroyImmediate( existingTransform.gameObject );
            }

            GameObject instanceObject = PrefabUtility.InstantiatePrefab( savedPrefab, interactionCanvasObject.scene ) as GameObject;
            instanceObject.name = "PopupCube";
            instanceObject.transform.SetParent( interactionCanvasObject.transform, false );
            instanceObject.SetActive( false );
            EnsureSampleInventoryState();
            EditorSceneManager.MarkSceneDirty( interactionCanvasObject.scene );
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "Cube UI prefab created and scene instance refreshed.";
        }

        ///<summary>
        /// 상호작용 캔버스 오브젝트 탐색
        ///</summary>
        private static GameObject FindInteractionCanvasObject()
        {
            Transform[] transformArray = Resources.FindObjectsOfTypeAll<Transform>();

            for ( int index = 0; index < transformArray.Length; index++ )
            {
                Transform targetTransform = transformArray[ index ];

                if ( targetTransform == null )
                {
                    continue;
                }

                if ( string.Equals( targetTransform.name, "Canvas_InteractionUI", System.StringComparison.Ordinal ) == false )
                {
                    continue;
                }

                GameObject targetObject = targetTransform.gameObject;

                if ( targetObject.scene.IsValid() == false || targetObject.scene.isLoaded == false )
                {
                    continue;
                }

                return targetObject;
            }

            return null;
        }

        ///<summary>
        /// 큐브 UI 프리팹 루트 생성
        ///</summary>
        private static GameObject CreateCubeUiPrefabRoot()
        {
            GameObject rootObject = new GameObject( "PopupCube", typeof( RectTransform ) );
            RectTransform rootRectTransform = rootObject.GetComponent<RectTransform>();
            ConfigureCenteredRect( rootRectTransform, new Vector2( 1020.0f, 690.0f ), Vector2.zero );

            GameObject panelObject = CreatePanelObject( "Panel", rootObject.transform, new Vector2( 1020.0f, 690.0f ), Vector2.zero, new Color32( 18, 23, 32, 245 ) );
            CreateOutline( panelObject, new Color32( 72, 102, 156, 255 ), new Vector2( 2.0f, -2.0f ) );
            RectTransform panelRectTransform = panelObject.GetComponent<RectTransform>();

            GameObject titleBandObject = CreatePanelObject( "TitleBand", panelObject.transform, new Vector2( 920.0f, 74.0f ), new Vector2( 0.0f, 282.0f ), new Color32( 28, 36, 50, 255 ) );
            CreateTextObject( "TitleText", titleBandObject.transform, "Mystic Cube", 34, TextAlignmentOptions.Center, new Vector2( 320.0f, 44.0f ), Vector2.zero, Color.white );

            GameObject dropSlotObject = CreatePanelObject( "EquipmentDropSlot", panelObject.transform, new Vector2( 820.0f, 132.0f ), new Vector2( 0.0f, 190.0f ), new Color32( 32, 40, 55, 255 ) );
            CreateOutline( dropSlotObject, new Color32( 99, 141, 210, 255 ), new Vector2( 2.0f, -2.0f ) );
            CreateTextObject( "DropSlotTitleText", dropSlotObject.transform, "CUBE TARGET", 18, TextAlignmentOptions.Center, new Vector2( 180.0f, 24.0f ), new Vector2( 0.0f, 43.0f ), new Color32( 140, 176, 230, 255 ) );
            CreateTextObject( "SelectedItemNameText", dropSlotObject.transform, "장비를 여기에 드래그", 24, TextAlignmentOptions.Center, new Vector2( 460.0f, 30.0f ), new Vector2( 58.0f, 8.0f ), Color.white );
            CreateTextObject( "SelectedItemHintText", dropSlotObject.transform, "인벤토리 또는 장착 창의 장비를 끌어와 큐브 대상을 지정하세요.", 18, TextAlignmentOptions.Center, new Vector2( 620.0f, 24.0f ), new Vector2( 58.0f, -24.0f ), new Color32( 191, 200, 214, 255 ) );
            CreateTextObject( "SelectedItemSourceText", dropSlotObject.transform, "대상 미선택", 18, TextAlignmentOptions.Center, new Vector2( 280.0f, 24.0f ), new Vector2( 250.0f, 42.0f ), new Color32( 255, 210, 120, 255 ) );

            GameObject iconFrameObject = CreatePanelObject( "SelectedItemIconFrame", dropSlotObject.transform, new Vector2( 84.0f, 84.0f ), new Vector2( -334.0f, 0.0f ), new Color32( 56, 64, 82, 255 ) );
            CreateOutline( iconFrameObject, new Color32( 94, 107, 132, 255 ), new Vector2( 1.0f, -1.0f ) );
            GameObject iconObject = new GameObject( "SelectedItemIconImage", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
            iconRectTransform.SetParent( iconFrameObject.transform, false );
            ConfigureCenteredRect( iconRectTransform, new Vector2( 66.0f, 66.0f ), Vector2.zero );
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.enabled = false;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            GameObject currentPanelObject = CreatePanelObject( "CurrentPanel", panelObject.transform, new Vector2( 430.0f, 280.0f ), new Vector2( -235.0f, -10.0f ), new Color32( 29, 36, 48, 255 ) );
            CreateOutline( currentPanelObject, new Color32( 77, 86, 103, 255 ), new Vector2( 1.0f, -1.0f ) );
            CButtonEx currentPanelButton = currentPanelObject.AddComponent<CButtonEx>();
            currentPanelButton.targetGraphic = currentPanelObject.GetComponent<Image>();
            GameObject previewPanelObject = CreatePanelObject( "PreviewPanel", panelObject.transform, new Vector2( 430.0f, 280.0f ), new Vector2( 235.0f, -10.0f ), new Color32( 29, 36, 48, 255 ) );
            CreateOutline( previewPanelObject, new Color32( 77, 86, 103, 255 ), new Vector2( 1.0f, -1.0f ) );
            CButtonEx previewPanelButton = previewPanelObject.AddComponent<CButtonEx>();
            previewPanelButton.targetGraphic = previewPanelObject.GetComponent<Image>();

            CreateTextObject( "CurrentLabel", currentPanelObject.transform, "CURRENT", 22, TextAlignmentOptions.Center, new Vector2( 160.0f, 32.0f ), new Vector2( 0.0f, 106.0f ), new Color32( 154, 182, 230, 255 ) );
            CreateTextObject( "PreviewLabel", previewPanelObject.transform, "PREVIEW", 22, TextAlignmentOptions.Center, new Vector2( 160.0f, 32.0f ), new Vector2( 0.0f, 106.0f ), new Color32( 255, 212, 133, 255 ) );
            CreateTextObject( "CurrentRankText", currentPanelObject.transform, "-", 24, TextAlignmentOptions.Center, new Vector2( 320.0f, 30.0f ), new Vector2( 0.0f, 68.0f ), Color.white );
            CreateTextObject( "PreviewRankText", previewPanelObject.transform, "-", 24, TextAlignmentOptions.Center, new Vector2( 320.0f, 30.0f ), new Vector2( 0.0f, 68.0f ), Color.white );
            CreateTextObject( "CurrentLine1Text", currentPanelObject.transform, "-", 20, TextAlignmentOptions.Left, new Vector2( 350.0f, 28.0f ), new Vector2( 0.0f, 18.0f ), Color.white );
            CreateTextObject( "CurrentLine2Text", currentPanelObject.transform, "-", 20, TextAlignmentOptions.Left, new Vector2( 350.0f, 28.0f ), new Vector2( 0.0f, -20.0f ), Color.white );
            CreateTextObject( "CurrentLine3Text", currentPanelObject.transform, "-", 20, TextAlignmentOptions.Left, new Vector2( 350.0f, 28.0f ), new Vector2( 0.0f, -58.0f ), Color.white );
            CreateTextObject( "PreviewLine1Text", previewPanelObject.transform, "-", 20, TextAlignmentOptions.Left, new Vector2( 350.0f, 28.0f ), new Vector2( 0.0f, 18.0f ), Color.white );
            CreateTextObject( "PreviewLine2Text", previewPanelObject.transform, "-", 20, TextAlignmentOptions.Left, new Vector2( 350.0f, 28.0f ), new Vector2( 0.0f, -20.0f ), Color.white );
            CreateTextObject( "PreviewLine3Text", previewPanelObject.transform, "-", 20, TextAlignmentOptions.Left, new Vector2( 350.0f, 28.0f ), new Vector2( 0.0f, -58.0f ), Color.white );

            CreateButtonObject( "RerollButton", panelObject.transform, "Reroll", new Vector2( 260.0f, 56.0f ), new Vector2( 0.0f, -262.0f ), new Color32( 54, 84, 146, 255 ) );
            CreateButtonObject( "CloseButton", panelObject.transform, "X", new Vector2( 52.0f, 52.0f ), new Vector2( 452.0f, 286.0f ), new Color32( 72, 52, 58, 255 ) );
            CreateTextObject( "StatusText", panelObject.transform, "기본 선택은 없습니다. 장착 여부와 관계없이 장비를 드래그해서 지정하세요.", 20, TextAlignmentOptions.Center, new Vector2( 820.0f, 50.0f ), new Vector2( 0.0f, -332.0f ), new Color32( 217, 223, 232, 255 ) );

            PopupCube controller = rootObject.AddComponent<PopupCube>();
            BindControllerReferences( controller, panelRectTransform );
            return rootObject;
        }

        ///<summary>
        /// 패널 오브젝트 생성
        ///</summary>
        private static GameObject CreatePanelObject( string _name, Transform _parent, Vector2 _size, Vector2 _anchoredPosition, Color _color )
        {
            GameObject panelObject = new GameObject( _name, typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parent, false );
            ConfigureCenteredRect( rectTransform, _size, _anchoredPosition );
            Image image = panelObject.GetComponent<Image>();
            image.color = _color;
            return panelObject;
        }

        ///<summary>
        /// 외곽선 컴포넌트 생성
        ///</summary>
        private static void CreateOutline( GameObject _targetObject, Color _color, Vector2 _effectDistance )
        {
            if ( _targetObject == null )
            {
                return;
            }

            Outline outline = _targetObject.AddComponent<Outline>();
            outline.effectColor = _color;
            outline.effectDistance = _effectDistance;
        }

        ///<summary>
        /// 텍스트 오브젝트 생성
        ///</summary>
        private static TextMeshProUGUI CreateTextObject( string _name, Transform _parent, string _text, int _fontSize, TextAlignmentOptions _alignment, Vector2 _size, Vector2 _anchoredPosition, Color _color )
        {
            GameObject textObject = new GameObject( _name, typeof( RectTransform ) );
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent( _parent, false );
            ConfigureCenteredRect( rectTransform, _size, _anchoredPosition );
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.font = TMP_Settings.defaultFontAsset;
            textComponent.fontSize = _fontSize;
            textComponent.alignment = _alignment;
            textComponent.text = _text;
            textComponent.color = _color;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        ///<summary>
        /// 버튼 오브젝트 생성
        ///</summary>
        private static CButtonEx CreateButtonObject( string _name, Transform _parent, string _label, Vector2 _size, Vector2 _anchoredPosition, Color _color )
        {
            GameObject buttonObject = CreatePanelObject( _name, _parent, _size, _anchoredPosition, _color );
            CreateOutline( buttonObject, new Color32( 20, 24, 30, 180 ), new Vector2( 1.0f, -1.0f ) );
            CButtonEx button = buttonObject.AddComponent<CButtonEx>();
            Image image = buttonObject.GetComponent<Image>();
            button.targetGraphic = image;
            CreateTextObject( "Label", buttonObject.transform, _label, 20, TextAlignmentOptions.Center, _size, Vector2.zero, Color.white );
            return button;
        }

        ///<summary>
        /// 중앙 기준 RectTransform 설정
        ///</summary>
        private static void ConfigureCenteredRect( RectTransform _rectTransform, Vector2 _size, Vector2 _anchoredPosition )
        {
            if ( _rectTransform == null )
            {
                return;
            }

            _rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
            _rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
            _rectTransform.pivot = new Vector2( 0.5f, 0.5f );
            _rectTransform.sizeDelta = _size;
            _rectTransform.anchoredPosition = _anchoredPosition;
        }

        ///<summary>
        /// 큐브 UI 컨트롤러 참조 바인딩
        ///</summary>
        private static void BindControllerReferences( PopupCube _controller, RectTransform _panelRectTransform )
        {
            if ( _controller == null )
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject( _controller );
            Transform rootTransform = _controller.transform;
            serializedObject.FindProperty( "windowRootRectTransform" ).objectReferenceValue = _panelRectTransform;
            serializedObject.FindProperty( "windowDragHandleRectTransform" ).objectReferenceValue = _panelRectTransform;
            serializedObject.FindProperty( "closeButton" ).objectReferenceValue = rootTransform.Find( "Panel/CloseButton" ).GetComponent<CButtonEx>();
            serializedObject.FindProperty( "rerollButton" ).objectReferenceValue = rootTransform.Find( "Panel/RerollButton" ).GetComponent<CButtonEx>();
            serializedObject.FindProperty( "equipmentDropSlotRectTransform" ).objectReferenceValue = rootTransform.Find( "Panel/EquipmentDropSlot" ).GetComponent<RectTransform>();
            serializedObject.FindProperty( "selectedItemIconImage" ).objectReferenceValue = rootTransform.Find( "Panel/EquipmentDropSlot/SelectedItemIconFrame/SelectedItemIconImage" ).GetComponent<Image>();
            serializedObject.FindProperty( "selectedItemFrameImage" ).objectReferenceValue = rootTransform.Find( "Panel/EquipmentDropSlot/SelectedItemIconFrame" ).GetComponent<Image>();
            serializedObject.FindProperty( "selectedItemNameText" ).objectReferenceValue = rootTransform.Find( "Panel/EquipmentDropSlot/SelectedItemNameText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "selectedItemHintText" ).objectReferenceValue = rootTransform.Find( "Panel/EquipmentDropSlot/SelectedItemHintText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "selectedItemSourceText" ).objectReferenceValue = rootTransform.Find( "Panel/EquipmentDropSlot/SelectedItemSourceText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "currentPanelButton" ).objectReferenceValue = rootTransform.Find( "Panel/CurrentPanel" ).GetComponent<CButtonEx>();
            serializedObject.FindProperty( "previewPanelButton" ).objectReferenceValue = rootTransform.Find( "Panel/PreviewPanel" ).GetComponent<CButtonEx>();
            serializedObject.FindProperty( "cubeNameText" ).objectReferenceValue = rootTransform.Find( "Panel/TitleBand/TitleText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "currentRankText" ).objectReferenceValue = rootTransform.Find( "Panel/CurrentPanel/CurrentRankText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "previewRankText" ).objectReferenceValue = rootTransform.Find( "Panel/PreviewPanel/PreviewRankText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "currentLine1Text" ).objectReferenceValue = rootTransform.Find( "Panel/CurrentPanel/CurrentLine1Text" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "currentLine2Text" ).objectReferenceValue = rootTransform.Find( "Panel/CurrentPanel/CurrentLine2Text" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "currentLine3Text" ).objectReferenceValue = rootTransform.Find( "Panel/CurrentPanel/CurrentLine3Text" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "previewLine1Text" ).objectReferenceValue = rootTransform.Find( "Panel/PreviewPanel/PreviewLine1Text" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "previewLine2Text" ).objectReferenceValue = rootTransform.Find( "Panel/PreviewPanel/PreviewLine2Text" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "previewLine3Text" ).objectReferenceValue = rootTransform.Find( "Panel/PreviewPanel/PreviewLine3Text" ).GetComponent<TextMeshProUGUI>();
            serializedObject.FindProperty( "statusText" ).objectReferenceValue = rootTransform.Find( "Panel/StatusText" ).GetComponent<TextMeshProUGUI>();
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty( _controller );
        }

        ///<summary>
        /// 샘플 인벤토리 상태 보장
        ///</summary>
        private static void EnsureSampleInventoryState()
        {
            CPlayerInventoryManager inventoryManager = Object.FindFirstObjectByType<CPlayerInventoryManager>();

            if ( inventoryManager == null )
            {
                return;
            }

            if ( inventoryManager.HasItem( CubeItemId, 1 ) == false )
            {
                inventoryManager.TryAddItemById( CubeItemId, 5 );
            }

            if ( inventoryManager.HasItem( SampleEquipmentItemId, 1 ) == false )
            {
                inventoryManager.TryAddItemById( SampleEquipmentItemId, 1 );
            }

            EditorUtility.SetDirty( inventoryManager );
        }

        ///<summary>
        /// 큐브 UI 폴더 구조 보장
        ///</summary>
        private static void EnsureFolderStructure()
        {
            EnsureFolder( "Assets", "Resources" );
            EnsureFolder( "Assets/Resources", "Prefabs" );
            EnsureFolder( "Assets/Resources/Prefabs", "UI" );
            EnsureFolder( "Assets/Resources/Prefabs/UI", "Popup" );
        }

        ///<summary>
        /// 폴더 생성 보장
        ///</summary>
        private static string EnsureFolder( string _parentPath, string _folderName )
        {
            string folderPath = $"{_parentPath}/{_folderName}";

            if ( AssetDatabase.IsValidFolder( folderPath ) )
            {
                return folderPath;
            }

            AssetDatabase.CreateFolder( _parentPath, _folderName );
            return folderPath;
        }
    }
}
