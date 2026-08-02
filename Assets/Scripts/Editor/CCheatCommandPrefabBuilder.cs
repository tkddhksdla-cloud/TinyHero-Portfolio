using TMPro;
using TinyHero.Core;
using TinyHero.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.Tools.Editor
{
    public static class CCheatCommandPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/UI/Popup/PopupCheatCommand.prefab";

        [MenuItem( "TinyHero/UI/Create Cheat Command Popup" )]
        private static void CreatePrefab()
        {
            GameObject rootObject = CreateRoot();
            GameObject contentRootObject = CreateContentRoot( rootObject.transform );
            GameObject panelObject = CreatePanel( contentRootObject.transform );
            GameObject titleRowObject = CreateRow( panelObject.transform, "TitleRow" );
            TMP_Text titleText = CreateText( titleRowObject.transform, "TitleText", "Cheat Command" );
            titleText.fontSize = 24.0f;
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;
            CButtonEx closeButton = CreateButton( titleRowObject.transform, "CloseButton", "Close" );
            GameObject levelRowObject = CreateRow( panelObject.transform, "LevelRow" );
            TMP_Text levelLabelText = CreateText( levelRowObject.transform, "LevelLabel", "Level" );
            LayoutElement levelLabelLayoutElement = levelLabelText.gameObject.AddComponent<LayoutElement>();
            levelLabelLayoutElement.preferredWidth = 100.0f;
            TMP_InputField levelInputField = CreateInputField( levelRowObject.transform, "LevelInput", "Target Level" );
            CButtonEx applyLevelButton = CreateButton( levelRowObject.transform, "ApplyLevelButton", "Apply" );
            Toggle levelLockToggle = CreateToggle( CreateRow( panelObject.transform, "LevelLockRow" ).transform );
            TMP_InputField itemIdInputField = CreateInputField( CreateRow( panelObject.transform, "ItemIdRow" ).transform, "ItemIdInput", "Item ID" );
            GameObject countRowObject = CreateRow( panelObject.transform, "CountRow" );
            TMP_InputField itemCountInputField = CreateInputField( countRowObject.transform, "ItemCountInput", "1" );
            itemCountInputField.text = "1";
            CButtonEx grantItemButton = CreateButton( countRowObject.transform, "GrantItemButton", "Grant Item" );
            CButtonEx grantAllItemsButton = CreateButton( CreateRow( panelObject.transform, "AllItemRow" ).transform, "GrantAllItemsButton", "Grant All Items" );
            TMP_Text statusText = CreateText( panelObject.transform, "StatusText", string.Empty );
            LayoutElement statusLayoutElement = statusText.gameObject.AddComponent<LayoutElement>();
            statusLayoutElement.preferredHeight = 72.0f;

            CCheatCommandUI cheatCommandUi = rootObject.GetComponent<CCheatCommandUI>();
            SerializedObject serializedObject = new SerializedObject( cheatCommandUi );
            serializedObject.FindProperty( "contentRootObject" ).objectReferenceValue = contentRootObject;
            serializedObject.FindProperty( "levelInputField" ).objectReferenceValue = levelInputField;
            serializedObject.FindProperty( "levelLockToggle" ).objectReferenceValue = levelLockToggle;
            serializedObject.FindProperty( "itemIdInputField" ).objectReferenceValue = itemIdInputField;
            serializedObject.FindProperty( "itemCountInputField" ).objectReferenceValue = itemCountInputField;
            serializedObject.FindProperty( "statusText" ).objectReferenceValue = statusText;
            serializedObject.FindProperty( "applyLevelButton" ).objectReferenceValue = applyLevelButton;
            serializedObject.FindProperty( "grantItemButton" ).objectReferenceValue = grantItemButton;
            serializedObject.FindProperty( "grantAllItemsButton" ).objectReferenceValue = grantAllItemsButton;
            serializedObject.FindProperty( "closeButton" ).objectReferenceValue = closeButton;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset( rootObject, PrefabPath );
            Object.DestroyImmediate( rootObject );
            AssetDatabase.SaveAssets();
        }

        private static GameObject CreateRoot()
        {
            GameObject rootObject = new GameObject( "PopupCheatCommand", typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ), typeof( CCheatCommandUI ) );
            Canvas canvas = rootObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            CanvasScaler canvasScaler = rootObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2( 1920.0f, 1080.0f );
            return rootObject;
        }

        private static GameObject CreateContentRoot( Transform _parentTransform )
        {
            GameObject contentRootObject = CreateObject( "ContentRoot", _parentTransform );
            RectTransform rectTransform = contentRootObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            Image image = contentRootObject.AddComponent<Image>();
            image.color = new Color( 0.0f, 0.0f, 0.0f, 0.7f );
            return contentRootObject;
        }

        private static GameObject CreatePanel( Transform _parentTransform )
        {
            GameObject panelObject = CreateObject( "Panel", _parentTransform );
            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
            rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
            rectTransform.sizeDelta = new Vector2( 600.0f, 470.0f );
            Image image = panelObject.AddComponent<Image>();
            image.color = new Color( 0.12f, 0.12f, 0.16f, 0.96f );
            VerticalLayoutGroup layoutGroup = panelObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset( 20, 20, 20, 20 );
            layoutGroup.spacing = 12.0f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            return panelObject;
        }

        private static GameObject CreateRow( Transform _parentTransform, string _name )
        {
            GameObject rowObject = CreateObject( _name, _parentTransform );
            LayoutElement layoutElement = rowObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 42.0f;
            HorizontalLayoutGroup layoutGroup = rowObject.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10.0f;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;
            return rowObject;
        }

        private static CButtonEx CreateButton( Transform _parentTransform, string _name, string _label )
        {
            GameObject buttonObject = CreateObject( _name, _parentTransform );
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color( 0.24f, 0.47f, 0.82f, 1.0f );
            CButtonEx button = buttonObject.AddComponent<CButtonEx>();
            button.targetGraphic = image;
            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 150.0f;
            TMP_Text labelText = CreateText( buttonObject.transform, "Label", _label );
            RectTransform labelRectTransform = labelText.rectTransform;
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = Vector2.zero;
            labelRectTransform.offsetMax = Vector2.zero;
            labelText.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TMP_InputField CreateInputField( Transform _parentTransform, string _name, string _placeholder )
        {
            GameObject inputObject = CreateObject( _name, _parentTransform );
            inputObject.AddComponent<Image>().color = Color.white;
            TMP_InputField inputField = inputObject.AddComponent<TMP_InputField>();
            inputObject.AddComponent<LayoutElement>().flexibleWidth = 1.0f;
            TMP_Text inputText = CreateText( inputObject.transform, "Text", string.Empty );
            inputText.rectTransform.anchorMin = Vector2.zero;
            inputText.rectTransform.anchorMax = Vector2.one;
            inputText.rectTransform.offsetMin = new Vector2( 12.0f, 6.0f );
            inputText.rectTransform.offsetMax = new Vector2( -12.0f, -6.0f );
            inputText.color = Color.black;
            inputField.textComponent = inputText;
            TMP_Text placeholderText = CreateText( inputObject.transform, "Placeholder", _placeholder );
            placeholderText.rectTransform.anchorMin = Vector2.zero;
            placeholderText.rectTransform.anchorMax = Vector2.one;
            placeholderText.rectTransform.offsetMin = new Vector2( 12.0f, 6.0f );
            placeholderText.rectTransform.offsetMax = new Vector2( -12.0f, -6.0f );
            placeholderText.color = Color.gray;
            inputField.placeholder = placeholderText;
            return inputField;
        }

        private static Toggle CreateToggle( Transform _parentTransform )
        {
            Toggle toggle = DefaultControls.CreateToggle( new DefaultControls.Resources() ).GetComponent<Toggle>();
            toggle.name = "LevelLockToggle";
            toggle.transform.SetParent( _parentTransform, false );
            TMP_Text labelText = CreateText( toggle.transform, "Label", "Level Lock" );
            RectTransform labelRectTransform = labelText.rectTransform;
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = new Vector2( 28.0f, 0.0f );
            labelRectTransform.offsetMax = Vector2.zero;
            return toggle;
        }

        private static TMP_Text CreateText( Transform _parentTransform, string _name, string _value )
        {
            GameObject textObject = CreateObject( _name, _parentTransform );
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = _value;
            text.fontSize = 18.0f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateObject( string _name, Transform _parentTransform )
        {
            GameObject result = new GameObject( _name, typeof( RectTransform ) );
            result.transform.SetParent( _parentTransform, false );
            return result;
        }
    }
}
