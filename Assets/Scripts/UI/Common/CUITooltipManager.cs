using TinyHero.Core;
using TinyHero.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// UI 툴팁 공용 표시 매니저
    ///</summary>
    public sealed class CUITooltipManager : CSingleTon<CUITooltipManager>
    {
        private const string TooltipCanvasObjectName = "Canvas_Tooltip";
        private const int TooltipCanvasSortingOrder = 6000;
        private const float CanvasReferenceWidth = 1920.0f;
        private const float CanvasReferenceHeight = 1080.0f;

        private Canvas tooltipCanvas;
        private CanvasScaler tooltipCanvasScaler;
        private GraphicRaycaster tooltipGraphicRaycaster;
        private GameObject itemTooltipPrefabObject;
        private GameObject skillTooltipPrefabObject;
        private CItemTooltipUI runtimeItemTooltipUi;
        private CSkillTooltipUI runtimeSkillTooltipUi;
        private CanvasGroup runtimeItemTooltipCanvasGroup;
        private CanvasGroup runtimeSkillTooltipCanvasGroup;

        ///<summary>
        /// 툴팁 매니저 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            EnsureTooltipCanvas();
        }

        ///<summary>
        /// 표시 중인 툴팁 갱신
        ///</summary>
        private void Update()
        {
            HideTooltipByMouseDownInternal();
            UpdateTooltipPositionInternal();
        }

        ///<summary>
        /// 아이템 툴팁 표시 요청
        ///</summary>
        public static void ShowItemTooltip( CItemDefinition _itemDefinition )
        {
            ShowItemTooltip( _itemDefinition, null, string.Empty );
        }

        ///<summary>
        /// 아이템과 잠재 툴팁 표시 요청
        ///</summary>
        public static void ShowItemTooltip( CItemDefinition _itemDefinition, CEquipmentPotentialData _equipmentPotentialData )
        {
            ShowItemTooltip( _itemDefinition, _equipmentPotentialData, string.Empty );
        }

        ///<summary>
        /// 아이템과 추가 정보 툴팁 표시 요청
        ///</summary>
        public static void ShowItemTooltip( CItemDefinition _itemDefinition, CEquipmentPotentialData _equipmentPotentialData, string _additionalInfoText )
        {
            CUITooltipManager manager = Instance;

            if ( manager == null )
            {
                return;
            }

            manager.ShowItemTooltipInternal( _itemDefinition, _equipmentPotentialData, _additionalInfoText );
        }

        ///<summary>
        /// 문자열 툴팁 표시 요청
        ///</summary>
        public static void ShowTextTooltip( string _titleText, string _descriptionText )
        {
            CUITooltipManager manager = Instance;

            if ( manager == null )
            {
                return;
            }

            manager.ShowTextTooltipInternal( _titleText, _descriptionText );
        }

        ///<summary>
        /// 스킬 툴팁 표시 요청
        ///</summary>
        public static void ShowSkillTooltip( string _titleText, string _infoText, string _currentLevelTitle, string _currentLevelDescription, string _nextLevelTitle, string _nextLevelDescription, bool _hasNextLevel )
        {
            CUITooltipManager manager = Instance;

            if ( manager == null )
            {
                return;
            }

            manager.ShowSkillTooltipInternal( _titleText, _infoText, _currentLevelTitle, _currentLevelDescription, _nextLevelTitle, _nextLevelDescription, _hasNextLevel );
        }

        ///<summary>
        /// 아이템 툴팁 숨김 요청
        ///</summary>
        public static void HideItemTooltip()
        {
            if ( TryGetExistingInstance( out CUITooltipManager manager ) == false || manager == null )
            {
                return;
            }

            manager.HideItemTooltipInternal();
        }

        ///<summary>
        /// 스킬 툴팁 숨김 요청
        ///</summary>
        public static void HideSkillTooltip()
        {
            if ( TryGetExistingInstance( out CUITooltipManager manager ) == false || manager == null )
            {
                return;
            }

            manager.HideSkillTooltipInternal();
        }

        ///<summary>
        /// 모든 툴팁 숨김 요청
        ///</summary>
        public static void HideAllTooltips()
        {
            if ( TryGetExistingInstance( out CUITooltipManager manager ) == false || manager == null )
            {
                return;
            }

            manager.HideAllTooltipsInternal();
        }

        ///<summary>
        /// 아이템 툴팁 표시 처리
        ///</summary>
        private void ShowItemTooltipInternal( CItemDefinition _itemDefinition, CEquipmentPotentialData _equipmentPotentialData, string _additionalInfoText )
        {
            if ( _itemDefinition == null )
            {
                HideItemTooltipInternal();
                return;
            }

            if ( EnsureItemTooltipReady() == false )
            {
                return;
            }

            HideSkillTooltipInternal();
            runtimeItemTooltipUi.SetTooltipContent( _itemDefinition, _equipmentPotentialData, _additionalInfoText );
            runtimeItemTooltipUi.transform.SetAsLastSibling();
            ShowItemTooltipAtMousePosition();
        }

        ///<summary>
        /// 문자열 툴팁 표시 처리
        ///</summary>
        private void ShowTextTooltipInternal( string _titleText, string _descriptionText )
        {
            if ( EnsureItemTooltipReady() == false )
            {
                return;
            }

            HideSkillTooltipInternal();
            runtimeItemTooltipUi.SetTooltipContent( _titleText, _descriptionText );
            runtimeItemTooltipUi.transform.SetAsLastSibling();
            ShowItemTooltipAtMousePosition();
        }

        ///<summary>
        /// 스킬 툴팁 표시 처리
        ///</summary>
        private void ShowSkillTooltipInternal( string _titleText, string _infoText, string _currentLevelTitle, string _currentLevelDescription, string _nextLevelTitle, string _nextLevelDescription, bool _hasNextLevel )
        {
            if ( EnsureSkillTooltipReady() == false )
            {
                return;
            }

            HideItemTooltipInternal();
            runtimeSkillTooltipUi.SetTooltipContent( _titleText, _infoText, _currentLevelTitle, _currentLevelDescription, _nextLevelTitle, _nextLevelDescription, _hasNextLevel );
            runtimeSkillTooltipUi.transform.SetAsLastSibling();
            ShowSkillTooltipAtMousePosition();
        }

        ///<summary>
        /// 아이템 툴팁 숨김 처리
        ///</summary>
        private void HideItemTooltipInternal()
        {
            if ( runtimeItemTooltipUi == null )
            {
                return;
            }

            SetCanvasGroupAlpha( runtimeItemTooltipCanvasGroup, 0.0f );
            runtimeItemTooltipUi.SetVisible( false );
        }

        ///<summary>
        /// 스킬 툴팁 숨김 처리
        ///</summary>
        private void HideSkillTooltipInternal()
        {
            if ( runtimeSkillTooltipUi == null )
            {
                return;
            }

            SetCanvasGroupAlpha( runtimeSkillTooltipCanvasGroup, 0.0f );
            runtimeSkillTooltipUi.SetVisible( false );
        }

        ///<summary>
        /// 아이템 툴팁 첫 표시 위치 보정
        ///</summary>
        private void ShowItemTooltipAtMousePosition()
        {
            if ( runtimeItemTooltipUi == null || tooltipCanvas == null )
            {
                return;
            }

            runtimeItemTooltipCanvasGroup = EnsureCanvasGroup( runtimeItemTooltipUi.gameObject, runtimeItemTooltipCanvasGroup );
            SetCanvasGroupAlpha( runtimeItemTooltipCanvasGroup, 0.0f );
            runtimeItemTooltipUi.SetVisible( true );
            Canvas.ForceUpdateCanvases();
            Vector2 mousePosition = Input.mousePosition;
            runtimeItemTooltipUi.SetScreenPosition( mousePosition, tooltipCanvas );
            Canvas.ForceUpdateCanvases();
            SetCanvasGroupAlpha( runtimeItemTooltipCanvasGroup, 1.0f );
        }

        ///<summary>
        /// 스킬 툴팁 첫 표시 위치 보정
        ///</summary>
        private void ShowSkillTooltipAtMousePosition()
        {
            if ( runtimeSkillTooltipUi == null || tooltipCanvas == null )
            {
                return;
            }

            runtimeSkillTooltipCanvasGroup = EnsureCanvasGroup( runtimeSkillTooltipUi.gameObject, runtimeSkillTooltipCanvasGroup );
            SetCanvasGroupAlpha( runtimeSkillTooltipCanvasGroup, 0.0f );
            runtimeSkillTooltipUi.SetVisible( true );
            Canvas.ForceUpdateCanvases();
            Vector2 mousePosition = Input.mousePosition;
            runtimeSkillTooltipUi.SetScreenPosition( mousePosition, tooltipCanvas );
            Canvas.ForceUpdateCanvases();
            SetCanvasGroupAlpha( runtimeSkillTooltipCanvasGroup, 1.0f );
        }

        ///<summary>
        /// 전체 툴팁 숨김 처리
        ///</summary>
        private void HideAllTooltipsInternal()
        {
            HideItemTooltipInternal();
            HideSkillTooltipInternal();
        }

        ///<summary>
        /// 표시 중인 툴팁 위치 갱신 처리
        ///</summary>
        private void UpdateTooltipPositionInternal()
        {
            Vector2 mousePosition = Input.mousePosition;

            if ( runtimeItemTooltipUi != null && runtimeItemTooltipUi.gameObject.activeSelf && tooltipCanvas != null )
            {
                runtimeItemTooltipUi.SetScreenPosition( mousePosition, tooltipCanvas );
            }

            if ( runtimeSkillTooltipUi != null && runtimeSkillTooltipUi.gameObject.activeSelf && tooltipCanvas != null )
            {
                runtimeSkillTooltipUi.SetScreenPosition( mousePosition, tooltipCanvas );
            }
        }

        ///<summary>
        /// 마우스 입력 기반 툴팁 숨김 처리
        ///</summary>
        private void HideTooltipByMouseDownInternal()
        {
            bool isLeftMouseDown = Input.GetMouseButtonDown( 0 );
            bool isRightMouseDown = Input.GetMouseButtonDown( 1 );
            bool isMiddleMouseDown = Input.GetMouseButtonDown( 2 );

            if ( isLeftMouseDown == false && isRightMouseDown == false && isMiddleMouseDown == false )
            {
                return;
            }

            HideAllTooltipsInternal();
        }

        ///<summary>
        /// 아이템 툴팁 준비 여부 반환
        ///</summary>
        private bool EnsureItemTooltipReady()
        {
            EnsureTooltipCanvas();
            EnsureItemTooltipPrefab();
            EnsureItemTooltipUi();
            bool result = tooltipCanvas != null && runtimeItemTooltipUi != null;
            return result;
        }

        ///<summary>
        /// 스킬 툴팁 준비 여부 반환
        ///</summary>
        private bool EnsureSkillTooltipReady()
        {
            EnsureTooltipCanvas();
            EnsureSkillTooltipPrefab();
            EnsureSkillTooltipUi();
            bool result = tooltipCanvas != null && runtimeSkillTooltipUi != null;
            return result;
        }

        ///<summary>
        /// 툴팁 전용 캔버스 보장
        ///</summary>
        private void EnsureTooltipCanvas()
        {
            if ( tooltipCanvas != null )
            {
                return;
            }

            GameObject canvasObject = new GameObject( TooltipCanvasObjectName, typeof( RectTransform ), typeof( Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
            DontDestroyOnLoad( canvasObject );
            tooltipCanvas = canvasObject.GetComponent<Canvas>();
            tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tooltipCanvas.sortingOrder = TooltipCanvasSortingOrder;
            tooltipCanvasScaler = canvasObject.GetComponent<CanvasScaler>();
            tooltipCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            tooltipCanvasScaler.referenceResolution = new Vector2( CanvasReferenceWidth, CanvasReferenceHeight );
            tooltipCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            tooltipCanvasScaler.matchWidthOrHeight = 0.5f;
            tooltipGraphicRaycaster = canvasObject.GetComponent<GraphicRaycaster>();
            tooltipGraphicRaycaster.enabled = false;
        }

        ///<summary>
        /// 아이템 툴팁 프리팹 보장
        ///</summary>
        private void EnsureItemTooltipPrefab()
        {
            if ( itemTooltipPrefabObject != null )
            {
                return;
            }

            CResourceManager resourceManager = CResourceManager.Instance;
            itemTooltipPrefabObject = resourceManager != null ? resourceManager.GetItemTooltipPrefab() : null;
        }

        ///<summary>
        /// 스킬 툴팁 프리팹 보장
        ///</summary>
        private void EnsureSkillTooltipPrefab()
        {
            if ( skillTooltipPrefabObject != null )
            {
                return;
            }

            CResourceManager resourceManager = CResourceManager.Instance;
            skillTooltipPrefabObject = resourceManager != null ? resourceManager.GetSkillTooltipPrefab() : null;
        }

        ///<summary>
        /// 아이템 툴팁 인스턴스 보장
        ///</summary>
        private void EnsureItemTooltipUi()
        {
            if ( runtimeItemTooltipUi != null || itemTooltipPrefabObject == null || tooltipCanvas == null )
            {
                return;
            }

            GameObject createdTooltipObject = Instantiate( itemTooltipPrefabObject, tooltipCanvas.transform );
            createdTooltipObject.name = itemTooltipPrefabObject.name;
            runtimeItemTooltipUi = createdTooltipObject.GetComponent<CItemTooltipUI>();

            if ( runtimeItemTooltipUi == null )
            {
                runtimeItemTooltipUi = createdTooltipObject.AddComponent<CItemTooltipUI>();
            }

            runtimeItemTooltipCanvasGroup = EnsureCanvasGroup( createdTooltipObject, runtimeItemTooltipCanvasGroup );
            SetCanvasGroupAlpha( runtimeItemTooltipCanvasGroup, 0.0f );
            runtimeItemTooltipUi.SetVisible( false );
        }

        ///<summary>
        /// 스킬 툴팁 인스턴스 보장
        ///</summary>
        private void EnsureSkillTooltipUi()
        {
            if ( runtimeSkillTooltipUi != null || skillTooltipPrefabObject == null || tooltipCanvas == null )
            {
                return;
            }

            GameObject createdTooltipObject = Instantiate( skillTooltipPrefabObject, tooltipCanvas.transform );
            createdTooltipObject.name = skillTooltipPrefabObject.name;
            runtimeSkillTooltipUi = createdTooltipObject.GetComponent<CSkillTooltipUI>();

            if ( runtimeSkillTooltipUi == null )
            {
                runtimeSkillTooltipUi = createdTooltipObject.AddComponent<CSkillTooltipUI>();
            }

            runtimeSkillTooltipCanvasGroup = EnsureCanvasGroup( createdTooltipObject, runtimeSkillTooltipCanvasGroup );
            SetCanvasGroupAlpha( runtimeSkillTooltipCanvasGroup, 0.0f );
            runtimeSkillTooltipUi.SetVisible( false );
        }

        ///<summary>
        /// 툴팁 CanvasGroup 보장
        ///</summary>
        private CanvasGroup EnsureCanvasGroup( GameObject _targetObject, CanvasGroup _cachedCanvasGroup )
        {
            if ( _cachedCanvasGroup != null )
            {
                return _cachedCanvasGroup;
            }

            if ( _targetObject == null )
            {
                return null;
            }

            CanvasGroup canvasGroup = _targetObject.GetComponent<CanvasGroup>();

            if ( canvasGroup == null )
            {
                canvasGroup = _targetObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            return canvasGroup;
        }

        ///<summary>
        /// 툴팁 CanvasGroup 투명도 반영
        ///</summary>
        private void SetCanvasGroupAlpha( CanvasGroup _canvasGroup, float _alpha )
        {
            if ( _canvasGroup == null )
            {
                return;
            }

            _canvasGroup.alpha = _alpha;
        }
    }
}
