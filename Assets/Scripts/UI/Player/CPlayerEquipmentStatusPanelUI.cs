using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using LayerLab.ArtMakerUnity;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 장비 슬롯, 스탯, 캐릭터 외형 프리뷰장비 상태 패널 컴포넌트
    ///</summary>
    public sealed class CPlayerEquipmentStatusPanelUI : MonoBehaviour
    {
        private const string PlayerVisualPrefabResourcePath = "Prefabs/Character/Player/Player";
        private const float PreviewTextureWidth = 300.0f;
        private const float PreviewTextureHeight = 250.0f;
        private static readonly Vector3 PreviewWorldPosition = new Vector3( 10000.0f, 10000.0f, 0.0f );
        private static readonly PartsType[] PreviewManagedPartsTypeArray =
        {
            PartsType.Sword,
            PartsType.Axe,
            PartsType.Bow,
            PartsType.Wand,
            PartsType.Staff,
            PartsType.Spear,
            PartsType.Blunt,
            PartsType.Crossbow,
            PartsType.Shield,
            PartsType.SubItem,
            PartsType.Helmet,
            PartsType.Chest
        };

        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private RectTransform previewFrameRectTransform;
        [SerializeField] private RawImage previewRawImage;
        [SerializeField] private RectTransform statSectionRectTransform;
        [SerializeField] private CPlayerStatView playerStatView;
        [SerializeField] private RectTransform slotRowRectTransform;
        [SerializeField] private CPlayerEquipmentSlotView helmetSlotView;
        [SerializeField] private CPlayerEquipmentSlotView armorSlotView;
        [SerializeField] private CPlayerEquipmentSlotView weaponSlotView;
        [SerializeField] private CPlayerEquipmentSlotView shieldSlotView;
        [SerializeField] private PopupItemInventory targetInventoryUiController;
        [SerializeField] private Canvas targetCanvas;

        [SerializeField] private CPlayerInventoryManager targetInventoryManager;
        [SerializeField] private CPlayerEquipmentManager targetEquipmentManager;
        [SerializeField] private CPlayerStatManager targetStatManager;
        [SerializeField] private PlayerController targetPlayerController;

        private Camera previewCamera;
        private RenderTexture previewRenderTexture;
        private GameObject previewRootObject;
        private GameObject previewInstanceObject;
        private PresetData.PresetItem previewDefaultPresetItem;
        private readonly Dictionary<PartsType, int> previewDefaultPartsIndexDictionary = new Dictionary<PartsType, int>();
        private readonly Dictionary<PartsType, bool> previewDefaultPartsVisibilityDictionary = new Dictionary<PartsType, bool>();
        private bool isShowingPanelOwnedTooltip;

        ///<summary>
        /// 패널 내부 UI 참조와 초기 상호작용 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            ConfigureSlotViews();
            ConfigureStatTooltipTriggers();
        }

        ///<summary>
        /// 패널 표시 시 대상 연결, 이벤트 구독, 프리뷰 갱신 처리
        ///</summary>
        private void OnEnable()
        {
            ResolveReferences();
            ResolveTargets();
            ConfigureSlotViews();
            ConfigureStatTooltipTriggers();
            SubscribeEvents();
            EnsurePreviewObjects();
            RefreshView();
            SetPreviewActive( true );
        }

        ///<summary>
        /// 장비 및 스탯 툴팁의 닫힘과 호버 상태를 처리합니다.
        ///</summary>
        private void Update()
        {
            HandleTooltipCloseInput();
            HandleTooltipHoverState();
        }

        ///<summary>
        /// 이벤트 구독을 해제하고 툴팁과 프리뷰를 비활성화합니다.
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEvents();
            HideTooltip();
            SetPreviewActive( false );
        }

        ///<summary>
        /// 런타임에 만든 프리뷰 카메라와 렌더 텍스처를 해제합니다.
        ///</summary>
        private void OnDestroy()
        {
            ReleasePreviewObjects();
        }

        ///<summary>
        /// 플레이어 인벤토리, 장비, 스탯, 컨트롤러 참조를 주입합니다.
        ///</summary>
        public void Bind( CPlayerInventoryManager _targetInventoryManager, CPlayerEquipmentManager _targetEquipmentManager, CPlayerStatManager _targetStatManager, PlayerController _targetPlayerController )
        {
            bool isSameBinding = targetInventoryManager == _targetInventoryManager
                && targetEquipmentManager == _targetEquipmentManager
                && targetStatManager == _targetStatManager
                && targetPlayerController == _targetPlayerController;

            if ( isSameBinding )
            {
                SubscribeEvents();
                RefreshView();
                return;
            }

            UnsubscribeEvents();
            targetInventoryManager = _targetInventoryManager;
            targetEquipmentManager = _targetEquipmentManager;
            targetStatManager = _targetStatManager;
            targetPlayerController = _targetPlayerController;
            SubscribeEvents();
            RefreshView();
        }

        ///<summary>
        /// 장착 아이템과 잠재능력 정보를 인벤토리 툴팁으로 표시합니다.
        ///</summary>
        public void ShowEquipmentTooltip( CItemDefinition _itemDefinition )
        {
            ShowEquipmentTooltip( eEquipmentType.NONE, _itemDefinition );
        }

        ///<summary>
        /// 장착 아이템과 잠재능력 정보를 인벤토리 툴팁으로 표시합니다.
        ///</summary>
        public void ShowEquipmentTooltip( eEquipmentType _equipmentType, CItemDefinition _itemDefinition )
        {
            ResolveReferences();

            if ( targetInventoryUiController == null || _itemDefinition == null )
            {
                return;
            }

            isShowingPanelOwnedTooltip = true;
            CEquipmentPotentialData equipmentPotentialData = targetEquipmentManager != null ? targetEquipmentManager.GetEquippedPotentialData( _equipmentType ) : null;
            targetInventoryUiController.ShowItemDefinitionTooltip( _itemDefinition, equipmentPotentialData );
        }

        ///<summary>
        /// 스탯 설명을 공용 툴팁으로 표시합니다.
        ///</summary>
        public void ShowStatTooltip( string _titleText, string _descriptionText )
        {
            ResolveReferences();

            if ( targetInventoryUiController == null )
            {
                return;
            }

            isShowingPanelOwnedTooltip = true;
            targetInventoryUiController.ShowTextTooltip( _titleText, _descriptionText );
        }

        ///<summary>
        /// 패널에서 표시한 공용 툴팁 닫기
        ///</summary>
        public void HideTooltip()
        {
            ResolveReferences();

            if ( targetInventoryUiController == null )
            {
                return;
            }

            isShowingPanelOwnedTooltip = false;
            targetInventoryUiController.HideSharedTooltip();
        }

        ///<summary>
        /// 지정 장비를 인벤토리로 해제하고 표시를 갱신합니다.
        ///</summary>
        public void TryUnequip( eEquipmentType _equipmentType )
        {
            if ( targetEquipmentManager == null || targetInventoryManager == null )
            {
                return;
            }

            bool didUnequip = targetEquipmentManager.TryUnequipToInventory( targetInventoryManager, _equipmentType );

            if ( didUnequip == false )
            {
                return;
            }

            RefreshEquipmentSlots();
            HideTooltip();
        }

        ///<summary>
        /// 스탯, 장비 슬롯, 캐릭터 프리뷰를 전체 갱신합니다.
        ///</summary>
        public void RefreshView()
        {
            ResolveTargets();
            ResolveReferences();

            if ( playerStatView != null )
            {
                playerStatView.Bind( targetStatManager );
            }

            RefreshEquipmentSlots();
            RefreshPreviewCharacter();
        }

        ///<summary>
        /// 직렬화된 패널 내부 UI 참조를 보정합니다.
        ///</summary>
        private void ResolveReferences()
        {
            if ( rootRectTransform == null )
            {
                rootRectTransform = transform as RectTransform;
            }

            if ( previewFrameRectTransform == null )
            {
                Transform previewFrameTransform = transform.Find( "PreviewFrame" );
                previewFrameRectTransform = previewFrameTransform as RectTransform;
            }

            if ( previewRawImage == null )
            {
                Transform previewImageTransform = transform.Find( "PreviewFrame/PreviewImage" );
                previewRawImage = previewImageTransform != null ? previewImageTransform.GetComponent<RawImage>() : null;
            }

            if ( statSectionRectTransform == null )
            {
                Transform statSectionTransform = transform.Find( "StatSection" );
                statSectionRectTransform = statSectionTransform as RectTransform;
            }

            if ( playerStatView == null && statSectionRectTransform != null )
            {
                playerStatView = statSectionRectTransform.GetComponent<CPlayerStatView>();
            }

            if ( slotRowRectTransform == null )
            {
                Transform slotRowTransform = transform.Find( "SlotRow" );
                slotRowRectTransform = slotRowTransform as RectTransform;
            }

            helmetSlotView = ResolveSlotViewReference( helmetSlotView, "HelmetSlot" );
            armorSlotView = ResolveSlotViewReference( armorSlotView, "ArmorSlot" );
            weaponSlotView = ResolveSlotViewReference( weaponSlotView, "WeaponSlot" );
            shieldSlotView = ResolveSlotViewReference( shieldSlotView, "ShieldSlot" );

            if ( targetCanvas == null )
            {
                targetCanvas = GetComponentInParent<Canvas>();
            }

            if ( targetInventoryUiController == null )
            {
                targetInventoryUiController = GetComponentInParent<PopupItemInventory>();
            }

            if ( targetInventoryUiController == null )
            {
                targetInventoryUiController = FindFirstObjectByType<PopupItemInventory>();
            }
        }

        ///<summary>
        /// 이름을 기준으로 장비 슬롯 뷰 참조를 보정합니다.
        ///</summary>
        private CPlayerEquipmentSlotView ResolveSlotViewReference( CPlayerEquipmentSlotView _currentReference, string _targetName )
        {
            if ( _currentReference != null )
            {
                return _currentReference;
            }

            Transform slotTransform = transform.Find( $"SlotRow/{_targetName}" );
            CPlayerEquipmentSlotView result = slotTransform != null ? slotTransform.GetComponent<CPlayerEquipmentSlotView>() : null;
            return result;
        }

        ///<summary>
        /// 런타임 플레이어 매니저와 컨트롤러 참조를 찾습니다.
        ///</summary>
        private void ResolveTargets()
        {
            bool hasGameManager = CGameManager.TryGetExistingInstance( out CGameManager gameManager );

            if ( hasGameManager == false )
            {
                return;
            }

            gameManager.TryGetActivePlayerController( out targetPlayerController );
            bool hasRuntimeContext = gameManager.TryGetPlayerRuntimeContext( out CPlayerRuntimeContext playerRuntimeContext );

            if ( hasRuntimeContext == false )
            {
                return;
            }

            targetEquipmentManager = playerRuntimeContext.GetEquipmentManager();
            targetInventoryManager = playerRuntimeContext.GetInventoryManager();
            targetStatManager = playerRuntimeContext.GetStatManager();
        }

        ///<summary>
        /// 장비 변경 이벤트를 구독합니다.
        ///</summary>
        private void SubscribeEvents()
        {
            if ( targetEquipmentManager == null )
            {
                return;
            }

            targetEquipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
            targetEquipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        }

        ///<summary>
        /// 장비 변경 이벤트 구독 해제
        ///</summary>
        private void UnsubscribeEvents()
        {
            if ( targetEquipmentManager == null )
            {
                return;
            }

            targetEquipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        ///<summary>
        /// 장비 변경 시 툴팁, 슬롯, 프리뷰를 갱신합니다.
        ///</summary>
        private void HandleEquipmentChanged( CPlayerEquipmentManager _equipmentManager )
        {
            HideTooltip();
            RefreshEquipmentSlots();
            RefreshPreviewCharacter();
        }

        ///<summary>
        /// 마우스 클릭으로 패널 소유 툴팁 닫기
        ///</summary>
        private void HandleTooltipCloseInput()
        {
            if ( isShowingPanelOwnedTooltip == false )
            {
                return;
            }

            bool isLeftMouseDown = Input.GetMouseButtonDown( 0 );
            bool isRightMouseDown = Input.GetMouseButtonDown( 1 );

            if ( isLeftMouseDown == false && isRightMouseDown == false )
            {
                return;
            }

            HideTooltip();
        }

        ///<summary>
        /// 툴팁 원본 UI에서 벗어나면 툴팁 닫기
        ///</summary>
        private void HandleTooltipHoverState()
        {
            if ( isShowingPanelOwnedTooltip == false )
            {
                return;
            }

            bool isHoveringTooltipSource = IsPointerHoveringTooltipSource();

            if ( isHoveringTooltipSource )
            {
                return;
            }

            HideTooltip();
        }

        ///<summary>
        /// 포인터가 이 패널의 장비 또는 스탯 UI 위에 있는지 확인합니다.
        ///</summary>
        private bool IsPointerHoveringTooltipSource()
        {
            EventSystem currentEventSystem = EventSystem.current;

            if ( currentEventSystem == null )
            {
                return false;
            }

            PointerEventData pointerEventData = new PointerEventData( currentEventSystem );
            pointerEventData.position = Input.mousePosition;
            System.Collections.Generic.List<RaycastResult> raycastResultList = new System.Collections.Generic.List<RaycastResult>();
            currentEventSystem.RaycastAll( pointerEventData, raycastResultList );

            for ( int index = 0; index < raycastResultList.Count; index++ )
            {
                GameObject hoveredObject = raycastResultList[ index ].gameObject;

                if ( hoveredObject == null )
                {
                    continue;
                }

                CPlayerStatTooltipTrigger statTooltipTrigger = hoveredObject.GetComponentInParent<CPlayerStatTooltipTrigger>();

                if ( statTooltipTrigger != null && statTooltipTrigger.transform.IsChildOf( transform ) )
                {
                    return true;
                }

                CPlayerEquipmentSlotView equipmentSlotView = hoveredObject.GetComponentInParent<CPlayerEquipmentSlotView>();

                if ( equipmentSlotView != null && equipmentSlotView.transform.IsChildOf( transform ) )
                {
                    return true;
                }
            }

            return false;
        }

        ///<summary>
        /// 모든 장비 슬롯 표시를 현재 장착 상태로 갱신합니다.
        ///</summary>
        private void RefreshEquipmentSlots()
        {
            RefreshSlotView( helmetSlotView, eEquipmentType.HELMET );
            RefreshSlotView( armorSlotView, eEquipmentType.ARMOR );
            RefreshSlotView( weaponSlotView, eEquipmentType.WEAPON );
            RefreshSlotView( shieldSlotView, eEquipmentType.SHIELD );
        }

        ///<summary>
        /// 한 장비 슬롯에 해당하는 장착 아이템 표시
        ///</summary>
        private void RefreshSlotView( CPlayerEquipmentSlotView _slotView, eEquipmentType _equipmentType )
        {
            if ( _slotView == null )
            {
                return;
            }

            CItemDefinition equippedItemDefinition = targetEquipmentManager != null ? targetEquipmentManager.GetEquippedItemDefinition( _equipmentType ) : null;
            _slotView.RefreshSlot( equippedItemDefinition );
        }

        ///<summary>
        /// 각 장비 슬롯의 타입과 기본 라벨 초기화
        ///</summary>
        private void ConfigureSlotViews()
        {
            ConfigureSlotView( helmetSlotView, eEquipmentType.HELMET, "HELMET", "H" );
            ConfigureSlotView( armorSlotView, eEquipmentType.ARMOR, "ARMOR", "A" );
            ConfigureSlotView( weaponSlotView, eEquipmentType.WEAPON, "WEAPON", "W" );
            ConfigureSlotView( shieldSlotView, eEquipmentType.SHIELD, "SHIELD", "S" );
        }

        ///<summary>
        /// 장비 슬롯 뷰와 드롭 대상 구성
        ///</summary>
        private void ConfigureSlotView( CPlayerEquipmentSlotView _slotView, eEquipmentType _equipmentType, string _labelText, string _placeholderText )
        {
            if ( _slotView == null )
            {
                return;
            }

            _slotView.Initialize( this, _equipmentType, _labelText, _placeholderText );
            CPlayerEquipmentSlotDropTarget dropTarget = _slotView.GetComponent<CPlayerEquipmentSlotDropTarget>();

            if ( dropTarget != null )
            {
                dropTarget.Configure( _equipmentType, null, targetEquipmentManager );
            }
        }

        ///<summary>
        /// 스탯 행별 툴팁 제목과 설명 설정
        ///</summary>
        private void ConfigureStatTooltipTriggers()
        {
            ConfigureStatTooltipTrigger( "StatSection/HpRow", "체력", "현재 체력과 최대 체력 수치." );
            ConfigureStatTooltipTrigger( "StatSection/MpRow", "마나", "현재 마나와 최대 마나 수치." );
            ConfigureStatTooltipTrigger( "StatSection/AtkRow", "공격력", "기본 공격과 스킬 계산에 사용되는 공격력 수치." );
            ConfigureStatTooltipTrigger( "StatSection/DefRow", "방어력", "피격 피해를 완화하는 데 사용되는 방어력 수치." );
            ConfigureStatTooltipTrigger( "StatSection/CrtRow", "치명타 확률", "CRT 1당 치명타 확률 1% 증가." );
            ConfigureStatTooltipTrigger( "StatSection/CrdRow", "치명타 피해", "CRD 1당 치명타 추가 피해 1% 증가." );
            ConfigureStatTooltipTrigger( "StatSection/AccRow", "정확도", "최종 피해의 최소값과 최대값 편차를 보정하는 정확도 수치." );
            ConfigureStatTooltipTrigger( "StatSection/CdrRow", "쿨타임 감소", "모든 스킬의 쿨타임을 CDR 수치만큼 감소시킵니다." );
            ConfigureStatTooltipTrigger( "StatSection/MoveRow", "이동 속도", "플레이어 이동 속도 증가 비율." );
            ConfigureStatTooltipTrigger( "StatSection/HrRow", "체력 재생", "시간 경과에 따라 회복되는 체력 수치." );
            ConfigureStatTooltipTrigger( "StatSection/MrRow", "마나 재생", "시간 경과에 따라 회복되는 마나 수치." );
            ConfigureStatTooltipTrigger( "StatSection/PointRow", "RNG", "기본공격과 스킬의 공격 범위 증가 비율." );
        }

        ///<summary>
        /// 지정 경로의 스탯 행에 툴팁 트리거를 연결합니다.
        ///</summary>
        private void ConfigureStatTooltipTrigger( string _targetPath, string _titleText, string _descriptionText )
        {
            Transform targetTransform = transform.Find( _targetPath );

            if ( targetTransform == null )
            {
                return;
            }

            CPlayerStatTooltipTrigger tooltipTrigger = targetTransform.GetComponent<CPlayerStatTooltipTrigger>();

            if ( tooltipTrigger == null )
            {
                return;
            }

            tooltipTrigger.Configure( this, _titleText, _descriptionText );
        }

        ///<summary>
        /// 캐릭터 프리뷰용 루트와 카메라를 생성합니다.
        ///</summary>
        private void EnsurePreviewObjects()
        {
            if ( previewRawImage == null )
            {
                return;
            }

            if ( previewRootObject == null )
            {
                previewRootObject = new GameObject( "CharacterPreviewRoot" );
                previewRootObject.transform.position = PreviewWorldPosition;
            }

            if ( previewCamera == null )
            {
                GameObject previewCameraObject = new GameObject( "CharacterPreviewCamera" );
                previewCameraObject.transform.position = PreviewWorldPosition + new Vector3( 0.0f, 0.0f, -10.0f );
                Camera createdCamera = previewCameraObject.AddComponent<Camera>();
                createdCamera.clearFlags = CameraClearFlags.SolidColor;
                createdCamera.backgroundColor = new Color( 0.0f, 0.0f, 0.0f, 0.0f );
                createdCamera.orthographic = true;
                createdCamera.nearClipPlane = 0.1f;
                createdCamera.farClipPlane = 50.0f;
                createdCamera.enabled = false;
                previewCamera = createdCamera;
            }

            RefreshPreviewRenderTexture();
        }

        ///<summary>
        /// 프리뷰 이미지 크기에 맞는 렌더 텍스처를 준비합니다.
        ///</summary>
        private void RefreshPreviewRenderTexture()
        {
            if ( previewRawImage == null || previewCamera == null )
            {
                return;
            }

            int textureWidth = Mathf.RoundToInt( Mathf.Max( PreviewTextureWidth, previewRawImage.rectTransform.rect.width ) );
            int textureHeight = Mathf.RoundToInt( Mathf.Max( PreviewTextureHeight, previewRawImage.rectTransform.rect.height ) );
            bool isTextureValid = previewRenderTexture != null && previewRenderTexture.width == textureWidth && previewRenderTexture.height == textureHeight;

            if ( isTextureValid == false )
            {
                if ( previewRenderTexture != null )
                {
                    previewRenderTexture.Release();
                    Destroy( previewRenderTexture );
                }

                previewRenderTexture = new RenderTexture( textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32 );
                previewRenderTexture.Create();
            }

            previewCamera.targetTexture = previewRenderTexture;
            previewRawImage.texture = previewRenderTexture;
        }

        ///<summary>
        /// 현재 플레이어 외형과 장비 상태를 프리뷰 캐릭터에 반영합니다.
        ///</summary>
        private void RefreshPreviewCharacter()
        {
            if ( previewRootObject == null || previewCamera == null )
            {
                return;
            }

            GameObject sourceVisualObject = ResolvePreviewSourceObject();

            if ( sourceVisualObject == null )
            {
                return;
            }

            if ( previewInstanceObject == null )
            {
                previewInstanceObject = Instantiate( sourceVisualObject, previewRootObject.transform );
                previewInstanceObject.name = "PreviewCharacter";
                previewInstanceObject.transform.localPosition = Vector3.zero;
                previewInstanceObject.transform.localRotation = Quaternion.identity;
                previewInstanceObject.transform.localScale = Vector3.one;
                CachePreviewDefaultPresetItem();
                RemovePreviewPresetComponent();
            }

            ApplyPreviewPartsState();
            ConfigurePreviewCamera();
        }

        ///<summary>
        /// 프리뷰에 사용할 기본 플레이어 외형 프리팹 로드
        ///</summary>
        private GameObject ResolvePreviewSourceObject()
        {
            GameObject loadedPrefabObject = Resources.Load<GameObject>( PlayerVisualPrefabResourcePath );
            return loadedPrefabObject;
        }

        ///<summary>
        /// 기본 외형을 복원한 뒤 현재 장비 파츠를 프리뷰에 적용합니다.
        ///</summary>
        private void ApplyPreviewPartsState()
        {
            PartsManager previewPartsManager = ResolvePreviewPartsManager();

            if ( previewPartsManager == null )
            {
                return;
            }

            previewPartsManager.Init();
            ApplyPreviewDefaultPresetState( previewPartsManager );
            CachePreviewDefaultPartsState( previewPartsManager );
            PartsManager sourcePartsManager = ResolveLivePlayerPartsManager();

            if ( sourcePartsManager != null )
            {
                previewPartsManager.CopyFrom( sourcePartsManager );
            }

            ApplyEquipmentStateToPreviewPartsManager( previewPartsManager );
        }

        ///<summary>
        /// 프리뷰 캐릭터의 기본 프리셋 보관
        ///</summary>
        private void CachePreviewDefaultPresetItem()
        {
            previewDefaultPresetItem = null;

            if ( previewInstanceObject == null )
            {
                return;
            }

            CharacterPrefabData previewCharacterPrefabData = previewInstanceObject.GetComponent<CharacterPrefabData>();

            if ( previewCharacterPrefabData == null )
            {
                return;
            }

            previewDefaultPresetItem = previewCharacterPrefabData.CreatePresetItem();
        }

        ///<summary>
        /// 보관한 기본 프리셋을 프리뷰 파츠 매니저에 적용합니다.
        ///</summary>
        private void ApplyPreviewDefaultPresetState( PartsManager _previewPartsManager )
        {
            if ( _previewPartsManager == null || previewDefaultPresetItem == null || previewDefaultPresetItem.isEmpty )
            {
                return;
            }

            _previewPartsManager.ApplyPresetItem( previewDefaultPresetItem );
        }

        ///<summary>
        /// 장비 적용 전 기본 파츠의 인덱스와 표시 상태를 보관합니다.
        ///</summary>
        private void CachePreviewDefaultPartsState( PartsManager _previewPartsManager )
        {
            if ( _previewPartsManager == null )
            {
                return;
            }

            previewDefaultPartsIndexDictionary.Clear();
            previewDefaultPartsVisibilityDictionary.Clear();

            for ( int index = 0; index < PreviewManagedPartsTypeArray.Length; index++ )
            {
                PartsType managedPartsType = PreviewManagedPartsTypeArray[ index ];
                int defaultPartsIndex = _previewPartsManager.GetActiveIndex( managedPartsType );
                bool defaultPartsVisibility = _previewPartsManager.IsPartsVisible( managedPartsType );
                previewDefaultPartsIndexDictionary[ managedPartsType ] = defaultPartsIndex;
                previewDefaultPartsVisibilityDictionary[ managedPartsType ] = defaultPartsVisibility;
            }
        }

        ///<summary>
        /// 복제된 프리뷰에서 원본 프리셋 컴포넌트를 제거합니다.
        ///</summary>
        private void RemovePreviewPresetComponent()
        {
            if ( previewInstanceObject == null )
            {
                return;
            }

            CharacterPrefabData previewCharacterPrefabData = previewInstanceObject.GetComponent<CharacterPrefabData>();

            if ( previewCharacterPrefabData == null )
            {
                return;
            }

            Destroy( previewCharacterPrefabData );
        }

        ///<summary>
        /// 프리뷰 캐릭터의 파츠 매니저를 반환합니다.
        ///</summary>
        private PartsManager ResolvePreviewPartsManager()
        {
            if ( previewInstanceObject == null )
            {
                return null;
            }

            PartsManager result = previewInstanceObject.GetComponentInChildren<PartsManager>( true );
            return result;
        }

        ///<summary>
        /// 실제 플레이어의 파츠 매니저를 반환합니다.
        ///</summary>
        private PartsManager ResolveLivePlayerPartsManager()
        {
            if ( targetPlayerController == null )
            {
                return null;
            }

            PartsManager result = targetPlayerController.GetComponentInChildren<PartsManager>( true );
            return result;
        }

        ///<summary>
        /// 현재 장비 슬롯 상태를 프리뷰 파츠 매니저에 적용합니다.
        ///</summary>
        private void ApplyEquipmentStateToPreviewPartsManager( PartsManager _previewPartsManager )
        {
            if ( _previewPartsManager == null )
            {
                return;
            }

            ApplyEquipmentVisualToPreviewPartsManager( _previewPartsManager, eEquipmentType.WEAPON );
            ApplyEquipmentVisualToPreviewPartsManager( _previewPartsManager, eEquipmentType.HELMET );
            ApplyEquipmentVisualToPreviewPartsManager( _previewPartsManager, eEquipmentType.ARMOR );
            ApplyEquipmentVisualToPreviewPartsManager( _previewPartsManager, eEquipmentType.SHIELD );
        }

        ///<summary>
        /// 한 장비 슬롯의 외형 파츠를 프리뷰에 적용합니다.
        ///</summary>
        private void ApplyEquipmentVisualToPreviewPartsManager( PartsManager _previewPartsManager, eEquipmentType _equipmentType )
        {
            if ( _previewPartsManager == null || targetEquipmentManager == null )
            {
                return;
            }

            CItemDefinition equippedItemDefinition = targetEquipmentManager.GetEquippedItemDefinition( _equipmentType );
            PartsType[] managedPartsTypeArray = ResolveManagedPartsTypeArray( _equipmentType );

            for ( int index = 0; index < managedPartsTypeArray.Length; index++ )
            {
                PartsType managedPartsType = managedPartsTypeArray[ index ];
                RestorePreviewDefaultPartsState( _previewPartsManager, managedPartsType );
            }

            if ( equippedItemDefinition == null || equippedItemDefinition.HasEquipmentPartsVisual() == false )
            {
                return;
            }

            PartsType equipmentPartsType = equippedItemDefinition.GetEquipmentPartsType();
            int equipmentPartsIndex = equippedItemDefinition.GetEquipmentPartsIndex();

            if ( IsCompatiblePartsType( _equipmentType, equipmentPartsType ) == false || equipmentPartsIndex < 0 )
            {
                return;
            }

            _previewPartsManager.EquipParts( equipmentPartsType, equipmentPartsIndex );
        }

        ///<summary>
        /// 지정 파츠 타입을 장비 적용 전 기본 상태로 복원합니다.
        ///</summary>
        private void RestorePreviewDefaultPartsState( PartsManager _previewPartsManager, PartsType _partsType )
        {
            if ( _previewPartsManager == null )
            {
                return;
            }

            bool hasDefaultIndex = previewDefaultPartsIndexDictionary.TryGetValue( _partsType, out int defaultPartsIndex );
            bool hasDefaultVisibility = previewDefaultPartsVisibilityDictionary.TryGetValue( _partsType, out bool defaultPartsVisibility );

            if ( hasDefaultIndex == false || hasDefaultVisibility == false || defaultPartsVisibility == false || defaultPartsIndex < 0 )
            {
                _previewPartsManager.UnequipParts( _partsType );
                return;
            }

            _previewPartsManager.EquipParts( _partsType, defaultPartsIndex );
        }

        ///<summary>
        /// 장비 타입이 제어하는 캐릭터 파츠 타입 목록 반환
        ///</summary>
        private PartsType[] ResolveManagedPartsTypeArray( eEquipmentType _equipmentType )
        {
            switch ( _equipmentType )
            {
                case eEquipmentType.WEAPON:
                    return new PartsType[]
                    {
                        PartsType.Sword,
                        PartsType.Axe,
                        PartsType.Bow,
                        PartsType.Wand,
                        PartsType.Staff,
                        PartsType.Spear,
                        PartsType.Blunt,
                        PartsType.Crossbow
                    };

                case eEquipmentType.HELMET:
                    return new PartsType[]
                    {
                        PartsType.Helmet
                    };

                case eEquipmentType.ARMOR:
                    return new PartsType[]
                    {
                        PartsType.Chest
                    };

                case eEquipmentType.SHIELD:
                    return new PartsType[]
                    {
                        PartsType.Shield,
                        PartsType.SubItem
                    };
            }

            return new PartsType[ 0 ];
        }

        ///<summary>
        /// 장비 타입과 외형 파츠 타입의 호환 여부를 확인합니다.
        ///</summary>
        private bool IsCompatiblePartsType( eEquipmentType _equipmentType, PartsType _partsType )
        {
            PartsType[] compatiblePartsTypeArray = ResolveManagedPartsTypeArray( _equipmentType );

            for ( int index = 0; index < compatiblePartsTypeArray.Length; index++ )
            {
                PartsType compatiblePartsType = compatiblePartsTypeArray[ index ];

                if ( compatiblePartsType != _partsType )
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        ///<summary>
        /// 프리뷰 캐릭터의 스프라이트 경계에 맞춰 카메라를 배치합니다.
        ///</summary>
        private void ConfigurePreviewCamera()
        {
            if ( previewCamera == null || previewInstanceObject == null || previewRawImage == null )
            {
                return;
            }

            SpriteRenderer[] spriteRenderers = previewInstanceObject.GetComponentsInChildren<SpriteRenderer>( true );

            if ( spriteRenderers == null || spriteRenderers.Length == 0 )
            {
                return;
            }

            Bounds previewBounds = spriteRenderers[ 0 ].bounds;

            for ( int index = 1; index < spriteRenderers.Length; index++ )
            {
                SpriteRenderer spriteRenderer = spriteRenderers[ index ];

                if ( spriteRenderer == null )
                {
                    continue;
                }

                previewBounds.Encapsulate( spriteRenderer.bounds );
            }

            float previewWidth = Mathf.Max( 1.0f, previewRawImage.rectTransform.rect.width );
            float previewHeight = Mathf.Max( 1.0f, previewRawImage.rectTransform.rect.height );
            float previewAspect = previewWidth / previewHeight;
            float horizontalSize = previewBounds.extents.x / Mathf.Max( 0.01f, previewAspect );
            float orthographicSize = Mathf.Max( previewBounds.extents.y, horizontalSize ) + 0.2f;
            Vector3 cameraPosition = previewBounds.center + new Vector3( 0.0f, 0.0f, -10.0f );
            previewCamera.transform.position = cameraPosition;
            previewCamera.orthographicSize = orthographicSize;
        }

        ///<summary>
        /// 프리뷰 카메라와 루트 오브젝트의 활성 상태를 설정합니다.
        ///</summary>
        private void SetPreviewActive( bool _isActive )
        {
            if ( previewCamera != null )
            {
                previewCamera.enabled = _isActive;
            }

            if ( previewRootObject != null )
            {
                previewRootObject.SetActive( _isActive );
            }
        }

        ///<summary>
        /// 생성한 프리뷰 런타임 리소스를 해제합니다.
        ///</summary>
        private void ReleasePreviewObjects()
        {
            if ( previewRenderTexture != null )
            {
                previewRenderTexture.Release();
                Destroy( previewRenderTexture );
                previewRenderTexture = null;
            }

            if ( previewCamera != null )
            {
                Destroy( previewCamera.gameObject );
                previewCamera = null;
            }

            if ( previewRootObject != null )
            {
                Destroy( previewRootObject );
                previewRootObject = null;
            }
        }
    }
}



