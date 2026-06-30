using System.Collections.Generic;
using TMPro;
using TinyHero.Core;
using TinyHero.Maps;
using TinyHero.Player;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 맵 상태와 월드 오브젝트를 미니맵으로 표시하는 UI 컴포넌트
    ///</summary>
    [DisallowMultipleComponent]
    public sealed class CMinimapDisplayUI : MonoBehaviour
    {
        private const string BackgroundObjectName = "BG";
        private const string MapNameTextObjectName = "TextMapName";
        private const string ViewportObjectName = "Viewport";
        private const string BackgroundImageObjectName = "BackgroundImage";
        private const string IconRootObjectName = "IconRoot";
        private const string PlayerIconObjectName = "PlayerIcon";
        private const float MinimumBoundsSize = 8.0f;
        private const float DefaultVisibleIconAlpha = 0.95f;
        private const float DefaultEdgeIconAlpha = 0.55f;
        private const float PlayerPulseSpeed = 3.25f;
        private const float PlayerPulseScaleAmplitude = 0.08f;
        private const float DefaultIconOutlineDistance = 1.2f;
        private const int CircleTextureSize = 64;
        private const int SolidTextureSize = 2;
        private const string MonsterIconPoolKeyPrefix = "UI.MinimapDisplay.MonsterIcon";
        private const string NpcIconPoolKeyPrefix = "UI.MinimapDisplay.NpcIcon";
        private const string PortalIconPoolKeyPrefix = "UI.MinimapDisplay.PortalIcon";
        private static readonly Color DefaultViewportColor = new Color( 0.05f, 0.07f, 0.09f, 0.52f );
        private static readonly Color DefaultBackgroundTintColor = new Color( 1.0f, 1.0f, 1.0f, 0.72f );
        private static readonly Color DefaultPlayerColor = new Color32( 255, 214, 64, 255 );
        private static readonly Color DefaultMonsterColor = new Color32( 255, 82, 82, 255 );
        private static readonly Color DefaultNpcColor = new Color32( 74, 201, 104, 255 );
        private static readonly Color DefaultPortalColor = new Color32( 68, 150, 255, 255 );
        private static readonly Color DefaultIconOutlineColor = new Color32( 18, 24, 30, 255 );
        private static Sprite cachedCircleSprite;
        private static Sprite cachedSolidSprite;

        [Header( "참조" )]
        [SerializeField] private RectTransform backgroundRectTransform;
        [SerializeField] private TMP_Text textMapName;
        [SerializeField] private RectTransform viewportRectTransform;
        [SerializeField] private Image viewportBackgroundImage;
        [SerializeField] private RectTransform iconRootRectTransform;
        [SerializeField] private Image playerIconImage;

        [Header( "설정값" )]
        [SerializeField] private float characterScanInterval = 0.5f;
        [SerializeField] private float boundsRefreshInterval = 1.0f;
        [SerializeField] private Vector2 minimapPadding = new Vector2( 5.0f, 5.0f );
        [SerializeField] private Vector2 playerIconSize = new Vector2( 16.0f, 16.0f );
        [SerializeField] private Vector2 monsterIconSize = new Vector2( 12.0f, 12.0f );
        [SerializeField] private Vector2 npcIconSize = new Vector2( 12.0f, 12.0f );
        [SerializeField] private Vector2 portalIconSize = new Vector2( 12.0f, 12.0f );
        [SerializeField] private Color viewportColor = new Color( 0.05f, 0.07f, 0.09f, 0.52f );
        [SerializeField] private Color backgroundTintColor = new Color( 1.0f, 1.0f, 1.0f, 0.72f );
        [SerializeField] private Color playerIconColor = new Color( 1.0f, 0.8392157f, 0.2509804f, 1.0f );
        [SerializeField] private Color monsterIconColor = new Color( 1.0f, 0.32156864f, 0.32156864f, 1.0f );
        [SerializeField] private Color npcIconColor = new Color( 0.2901961f, 0.7882353f, 0.40784314f, 1.0f );
        [SerializeField] private Color portalIconColor = new Color( 0.26666668f, 0.5882353f, 1.0f, 1.0f );

        private readonly List<MonsterObject> trackedMonsterList = new List<MonsterObject>();
        private readonly List<CNPCObject> trackedNpcList = new List<CNPCObject>();
        private readonly List<PortalObject> trackedPortalList = new List<PortalObject>();
        private readonly Dictionary<MonsterObject, Image> monsterIconByMonster = new Dictionary<MonsterObject, Image>();
        private readonly Dictionary<CNPCObject, Image> npcIconByNpc = new Dictionary<CNPCObject, Image>();
        private readonly Dictionary<PortalObject, Image> portalIconByPortal = new Dictionary<PortalObject, Image>();
        private readonly List<MonsterObject> releaseTargetMonsterList = new List<MonsterObject>();
        private readonly List<CNPCObject> releaseTargetNpcList = new List<CNPCObject>();
        private readonly List<PortalObject> releaseTargetPortalList = new List<PortalObject>();
        private string monsterIconPoolKey = string.Empty;
        private string npcIconPoolKey = string.Empty;
        private string portalIconPoolKey = string.Empty;
        private PlayerController targetPlayerController;
        private string lastMapId = string.Empty;
        private Sprite currentMinimapBackgroundSprite;
        private Bounds cachedMapBounds;
        private float remainingCharacterScanTime;
        private float remainingBoundsRefreshTime;
        private bool hasCachedMapBounds;

        ///<summary>
        /// 미니맵 구성 초기화
        ///</summary>
        private void Awake()
        {
            monsterIconPoolKey = MonsterIconPoolKeyPrefix + "." + GetInstanceID();
            npcIconPoolKey = NpcIconPoolKeyPrefix + "." + GetInstanceID();
            portalIconPoolKey = PortalIconPoolKeyPrefix + "." + GetInstanceID();
            ApplyDefaultConfigurationValues();
            ResolveReferences();
            EnsureRuntimeHierarchy();
            EnsureIconPoolsInitialized();
            RefreshMapNameText();
        }

        ///<summary>
        /// 미니맵 초기 상태 반영
        ///</summary>
        private void Start()
        {
            ForceRefresh();
        }

        ///<summary>
        /// 미니맵 활성화 처리
        ///</summary>
        private void OnEnable()
        {
            remainingCharacterScanTime = 0.0f;
            remainingBoundsRefreshTime = 0.0f;
            ForceRefresh();
        }

        ///<summary>
        /// 미니맵 비활성화 정리
        ///</summary>
        private void OnDisable()
        {
            ReleaseAllMonsterIcons();
            ReleaseAllNpcIcons();
            ReleaseAllPortalIcons();
        }

        ///<summary>
        /// 미니맵 파괴 정리
        ///</summary>
        private void OnDestroy()
        {
            ReleaseAllMonsterIcons();
            ReleaseAllNpcIcons();
            ReleaseAllPortalIcons();
            CObjectPoolManager.TryClearPool( monsterIconPoolKey );
            CObjectPoolManager.TryClearPool( npcIconPoolKey );
            CObjectPoolManager.TryClearPool( portalIconPoolKey );
        }

        ///<summary>
        /// 미니맵 프레임 갱신 처리
        ///</summary>
        private void LateUpdate()
        {
            HandleMapChange();
            ResolvePlayerController();
            RefreshMapNameText();
            RefreshBackgroundImage();
            TickBoundsRefresh();
            TickCharacterRefresh();
            UpdatePlayerIcon();
            UpdateMonsterIcons();
            UpdateNpcIcons();
            UpdatePortalIcons();
        }

        ///<summary>
        /// 즉시 전체 상태 재구성
        ///</summary>
        private void ForceRefresh()
        {
            CacheCurrentMapId();
            ResolvePlayerController();
            RefreshMapNameText();
            RefreshBackgroundImage();
            RefreshMapBounds();
            RefreshTrackedCharacters();
            UpdatePlayerIcon();
            UpdateMonsterIcons();
            UpdateNpcIcons();
            UpdatePortalIcons();
        }

        ///<summary>
        /// 맵 전환 감지와 미니맵 초기화 처리
        ///</summary>
        private void HandleMapChange()
        {
            if ( CMapManager.TryGetInstance( out CMapManager mapManager ) == false || mapManager == null )
            {
                return;
            }

            string currentMapId = mapManager.GetCurrentMapId();

            if ( string.Equals( lastMapId, currentMapId, System.StringComparison.Ordinal ) )
            {
                return;
            }

            ResetMinimapState();
            lastMapId = currentMapId;
            ForceRefresh();
        }

        ///<summary>
        /// 현재 맵 식별자 캐시
        ///</summary>
        private void CacheCurrentMapId()
        {
            if ( CMapManager.TryGetInstance( out CMapManager mapManager ) == false || mapManager == null )
            {
                lastMapId = string.Empty;
                return;
            }

            lastMapId = mapManager.GetCurrentMapId();
        }

        ///<summary>
        /// 미니맵 상태 초기화
        ///</summary>
        private void ResetMinimapState()
        {
            ReleaseAllMonsterIcons();
            ReleaseAllNpcIcons();
            ReleaseAllPortalIcons();
            trackedMonsterList.Clear();
            trackedNpcList.Clear();
            trackedPortalList.Clear();
            hasCachedMapBounds = false;
            currentMinimapBackgroundSprite = null;

            if ( viewportBackgroundImage != null )
            {
                viewportBackgroundImage.sprite = null;
                viewportBackgroundImage.enabled = false;
            }
        }

        ///<summary>
        /// 기본 설정값 보정
        ///</summary>
        private void ApplyDefaultConfigurationValues()
        {
            if ( viewportColor.a <= 0.0f )
            {
                viewportColor = DefaultViewportColor;
            }

            if ( backgroundTintColor.a <= 0.0f )
            {
                backgroundTintColor = DefaultBackgroundTintColor;
            }

            if ( playerIconColor.a <= 0.0f )
            {
                playerIconColor = DefaultPlayerColor;
            }

            if ( monsterIconColor.a <= 0.0f )
            {
                monsterIconColor = DefaultMonsterColor;
            }

            if ( npcIconColor.a <= 0.0f )
            {
                npcIconColor = DefaultNpcColor;
            }

            if ( portalIconColor.a <= 0.0f )
            {
                portalIconColor = DefaultPortalColor;
            }
        }

        ///<summary>
        /// 직렬화 참조 자동 연결
        ///</summary>
        private void ResolveReferences()
        {
            if ( backgroundRectTransform == null )
            {
                Transform backgroundTransform = transform.Find( BackgroundObjectName );
                backgroundRectTransform = backgroundTransform as RectTransform;
            }

            if ( textMapName == null )
            {
                Transform mapNameTransform = transform.Find( MapNameTextObjectName );
                TMP_Text resolvedTextMapName = mapNameTransform != null ? mapNameTransform.GetComponent<TMP_Text>() : null;
                textMapName = resolvedTextMapName;
            }

            if ( viewportRectTransform == null )
            {
                Transform viewportTransform = ResolveParentRectTransform().Find( ViewportObjectName );
                viewportRectTransform = viewportTransform as RectTransform;
            }

            if ( viewportBackgroundImage == null )
            {
                Transform viewportBackgroundTransform = viewportRectTransform != null ? viewportRectTransform.Find( BackgroundImageObjectName ) : null;
                Image resolvedViewportBackgroundImage = viewportBackgroundTransform != null ? viewportBackgroundTransform.GetComponent<Image>() : null;
                viewportBackgroundImage = resolvedViewportBackgroundImage;
            }

            if ( iconRootRectTransform == null )
            {
                Transform iconRootTransform = viewportRectTransform != null ? viewportRectTransform.Find( IconRootObjectName ) : null;
                iconRootRectTransform = iconRootTransform as RectTransform;
            }

            if ( playerIconImage == null )
            {
                Transform playerIconTransform = iconRootRectTransform != null ? iconRootRectTransform.Find( PlayerIconObjectName ) : null;
                Image resolvedPlayerIconImage = playerIconTransform != null ? playerIconTransform.GetComponent<Image>() : null;
                playerIconImage = resolvedPlayerIconImage;
            }
        }

        ///<summary>
        /// 런타임 UI 계층 보장
        ///</summary>
        private void EnsureRuntimeHierarchy()
        {
            RectTransform parentRectTransform = ResolveParentRectTransform();

            if ( parentRectTransform == null )
            {
                return;
            }

            viewportRectTransform = EnsureRectChild( viewportRectTransform, parentRectTransform, ViewportObjectName );
            ConfigureViewportRect( viewportRectTransform );
            EnsureViewportGraphic( viewportRectTransform );
            EnsureViewportMask( viewportRectTransform );
            viewportBackgroundImage = EnsureViewportBackgroundImage();
            iconRootRectTransform = EnsureRectChild( iconRootRectTransform, viewportRectTransform, IconRootObjectName );
            StretchRectTransform( iconRootRectTransform );
            playerIconImage = EnsurePlayerIconImage();
        }

        ///<summary>
        /// 미니맵 기준 부모 RectTransform 결정
        ///</summary>
        private RectTransform ResolveParentRectTransform()
        {
            RectTransform result = backgroundRectTransform != null ? backgroundRectTransform : transform as RectTransform;
            return result;
        }

        ///<summary>
        /// 사각 UI 자식 보장
        ///</summary>
        private RectTransform EnsureRectChild( RectTransform _targetRectTransform, RectTransform _parentRectTransform, string _childName )
        {
            if ( _targetRectTransform != null )
            {
                return _targetRectTransform;
            }

            Transform existingChildTransform = _parentRectTransform.Find( _childName );

            if ( existingChildTransform != null )
            {
                RectTransform existingRectTransform = existingChildTransform as RectTransform;
                return existingRectTransform;
            }

            GameObject childObject = new GameObject( _childName, typeof( RectTransform ) );
            RectTransform childRectTransform = childObject.GetComponent<RectTransform>();
            childRectTransform.SetParent( _parentRectTransform, false );
            return childRectTransform;
        }

        ///<summary>
        /// 뷰포트 레이아웃 구성
        ///</summary>
        private void ConfigureViewportRect( RectTransform _viewportRectTransform )
        {
            if ( _viewportRectTransform == null )
            {
                return;
            }

            StretchRectTransform( _viewportRectTransform );
            _viewportRectTransform.offsetMin = new Vector2( minimapPadding.x, minimapPadding.y );
            _viewportRectTransform.offsetMax = new Vector2( -minimapPadding.x, -minimapPadding.y );
        }

        ///<summary>
        /// RectTransform 전체 스트레치 적용
        ///</summary>
        private void StretchRectTransform( RectTransform _targetRectTransform )
        {
            if ( _targetRectTransform == null )
            {
                return;
            }

            _targetRectTransform.anchorMin = Vector2.zero;
            _targetRectTransform.anchorMax = Vector2.one;
            _targetRectTransform.pivot = new Vector2( 0.5f, 0.5f );
            _targetRectTransform.anchoredPosition = Vector2.zero;
            _targetRectTransform.sizeDelta = Vector2.zero;
            _targetRectTransform.offsetMin = Vector2.zero;
            _targetRectTransform.offsetMax = Vector2.zero;
            _targetRectTransform.localScale = Vector3.one;
        }

        ///<summary>
        /// 뷰포트 오버레이 그래픽 보장
        ///</summary>
        private void EnsureViewportGraphic( RectTransform _viewportRectTransform )
        {
            if ( _viewportRectTransform == null )
            {
                return;
            }

            Image viewportImage = _viewportRectTransform.GetComponent<Image>();

            if ( viewportImage == null )
            {
                viewportImage = _viewportRectTransform.gameObject.AddComponent<Image>();
            }

            viewportImage.sprite = GetSolidSprite();
            viewportImage.type = Image.Type.Simple;
            viewportImage.color = viewportColor;
            viewportImage.raycastTarget = false;
        }

        ///<summary>
        /// 뷰포트 마스크 보장
        ///</summary>
        private void EnsureViewportMask( RectTransform _viewportRectTransform )
        {
            if ( _viewportRectTransform == null )
            {
                return;
            }

            RectMask2D rectMask = _viewportRectTransform.GetComponent<RectMask2D>();

            if ( rectMask == null )
            {
                _viewportRectTransform.gameObject.AddComponent<RectMask2D>();
            }
        }

        ///<summary>
        /// 배경 스프라이트 이미지 보장
        ///</summary>
        private Image EnsureViewportBackgroundImage()
        {
            if ( viewportRectTransform == null )
            {
                return null;
            }

            Transform existingBackgroundTransform = viewportRectTransform.Find( BackgroundImageObjectName );
            Image targetBackgroundImage = viewportBackgroundImage;

            if ( targetBackgroundImage == null && existingBackgroundTransform != null )
            {
                targetBackgroundImage = existingBackgroundTransform.GetComponent<Image>();
            }

            if ( targetBackgroundImage == null )
            {
                GameObject backgroundImageObject = new GameObject( BackgroundImageObjectName, typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
                RectTransform backgroundImageRectTransform = backgroundImageObject.GetComponent<RectTransform>();
                backgroundImageRectTransform.SetParent( viewportRectTransform, false );
                targetBackgroundImage = backgroundImageObject.GetComponent<Image>();
            }

            RectTransform targetRectTransform = targetBackgroundImage.rectTransform;
            StretchRectTransform( targetRectTransform );
            targetBackgroundImage.type = Image.Type.Simple;
            targetBackgroundImage.preserveAspect = true;
            targetBackgroundImage.color = backgroundTintColor;
            targetBackgroundImage.raycastTarget = false;
            viewportBackgroundImage = targetBackgroundImage;
            return viewportBackgroundImage;
        }

        ///<summary>
        /// 플레이어 아이콘 보장
        ///</summary>
        private Image EnsurePlayerIconImage()
        {
            if ( iconRootRectTransform == null )
            {
                return null;
            }

            if ( playerIconImage != null )
            {
                ConfigureCharacterIcon( playerIconImage, playerIconSize, playerIconColor );
                return playerIconImage;
            }

            Transform existingPlayerIconTransform = iconRootRectTransform.Find( PlayerIconObjectName );

            if ( existingPlayerIconTransform != null )
            {
                Image existingPlayerIconImage = existingPlayerIconTransform.GetComponent<Image>();
                playerIconImage = existingPlayerIconImage;
                ConfigureCharacterIcon( playerIconImage, playerIconSize, playerIconColor );
                return playerIconImage;
            }

            GameObject playerIconObject = new GameObject( PlayerIconObjectName, typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform playerIconRectTransform = playerIconObject.GetComponent<RectTransform>();
            playerIconRectTransform.SetParent( iconRootRectTransform, false );
            Image createdPlayerIconImage = playerIconObject.GetComponent<Image>();
            playerIconImage = createdPlayerIconImage;
            ConfigureCharacterIcon( playerIconImage, playerIconSize, playerIconColor );
            return playerIconImage;
        }

        ///<summary>
        /// 캐릭터 아이콘 외형 구성
        ///</summary>
        private void ConfigureCharacterIcon( Image _targetImage, Vector2 _iconSize, Color _iconColor )
        {
            if ( _targetImage == null )
            {
                return;
            }

            RectTransform targetRectTransform = _targetImage.rectTransform;
            targetRectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
            targetRectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
            targetRectTransform.pivot = new Vector2( 0.5f, 0.5f );
            targetRectTransform.anchoredPosition = Vector2.zero;
            targetRectTransform.sizeDelta = _iconSize;
            targetRectTransform.localScale = Vector3.one;
            targetRectTransform.localRotation = Quaternion.identity;
            _targetImage.sprite = GetCircleSprite();
            _targetImage.color = _iconColor;
            _targetImage.raycastTarget = false;
            _targetImage.enabled = false;
            EnsureCharacterIconOutline( _targetImage );
        }

        ///<summary>
        /// 캐릭터 아이콘 풀 초기화
        ///</summary>
        private void EnsureIconPoolsInitialized()
        {
            CObjectPoolManager.TryEnsurePoolRegistered<Image>( monsterIconPoolKey, CreateMonsterIcon, OnGetMonsterIcon, OnReleaseCharacterIcon, OnDestroyCharacterIcon );
            CObjectPoolManager.TryEnsurePoolRegistered<Image>( npcIconPoolKey, CreateNpcIcon, OnGetNpcIcon, OnReleaseCharacterIcon, OnDestroyCharacterIcon );
            CObjectPoolManager.TryEnsurePoolRegistered<Image>( portalIconPoolKey, CreatePortalIcon, OnGetPortalIcon, OnReleaseCharacterIcon, OnDestroyCharacterIcon );
        }

        ///<summary>
        /// 몬스터 아이콘 인스턴스 생성
        ///</summary>
        private Image CreateMonsterIcon()
        {
            Image createdImage = CreateCharacterIconInstance( "MonsterIcon", monsterIconSize, monsterIconColor );
            return createdImage;
        }

        ///<summary>
        /// NPC 아이콘 인스턴스 생성
        ///</summary>
        private Image CreateNpcIcon()
        {
            Image createdImage = CreateCharacterIconInstance( "NpcIcon", npcIconSize, npcIconColor );
            return createdImage;
        }

        ///<summary>
        /// 포탈 아이콘 인스턴스 생성
        ///</summary>
        private Image CreatePortalIcon()
        {
            Image createdImage = CreateCharacterIconInstance( "PortalIcon", portalIconSize, portalIconColor );
            return createdImage;
        }

        ///<summary>
        /// 캐릭터 아이콘 인스턴스 생성
        ///</summary>
        private Image CreateCharacterIconInstance( string _objectName, Vector2 _iconSize, Color _iconColor )
        {
            if ( iconRootRectTransform == null )
            {
                return null;
            }

            GameObject iconObject = new GameObject( _objectName, typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
            RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
            iconRectTransform.SetParent( iconRootRectTransform, false );
            Image iconImage = iconObject.GetComponent<Image>();
            ConfigureCharacterIcon( iconImage, _iconSize, _iconColor );
            iconObject.SetActive( false );
            return iconImage;
        }

        ///<summary>
        /// 몬스터 아이콘 대여 후처리
        ///</summary>
        private void OnGetMonsterIcon( Image _monsterIconImage )
        {
            ConfigureRentedCharacterIcon( _monsterIconImage, monsterIconSize, monsterIconColor );
        }

        ///<summary>
        /// NPC 아이콘 대여 후처리
        ///</summary>
        private void OnGetNpcIcon( Image _npcIconImage )
        {
            ConfigureRentedCharacterIcon( _npcIconImage, npcIconSize, npcIconColor );
        }

        ///<summary>
        /// 포탈 아이콘 대여 후처리
        ///</summary>
        private void OnGetPortalIcon( Image _portalIconImage )
        {
            ConfigureRentedCharacterIcon( _portalIconImage, portalIconSize, portalIconColor );
        }

        ///<summary>
        /// 대여 아이콘 표시 구성
        ///</summary>
        private void ConfigureRentedCharacterIcon( Image _targetImage, Vector2 _iconSize, Color _iconColor )
        {
            if ( _targetImage == null )
            {
                return;
            }

            _targetImage.rectTransform.SetParent( iconRootRectTransform, false );
            ConfigureCharacterIcon( _targetImage, _iconSize, _iconColor );
            _targetImage.gameObject.SetActive( true );
            _targetImage.enabled = true;
        }

        ///<summary>
        /// 캐릭터 아이콘 외곽선 보장
        ///</summary>
        private void EnsureCharacterIconOutline( Image _targetImage )
        {
            if ( _targetImage == null )
            {
                return;
            }

            Outline targetOutline = _targetImage.GetComponent<Outline>();

            if ( targetOutline == null )
            {
                targetOutline = _targetImage.gameObject.AddComponent<Outline>();
            }

            targetOutline.effectColor = DefaultIconOutlineColor;
            targetOutline.effectDistance = new Vector2( DefaultIconOutlineDistance, -DefaultIconOutlineDistance );
            targetOutline.useGraphicAlpha = true;
        }

        ///<summary>
        /// 캐릭터 아이콘 반환 후처리
        ///</summary>
        private void OnReleaseCharacterIcon( Image _targetImage )
        {
            if ( _targetImage == null )
            {
                return;
            }

            _targetImage.enabled = false;
            _targetImage.gameObject.SetActive( false );
        }

        ///<summary>
        /// 캐릭터 아이콘 파괴 처리
        ///</summary>
        private void OnDestroyCharacterIcon( Image _targetImage )
        {
            if ( _targetImage == null )
            {
                return;
            }

            Destroy( _targetImage.gameObject );
        }

        ///<summary>
        /// 현재 맵 이름 텍스트 갱신
        ///</summary>
        private void RefreshMapNameText()
        {
            if ( textMapName == null )
            {
                return;
            }

            if ( CMapManager.TryGetInstance( out CMapManager mapManager ) == false || mapManager == null )
            {
                return;
            }

            string currentMapName = mapManager.GetCurrentMapName();

            if ( string.IsNullOrWhiteSpace( currentMapName ) )
            {
                return;
            }

            textMapName.text = currentMapName;
        }

        ///<summary>
        /// 맵 배경 이미지 갱신
        ///</summary>
        private void RefreshBackgroundImage()
        {
            if ( viewportBackgroundImage == null )
            {
                return;
            }

            if ( CMapManager.TryGetInstance( out CMapManager mapManager ) == false || mapManager == null )
            {
                viewportBackgroundImage.enabled = false;
                currentMinimapBackgroundSprite = null;
                return;
            }

            Sprite currentBackgroundSprite = mapManager.GetCurrentBackgroundSprite();

            if ( currentBackgroundSprite == null )
            {
                viewportBackgroundImage.enabled = false;
                currentMinimapBackgroundSprite = null;
                return;
            }

            if ( currentMinimapBackgroundSprite != currentBackgroundSprite )
            {
                currentMinimapBackgroundSprite = currentBackgroundSprite;
                viewportBackgroundImage.sprite = currentBackgroundSprite;
            }

            viewportBackgroundImage.color = backgroundTintColor;
            viewportBackgroundImage.enabled = true;
        }

        ///<summary>
        /// 플레이어 제어 컴포넌트 탐색
        ///</summary>
        private void ResolvePlayerController()
        {
            if ( targetPlayerController != null && targetPlayerController.gameObject.activeInHierarchy && targetPlayerController.enabled )
            {
                return;
            }

            PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int playerControllerCount = playerControllerArray.Length;
            targetPlayerController = null;

            for ( int index = 0; index < playerControllerCount; index++ )
            {
                PlayerController playerController = playerControllerArray[ index ];

                if ( playerController == null || playerController.enabled == false || playerController.gameObject.activeInHierarchy == false )
                {
                    continue;
                }

                targetPlayerController = playerController;
                return;
            }
        }

        ///<summary>
        /// 맵 경계 갱신 타이머 처리
        ///</summary>
        private void TickBoundsRefresh()
        {
            remainingBoundsRefreshTime -= Time.unscaledDeltaTime;

            if ( remainingBoundsRefreshTime > 0.0f )
            {
                return;
            }

            remainingBoundsRefreshTime = Mathf.Max( 0.1f, boundsRefreshInterval );
            RefreshMapBounds();
        }

        ///<summary>
        /// 캐릭터 탐색 타이머 처리
        ///</summary>
        private void TickCharacterRefresh()
        {
            remainingCharacterScanTime -= Time.unscaledDeltaTime;

            if ( remainingCharacterScanTime > 0.0f )
            {
                return;
            }

            remainingCharacterScanTime = Mathf.Max( 0.1f, characterScanInterval );
            RefreshTrackedCharacters();
        }

        ///<summary>
        /// 현재 맵 경계 재계산
        ///</summary>
        private void RefreshMapBounds()
        {
            if ( CMapManager.TryGetInstance( out CMapManager mapManager ) && mapManager != null )
            {
                bool hasBackgroundBounds = mapManager.TryGetCurrentBackgroundBounds( out Bounds backgroundBounds );

                if ( hasBackgroundBounds )
                {
                    cachedMapBounds = NormalizeBoundsSize( backgroundBounds );
                    hasCachedMapBounds = true;
                    return;
                }
            }

            Bounds nextBounds = new Bounds();
            bool hasAnyBounds = false;
            Collider2D[] colliderArray = FindObjectsByType<Collider2D>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int colliderCount = colliderArray.Length;

            for ( int index = 0; index < colliderCount; index++ )
            {
                Collider2D collider = colliderArray[ index ];

                if ( ShouldIncludeBoundsSource( collider ) == false )
                {
                    continue;
                }

                AppendBounds( collider.bounds, ref nextBounds, ref hasAnyBounds );
            }

            if ( hasAnyBounds == false )
            {
                SpriteRenderer[] spriteRendererArray = FindObjectsByType<SpriteRenderer>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
                int spriteRendererCount = spriteRendererArray.Length;

                for ( int index = 0; index < spriteRendererCount; index++ )
                {
                    SpriteRenderer spriteRenderer = spriteRendererArray[ index ];

                    if ( ShouldIncludeBoundsSource( spriteRenderer ) == false )
                    {
                        continue;
                    }

                    if ( string.Equals( spriteRenderer.sortingLayerName, "SkillEffect", System.StringComparison.Ordinal ) )
                    {
                        continue;
                    }

                    AppendBounds( spriteRenderer.bounds, ref nextBounds, ref hasAnyBounds );
                }
            }

            if ( hasAnyBounds == false )
            {
                Bounds fallbackBounds = BuildFallbackBounds();
                cachedMapBounds = fallbackBounds;
                hasCachedMapBounds = true;
                return;
            }

            cachedMapBounds = NormalizeBoundsSize( nextBounds );
            hasCachedMapBounds = true;
        }

        ///<summary>
        /// 경계 포함 대상 판정
        ///</summary>
        private bool ShouldIncludeBoundsSource( Component _targetComponent )
        {
            if ( _targetComponent == null )
            {
                return false;
            }

            GameObject targetObject = _targetComponent.gameObject;

            if ( targetObject.activeInHierarchy == false )
            {
                return false;
            }

            if ( targetObject.layer == LayerMask.NameToLayer( "UI" ) )
            {
                return false;
            }

            if ( targetObject.GetComponentInParent<Canvas>() != null )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// 경계 누적 반영
        ///</summary>
        private void AppendBounds( Bounds _sourceBounds, ref Bounds _targetBounds, ref bool _hasAnyBounds )
        {
            if ( _sourceBounds.size.sqrMagnitude <= 0.0001f )
            {
                return;
            }

            if ( _hasAnyBounds == false )
            {
                _targetBounds = _sourceBounds;
                _hasAnyBounds = true;
                return;
            }

            _targetBounds.Encapsulate( _sourceBounds.min );
            _targetBounds.Encapsulate( _sourceBounds.max );
        }

        ///<summary>
        /// 최소 크기 보정 경계 생성
        ///</summary>
        private Bounds NormalizeBoundsSize( Bounds _sourceBounds )
        {
            Vector3 normalizedSize = _sourceBounds.size;
            normalizedSize.x = Mathf.Max( MinimumBoundsSize, normalizedSize.x );
            normalizedSize.y = Mathf.Max( MinimumBoundsSize, normalizedSize.y );
            normalizedSize.z = Mathf.Max( 1.0f, normalizedSize.z );
            Bounds result = new Bounds( _sourceBounds.center, normalizedSize );
            return result;
        }

        ///<summary>
        /// 플레이어 기준 대체 경계 생성
        ///</summary>
        private Bounds BuildFallbackBounds()
        {
            Vector3 centerPosition = targetPlayerController != null ? targetPlayerController.transform.position : Vector3.zero;
            Vector3 fallbackSize = new Vector3( MinimumBoundsSize, MinimumBoundsSize, 1.0f );
            Bounds result = new Bounds( centerPosition, fallbackSize );
            return result;
        }

        ///<summary>
        /// 추적 캐릭터 목록 갱신
        ///</summary>
        private void RefreshTrackedCharacters()
        {
            RefreshTrackedMonsters();
            RefreshTrackedNpcs();
            RefreshTrackedPortals();
        }

        ///<summary>
        /// 추적 몬스터 목록 갱신
        ///</summary>
        private void RefreshTrackedMonsters()
        {
            trackedMonsterList.Clear();
            MonsterObject[] monsterObjectArray = FindObjectsByType<MonsterObject>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int monsterCount = monsterObjectArray.Length;

            for ( int index = 0; index < monsterCount; index++ )
            {
                MonsterObject monsterObject = monsterObjectArray[ index ];

                if ( IsTrackableMonster( monsterObject ) == false )
                {
                    continue;
                }

                trackedMonsterList.Add( monsterObject );
                EnsureMonsterIconAssigned( monsterObject );
            }

            CollectReleasedMonsterTargets();
            ReleaseCollectedMonsterIcons();
        }

        ///<summary>
        /// 추적 NPC 목록 갱신
        ///</summary>
        private void RefreshTrackedNpcs()
        {
            trackedNpcList.Clear();
            CNPCObject[] npcObjectArray = FindObjectsByType<CNPCObject>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int npcCount = npcObjectArray.Length;

            for ( int index = 0; index < npcCount; index++ )
            {
                CNPCObject npcObject = npcObjectArray[ index ];

                if ( IsTrackableNpc( npcObject ) == false )
                {
                    continue;
                }

                trackedNpcList.Add( npcObject );
                EnsureNpcIconAssigned( npcObject );
            }

            CollectReleasedNpcTargets();
            ReleaseCollectedNpcIcons();
        }

        ///<summary>
        /// 추적 포탈 목록 갱신
        ///</summary>
        private void RefreshTrackedPortals()
        {
            trackedPortalList.Clear();
            PortalObject[] portalObjectArray = FindObjectsByType<PortalObject>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            int portalCount = portalObjectArray.Length;

            for ( int index = 0; index < portalCount; index++ )
            {
                PortalObject portalObject = portalObjectArray[ index ];

                if ( IsTrackablePortal( portalObject ) == false )
                {
                    continue;
                }

                trackedPortalList.Add( portalObject );
                EnsurePortalIconAssigned( portalObject );
            }

            CollectReleasedPortalTargets();
            ReleaseCollectedPortalIcons();
        }

        ///<summary>
        /// 추적 가능한 몬스터 판정
        ///</summary>
        private bool IsTrackableMonster( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return false;
            }

            if ( _monsterObject.gameObject.activeInHierarchy == false || _monsterObject.enabled == false )
            {
                return false;
            }

            if ( _monsterObject.GetCurrentHp() <= 0 )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// 추적 가능한 NPC 판정
        ///</summary>
        private bool IsTrackableNpc( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return false;
            }

            if ( _npcObject.gameObject.activeInHierarchy == false || _npcObject.enabled == false )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// 추적 가능한 포탈 판정
        ///</summary>
        private bool IsTrackablePortal( PortalObject _portalObject )
        {
            if ( _portalObject == null )
            {
                return false;
            }

            if ( _portalObject.gameObject.activeInHierarchy == false || _portalObject.enabled == false )
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// 몬스터 아이콘 연결 보장
        ///</summary>
        private void EnsureMonsterIconAssigned( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null || monsterIconByMonster.ContainsKey( _monsterObject ) )
            {
                return;
            }

            EnsureIconPoolsInitialized();

            if ( CObjectPoolManager.TryGet( monsterIconPoolKey, out Image monsterIconImage ) == false || monsterIconImage == null )
            {
                return;
            }

            monsterIconByMonster.Add( _monsterObject, monsterIconImage );
        }

        ///<summary>
        /// NPC 아이콘 연결 보장
        ///</summary>
        private void EnsureNpcIconAssigned( CNPCObject _npcObject )
        {
            if ( _npcObject == null || npcIconByNpc.ContainsKey( _npcObject ) )
            {
                return;
            }

            EnsureIconPoolsInitialized();

            if ( CObjectPoolManager.TryGet( npcIconPoolKey, out Image npcIconImage ) == false || npcIconImage == null )
            {
                return;
            }

            npcIconByNpc.Add( _npcObject, npcIconImage );
        }

        ///<summary>
        /// 포탈 아이콘 연결 보장
        ///</summary>
        private void EnsurePortalIconAssigned( PortalObject _portalObject )
        {
            if ( _portalObject == null || portalIconByPortal.ContainsKey( _portalObject ) )
            {
                return;
            }

            EnsureIconPoolsInitialized();

            if ( CObjectPoolManager.TryGet( portalIconPoolKey, out Image portalIconImage ) == false || portalIconImage == null )
            {
                return;
            }

            portalIconByPortal.Add( _portalObject, portalIconImage );
        }

        ///<summary>
        /// 반환 대상 몬스터 수집
        ///</summary>
        private void CollectReleasedMonsterTargets()
        {
            releaseTargetMonsterList.Clear();

            foreach ( KeyValuePair<MonsterObject, Image> pairData in monsterIconByMonster )
            {
                MonsterObject monsterObject = pairData.Key;

                if ( trackedMonsterList.Contains( monsterObject ) )
                {
                    continue;
                }

                releaseTargetMonsterList.Add( monsterObject );
            }
        }

        ///<summary>
        /// 반환 대상 NPC 수집
        ///</summary>
        private void CollectReleasedNpcTargets()
        {
            releaseTargetNpcList.Clear();

            foreach ( KeyValuePair<CNPCObject, Image> pairData in npcIconByNpc )
            {
                CNPCObject npcObject = pairData.Key;

                if ( trackedNpcList.Contains( npcObject ) )
                {
                    continue;
                }

                releaseTargetNpcList.Add( npcObject );
            }
        }

        ///<summary>
        /// 반환 대상 포탈 수집
        ///</summary>
        private void CollectReleasedPortalTargets()
        {
            releaseTargetPortalList.Clear();

            foreach ( KeyValuePair<PortalObject, Image> pairData in portalIconByPortal )
            {
                PortalObject portalObject = pairData.Key;

                if ( trackedPortalList.Contains( portalObject ) )
                {
                    continue;
                }

                releaseTargetPortalList.Add( portalObject );
            }
        }

        ///<summary>
        /// 수집된 몬스터 아이콘 반환
        ///</summary>
        private void ReleaseCollectedMonsterIcons()
        {
            int releaseTargetCount = releaseTargetMonsterList.Count;

            for ( int index = 0; index < releaseTargetCount; index++ )
            {
                MonsterObject monsterObject = releaseTargetMonsterList[ index ];
                ReleaseMonsterIcon( monsterObject );
            }

            releaseTargetMonsterList.Clear();
        }

        ///<summary>
        /// 수집된 NPC 아이콘 반환
        ///</summary>
        private void ReleaseCollectedNpcIcons()
        {
            int releaseTargetCount = releaseTargetNpcList.Count;

            for ( int index = 0; index < releaseTargetCount; index++ )
            {
                CNPCObject npcObject = releaseTargetNpcList[ index ];
                ReleaseNpcIcon( npcObject );
            }

            releaseTargetNpcList.Clear();
        }

        ///<summary>
        /// 수집된 포탈 아이콘 반환
        ///</summary>
        private void ReleaseCollectedPortalIcons()
        {
            int releaseTargetCount = releaseTargetPortalList.Count;

            for ( int index = 0; index < releaseTargetCount; index++ )
            {
                PortalObject portalObject = releaseTargetPortalList[ index ];
                ReleasePortalIcon( portalObject );
            }

            releaseTargetPortalList.Clear();
        }

        ///<summary>
        /// 전체 몬스터 아이콘 반환
        ///</summary>
        private void ReleaseAllMonsterIcons()
        {
            releaseTargetMonsterList.Clear();

            foreach ( KeyValuePair<MonsterObject, Image> pairData in monsterIconByMonster )
            {
                releaseTargetMonsterList.Add( pairData.Key );
            }

            ReleaseCollectedMonsterIcons();
        }

        ///<summary>
        /// 전체 NPC 아이콘 반환
        ///</summary>
        private void ReleaseAllNpcIcons()
        {
            releaseTargetNpcList.Clear();

            foreach ( KeyValuePair<CNPCObject, Image> pairData in npcIconByNpc )
            {
                releaseTargetNpcList.Add( pairData.Key );
            }

            ReleaseCollectedNpcIcons();
        }

        ///<summary>
        /// 전체 포탈 아이콘 반환
        ///</summary>
        private void ReleaseAllPortalIcons()
        {
            releaseTargetPortalList.Clear();

            foreach ( KeyValuePair<PortalObject, Image> pairData in portalIconByPortal )
            {
                releaseTargetPortalList.Add( pairData.Key );
            }

            ReleaseCollectedPortalIcons();
        }

        ///<summary>
        /// 단일 몬스터 아이콘 반환
        ///</summary>
        private void ReleaseMonsterIcon( MonsterObject _monsterObject )
        {
            if ( _monsterObject == null )
            {
                return;
            }

            bool hasIcon = monsterIconByMonster.TryGetValue( _monsterObject, out Image monsterIconImage );

            if ( hasIcon == false )
            {
                return;
            }

            monsterIconByMonster.Remove( _monsterObject );

            if ( monsterIconImage != null )
            {
                CObjectPoolManager.TryRelease( monsterIconPoolKey, monsterIconImage );
            }
        }

        ///<summary>
        /// 단일 NPC 아이콘 반환
        ///</summary>
        private void ReleaseNpcIcon( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return;
            }

            bool hasIcon = npcIconByNpc.TryGetValue( _npcObject, out Image npcIconImage );

            if ( hasIcon == false )
            {
                return;
            }

            npcIconByNpc.Remove( _npcObject );

            if ( npcIconImage != null )
            {
                CObjectPoolManager.TryRelease( npcIconPoolKey, npcIconImage );
            }
        }

        ///<summary>
        /// 단일 포탈 아이콘 반환
        ///</summary>
        private void ReleasePortalIcon( PortalObject _portalObject )
        {
            if ( _portalObject == null )
            {
                return;
            }

            bool hasIcon = portalIconByPortal.TryGetValue( _portalObject, out Image portalIconImage );

            if ( hasIcon == false )
            {
                return;
            }

            portalIconByPortal.Remove( _portalObject );

            if ( portalIconImage != null )
            {
                CObjectPoolManager.TryRelease( portalIconPoolKey, portalIconImage );
            }
        }

        ///<summary>
        /// 플레이어 아이콘 갱신
        ///</summary>
        private void UpdatePlayerIcon()
        {
            if ( playerIconImage == null )
            {
                return;
            }

            if ( targetPlayerController == null || hasCachedMapBounds == false )
            {
                playerIconImage.enabled = false;
                return;
            }

            Vector2 anchoredPosition = ConvertWorldPositionToAnchoredPosition( targetPlayerController.transform.position );
            playerIconImage.enabled = true;
            playerIconImage.rectTransform.anchoredPosition = anchoredPosition;
            playerIconImage.color = playerIconColor;
            playerIconImage.rectTransform.localScale = Vector3.one;
        }

        ///<summary>
        /// 몬스터 아이콘 갱신
        ///</summary>
        private void UpdateMonsterIcons()
        {
            if ( hasCachedMapBounds == false )
            {
                return;
            }

            int trackedMonsterCount = trackedMonsterList.Count;

            for ( int index = 0; index < trackedMonsterCount; index++ )
            {
                MonsterObject monsterObject = trackedMonsterList[ index ];

                if ( monsterObject == null )
                {
                    continue;
                }

                bool hasIcon = monsterIconByMonster.TryGetValue( monsterObject, out Image monsterIconImage );

                if ( hasIcon == false || monsterIconImage == null )
                {
                    continue;
                }

                UpdateCharacterIconPosition( monsterIconImage, monsterObject.transform.position, monsterIconColor );
            }
        }

        ///<summary>
        /// NPC 아이콘 갱신
        ///</summary>
        private void UpdateNpcIcons()
        {
            if ( hasCachedMapBounds == false )
            {
                return;
            }

            int trackedNpcCount = trackedNpcList.Count;

            for ( int index = 0; index < trackedNpcCount; index++ )
            {
                CNPCObject npcObject = trackedNpcList[ index ];

                if ( npcObject == null )
                {
                    continue;
                }

                bool hasIcon = npcIconByNpc.TryGetValue( npcObject, out Image npcIconImage );

                if ( hasIcon == false || npcIconImage == null )
                {
                    continue;
                }

                UpdateCharacterIconPosition( npcIconImage, npcObject.transform.position, npcIconColor );
            }
        }

        ///<summary>
        /// 포탈 아이콘 갱신
        ///</summary>
        private void UpdatePortalIcons()
        {
            if ( hasCachedMapBounds == false )
            {
                return;
            }

            int trackedPortalCount = trackedPortalList.Count;

            for ( int index = 0; index < trackedPortalCount; index++ )
            {
                PortalObject portalObject = trackedPortalList[ index ];

                if ( portalObject == null )
                {
                    continue;
                }

                bool hasIcon = portalIconByPortal.TryGetValue( portalObject, out Image portalIconImage );

                if ( hasIcon == false || portalIconImage == null )
                {
                    continue;
                }

                UpdateCharacterIconPosition( portalIconImage, portalObject.transform.position, portalIconColor );
            }
        }

        ///<summary>
        /// 캐릭터 아이콘 위치와 투명도 갱신
        ///</summary>
        private void UpdateCharacterIconPosition( Image _targetImage, Vector3 _worldPosition, Color _baseColor )
        {
            if ( _targetImage == null )
            {
                return;
            }

            Vector2 anchoredPosition = ConvertWorldPositionToAnchoredPosition( _worldPosition );
            _targetImage.rectTransform.anchoredPosition = anchoredPosition;
            _targetImage.rectTransform.localScale = Vector3.one;
            Color iconColor = _baseColor;
            bool isNearMapEdge = IsNearMapEdge( anchoredPosition );
            iconColor.a = isNearMapEdge ? DefaultEdgeIconAlpha : DefaultVisibleIconAlpha;
            _targetImage.color = iconColor;
            _targetImage.enabled = true;
        }

        ///<summary>
        /// 맵 외곽 근접 아이콘 판정
        ///</summary>
        private bool IsNearMapEdge( Vector2 _anchoredPosition )
        {
            if ( viewportRectTransform == null )
            {
                return false;
            }

            Rect displayedMapRect = ResolveDisplayedMapRect();
            float edgeThresholdX = displayedMapRect.width * 0.48f;
            float edgeThresholdY = displayedMapRect.height * 0.48f;
            bool result = Mathf.Abs( _anchoredPosition.x ) >= edgeThresholdX || Mathf.Abs( _anchoredPosition.y ) >= edgeThresholdY;
            return result;
        }

        ///<summary>
        /// 월드 좌표의 미니맵 위치 변환
        ///</summary>
        private Vector2 ConvertWorldPositionToAnchoredPosition( Vector3 _worldPosition )
        {
            if ( hasCachedMapBounds == false || viewportRectTransform == null )
            {
                return Vector2.zero;
            }

            Rect displayedMapRect = ResolveDisplayedMapRect();
            float normalizedX = Mathf.InverseLerp( cachedMapBounds.min.x, cachedMapBounds.max.x, _worldPosition.x );
            float normalizedY = Mathf.InverseLerp( cachedMapBounds.min.y, cachedMapBounds.max.y, _worldPosition.y );
            Vector2 anchoredPosition = new Vector2(
                Mathf.Lerp( displayedMapRect.xMin, displayedMapRect.xMax, normalizedX ),
                Mathf.Lerp( displayedMapRect.yMin, displayedMapRect.yMax, normalizedY ) );
            return anchoredPosition;
        }

        ///<summary>
        /// 실제 표시 가능 미니맵 크기 계산
        ///</summary>
        private Vector2 ResolveAvailableMapSize()
        {
            if ( viewportRectTransform == null )
            {
                return Vector2.zero;
            }

            Rect viewportRect = viewportRectTransform.rect;
            float availableWidth = Mathf.Max( 1.0f, viewportRect.width );
            float availableHeight = Mathf.Max( 1.0f, viewportRect.height );

            if ( viewportBackgroundImage == null || viewportBackgroundImage.sprite == null || viewportBackgroundImage.preserveAspect == false )
            {
                Vector2 fallbackResult = new Vector2( availableWidth, availableHeight );
                return fallbackResult;
            }

            Rect spriteRect = viewportBackgroundImage.sprite.rect;
            float spriteWidth = Mathf.Max( 1.0f, spriteRect.width );
            float spriteHeight = Mathf.Max( 1.0f, spriteRect.height );
            float widthScale = availableWidth / spriteWidth;
            float heightScale = availableHeight / spriteHeight;
            float uniformScale = Mathf.Min( widthScale, heightScale );
            availableWidth = spriteWidth * uniformScale;
            availableHeight = spriteHeight * uniformScale;
            Vector2 result = new Vector2( availableWidth, availableHeight );
            return result;
        }

        ///<summary>
        /// 실제 표시 배경 기준 미니맵 영역 계산
        ///</summary>
        private Rect ResolveDisplayedMapRect()
        {
            Vector2 availableSize = ResolveAvailableMapSize();
            Rect result = new Rect(
                -availableSize.x * 0.5f,
                -availableSize.y * 0.5f,
                availableSize.x,
                availableSize.y );
            return result;
        }

        ///<summary>
        /// 원형 스프라이트 반환
        ///</summary>
        private Sprite GetCircleSprite()
        {
            if ( cachedCircleSprite != null )
            {
                return cachedCircleSprite;
            }

            Texture2D circleTexture = new Texture2D( CircleTextureSize, CircleTextureSize, TextureFormat.RGBA32, false );
            circleTexture.name = "Runtime_MinimapCircle";
            circleTexture.wrapMode = TextureWrapMode.Clamp;
            circleTexture.filterMode = FilterMode.Bilinear;
            Vector2 textureCenter = new Vector2( ( CircleTextureSize - 1 ) * 0.5f, ( CircleTextureSize - 1 ) * 0.5f );
            float radius = CircleTextureSize * 0.5f - 2.0f;
            float featherDistance = 1.75f;

            for ( int y = 0; y < CircleTextureSize; y++ )
            {
                for ( int x = 0; x < CircleTextureSize; x++ )
                {
                    Vector2 samplePoint = new Vector2( x, y );
                    float distance = Vector2.Distance( samplePoint, textureCenter );
                    float normalizedAlpha = Mathf.Clamp01( 1.0f - ( distance - radius ) / featherDistance );
                    Color pixelColor = new Color( 1.0f, 1.0f, 1.0f, normalizedAlpha );
                    circleTexture.SetPixel( x, y, pixelColor );
                }
            }

            circleTexture.Apply();
            Rect spriteRect = new Rect( 0.0f, 0.0f, CircleTextureSize, CircleTextureSize );
            Vector2 spritePivot = new Vector2( 0.5f, 0.5f );
            cachedCircleSprite = Sprite.Create( circleTexture, spriteRect, spritePivot, 100.0f );
            return cachedCircleSprite;
        }

        ///<summary>
        /// 단색 스프라이트 반환
        ///</summary>
        private Sprite GetSolidSprite()
        {
            if ( cachedSolidSprite != null )
            {
                return cachedSolidSprite;
            }

            Texture2D solidTexture = new Texture2D( SolidTextureSize, SolidTextureSize, TextureFormat.RGBA32, false );
            solidTexture.name = "Runtime_MinimapSolid";
            solidTexture.wrapMode = TextureWrapMode.Clamp;
            solidTexture.filterMode = FilterMode.Point;

            for ( int y = 0; y < SolidTextureSize; y++ )
            {
                for ( int x = 0; x < SolidTextureSize; x++ )
                {
                    solidTexture.SetPixel( x, y, Color.white );
                }
            }

            solidTexture.Apply();
            Rect spriteRect = new Rect( 0.0f, 0.0f, SolidTextureSize, SolidTextureSize );
            Vector2 spritePivot = new Vector2( 0.5f, 0.5f );
            cachedSolidSprite = Sprite.Create( solidTexture, spriteRect, spritePivot, 100.0f );
            return cachedSolidSprite;
        }
    }
}
