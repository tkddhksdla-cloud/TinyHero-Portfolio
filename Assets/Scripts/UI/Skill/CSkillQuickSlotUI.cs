using System.Collections.Generic;
using TinyHero.Player;
using TinyHero.Skill;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 퀵슬롯 영역 UI 제어 컴포넌트
    ///</summary>
    public sealed class CSkillQuickSlotUI : MonoBehaviour
    {
        private const float DragGhostAlpha = 0.55f;
        [SerializeField] private CSkillManager targetSkillManager;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private List<CSkillQuickSlotItemView> slotViewList = new List<CSkillQuickSlotItemView>();

        private RectTransform dragGhostRectTransform;
        private Image dragGhostImage;
        private string draggedSkillId = string.Empty;
        private int draggedFromQuickSlotIndex = -1;

        ///<summary>
        /// 퀵슬롯 UI 초기화
        ///</summary>
        private void Awake()
        {
            EnsureSkillManagerBinding();
            ResolveCanvas();
            CollectSlotViews();
            BindSlotViews();
        }

        ///<summary>
        /// 이벤트 구독 및 초기 갱신
        ///</summary>
        private void OnEnable()
        {
            EnsureSkillManagerBinding();
            ResolveCanvas();
            CollectSlotViews();
            BindSlotViews();
            RefreshView();
        }

        ///<summary>
        /// 이벤트 구독 해제
        ///</summary>
        private void OnDisable()
        {
            UnsubscribeEvents();
            EndSkillDragInternal();
        }

        ///<summary>
        /// 키 입력 및 쿨타임 표시 갱신
        ///</summary>
        private void Update()
        {
            EnsureSkillManagerBinding();
            ResolveCanvas();
            ProcessSlotInput();
            RefreshView();
            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 스킬 목록 기반 드래그 시작 처리
        ///</summary>
        public void TryBeginDragFromSkillList( string _skillId, PointerEventData _eventData )
        {
            if ( targetSkillManager == null || string.IsNullOrWhiteSpace( _skillId ) )
            {
                return;
            }

            bool canAssignSkill = targetSkillManager.CanAssignSkillToQuickSlot( _skillId, 0 );

            if ( canAssignSkill == false )
            {
                return;
            }

            BeginSkillDragInternal( _skillId, -1 );
        }

        ///<summary>
        /// 퀵슬롯 기반 드래그 시작 처리
        ///</summary>
        public void TryBeginDragFromQuickSlot( int _quickSlotIndex, PointerEventData _eventData )
        {
            if ( targetSkillManager == null || _quickSlotIndex < 0 )
            {
                return;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinitionByQuickSlotIndex( _quickSlotIndex );

            if ( skillDefinition == null )
            {
                return;
            }

            string skillId = skillDefinition.GetSkillId();
            bool isUnlocked = targetSkillManager.IsSkillUnlocked( skillId );

            if ( isUnlocked == false )
            {
                return;
            }

            BeginSkillDragInternal( skillId, _quickSlotIndex );
        }

        ///<summary>
        /// 스킬 드래그 진행 처리
        ///</summary>
        public void UpdateSkillDrag( PointerEventData _eventData )
        {
            if ( string.IsNullOrWhiteSpace( draggedSkillId ) )
            {
                return;
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 스킬 드래그 종료 처리
        ///</summary>
        public void EndSkillDrag( PointerEventData _eventData )
        {
            EndSkillDragInternal();
        }

        ///<summary>
        /// 퀵슬롯 드롭 처리
        ///</summary>
        public void HandleSlotDrop( int _quickSlotIndex )
        {
            if ( targetSkillManager == null || string.IsNullOrWhiteSpace( draggedSkillId ) || _quickSlotIndex < 0 )
            {
                return;
            }

            bool didProcess = false;

            if ( draggedFromQuickSlotIndex >= 0 )
            {
                if ( draggedFromQuickSlotIndex == _quickSlotIndex )
                {
                    didProcess = true;
                }
                else
                {
                    didProcess = targetSkillManager.TrySwapQuickSlotAssignments( draggedFromQuickSlotIndex, _quickSlotIndex );
                }
            }
            else
            {
                didProcess = targetSkillManager.TryAssignSkillToQuickSlot( draggedSkillId, _quickSlotIndex );
            }

            if ( didProcess )
            {
                RefreshView();
            }

            EndSkillDragInternal();
        }

        ///<summary>
        /// 스킬 상태 변경 반영
        ///</summary>
        private void HandleSkillStateChanged()
        {
            RefreshView();
        }

        ///<summary>
        /// 스킬 매니저 참조 결정
        ///</summary>
        private void ResolveSkillManager()
        {
            PlayerController[] playerControllerArray = FindObjectsByType<PlayerController>( FindObjectsInactive.Exclude, FindObjectsSortMode.None );
            CSkillManager resolvedSkillManager = null;
            int playerControllerCount = playerControllerArray.Length;

            for ( int index = 0; index < playerControllerCount; index++ )
            {
                PlayerController playerController = playerControllerArray[ index ];

                if ( playerController == null )
                {
                    continue;
                }

                if ( playerController.enabled == false || playerController.gameObject.activeInHierarchy == false )
                {
                    continue;
                }

                CSkillManager playerSkillManager = playerController.GetComponent<CSkillManager>();

                if ( playerSkillManager == null || playerSkillManager.enabled == false )
                {
                    continue;
                }

                resolvedSkillManager = playerSkillManager;
                break;
            }

            targetSkillManager = resolvedSkillManager;
        }

        ///<summary>
        /// 스킬 매니저 바인딩 최신 상태 보장
        ///</summary>
        private void EnsureSkillManagerBinding()
        {
            CSkillManager previousSkillManager = targetSkillManager;
            ResolveSkillManager();

            if ( previousSkillManager == targetSkillManager )
            {
                return;
            }

            if ( previousSkillManager != null )
            {
                previousSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
            }

            if ( targetSkillManager != null )
            {
                targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
                targetSkillManager.OnSkillStateChanged += HandleSkillStateChanged;
            }

            BindSlotViews();
        }

        ///<summary>
        /// 슬롯 뷰 바인딩 처리
        ///</summary>
        private void BindSlotViews()
        {
            for ( int index = 0; index < slotViewList.Count; index++ )
            {
                CSkillQuickSlotItemView slotView = slotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotView.Bind( this, targetSkillManager, index );
            }
        }

        ///<summary>
        /// 자식 슬롯 뷰 수집 처리
        ///</summary>
        private void CollectSlotViews()
        {
            if ( slotViewList == null )
            {
                slotViewList = new List<CSkillQuickSlotItemView>();
            }

            if ( slotViewList.Count > 0 )
            {
                return;
            }

            CSkillQuickSlotItemView[] collectedSlotViewArray = GetComponentsInChildren<CSkillQuickSlotItemView>( true );

            for ( int index = 0; index < collectedSlotViewArray.Length; index++ )
            {
                CSkillQuickSlotItemView slotView = collectedSlotViewArray[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotViewList.Add( slotView );
            }
        }

        ///<summary>
        /// 퀵슬롯 캔버스 참조 결정
        ///</summary>
        private void ResolveCanvas()
        {
            if ( targetCanvas != null )
            {
                return;
            }

            Canvas resolvedCanvas = GetComponentInParent<Canvas>();
            targetCanvas = resolvedCanvas;
        }

        ///<summary>
        /// 스킬 이벤트 구독
        ///</summary>
        private void SubscribeEvents()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
            targetSkillManager.OnSkillStateChanged += HandleSkillStateChanged;
        }

        ///<summary>
        /// 스킬 이벤트 구독 해제
        ///</summary>
        private void UnsubscribeEvents()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            targetSkillManager.OnSkillStateChanged -= HandleSkillStateChanged;
        }

        ///<summary>
        /// 퀵슬롯 입력 처리
        ///</summary>
        private void ProcessSlotInput()
        {
            if ( targetSkillManager == null )
            {
                return;
            }

            PlayerController targetPlayerController = targetSkillManager.GetComponent<PlayerController>();

            if ( targetPlayerController != null )
            {
                return;
            }

            for ( int index = 0; index < slotViewList.Count; index++ )
            {
                CSkillQuickSlotItemView slotView = slotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                bool shouldTrigger = slotView.IsTriggerRequested();

                if ( shouldTrigger == false )
                {
                    continue;
                }

                targetSkillManager.TryUseSkillByQuickSlotIndex( index );
            }
        }

        ///<summary>
        /// 퀵슬롯 전체 시각 상태 갱신
        ///</summary>
        public void RefreshView()
        {
            EnsureSkillManagerBinding();

            for ( int index = 0; index < slotViewList.Count; index++ )
            {
                CSkillQuickSlotItemView slotView = slotViewList[ index ];

                if ( slotView == null )
                {
                    continue;
                }

                slotView.RefreshView();
            }
        }

        ///<summary>
        /// 스킬 드래그 내부 시작 처리
        ///</summary>
        private void BeginSkillDragInternal( string _skillId, int _fromQuickSlotIndex )
        {
            if ( string.IsNullOrWhiteSpace( _skillId ) || targetSkillManager == null )
            {
                return;
            }

            CSkillDefinition skillDefinition = targetSkillManager.GetSkillDefinition( _skillId );

            if ( skillDefinition == null || skillDefinition.GetSkillType() != eSkillType.ACTIVE )
            {
                return;
            }

            EnsureDragGhost();
            draggedSkillId = _skillId.Trim();
            draggedFromQuickSlotIndex = _fromQuickSlotIndex;

            if ( dragGhostImage != null )
            {
                Sprite skillIconSprite = skillDefinition.GetSkillIcon();
                dragGhostImage.sprite = skillIconSprite;
                Color ghostColor = dragGhostImage.color;
                ghostColor.a = DragGhostAlpha;
                dragGhostImage.color = ghostColor;
                dragGhostImage.enabled = skillIconSprite != null;
            }

            if ( dragGhostRectTransform != null )
            {
                dragGhostRectTransform.SetAsLastSibling();
            }

            UpdateDragGhostPosition();
        }

        ///<summary>
        /// 스킬 드래그 내부 종료 처리
        ///</summary>
        private void EndSkillDragInternal()
        {
            draggedSkillId = string.Empty;
            draggedFromQuickSlotIndex = -1;

            if ( dragGhostImage != null )
            {
                dragGhostImage.enabled = false;
            }
        }

        ///<summary>
        /// 스킬 드래그 고스트 생성 보장
        ///</summary>
        private void EnsureDragGhost()
        {
            if ( dragGhostRectTransform != null && dragGhostImage != null )
            {
                return;
            }

            ResolveCanvas();
            RectTransform dragGhostParentRectTransform = ResolveDragGhostParentRectTransform();

            if ( dragGhostParentRectTransform == null )
            {
                return;
            }

            GameObject dragGhostObject = new GameObject( "SkillDragGhost", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ), typeof( LayoutElement ) );
            RectTransform rectTransform = dragGhostObject.GetComponent<RectTransform>();
            rectTransform.SetParent( dragGhostParentRectTransform, false );
            rectTransform.sizeDelta = new Vector2( 72.0f, 72.0f );
            Image image = dragGhostObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
            LayoutElement layoutElement = dragGhostObject.GetComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            rectTransform.SetAsLastSibling();
            dragGhostRectTransform = rectTransform;
            dragGhostImage = image;
        }

        ///<summary>
        /// 스킬 드래그 고스트 위치 갱신
        ///</summary>
        private void UpdateDragGhostPosition()
        {
            if ( dragGhostRectTransform == null || string.IsNullOrWhiteSpace( draggedSkillId ) || targetCanvas == null )
            {
                return;
            }

            RectTransform canvasRectTransform = ResolveDragGhostParentRectTransform();

            if ( canvasRectTransform == null )
            {
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            Vector2 localPoint;
            Camera eventCamera = ResolveDragEventCamera( canvasRectTransform );
            bool isConverted = RectTransformUtility.ScreenPointToLocalPointInRectangle( canvasRectTransform, mousePosition, eventCamera, out localPoint );

            if ( isConverted == false )
            {
                return;
            }

            dragGhostRectTransform.anchoredPosition = localPoint;
            dragGhostRectTransform.SetAsLastSibling();
        }

        ///<summary>
        /// 스킬 드래그 전용 캔버스 결정
        ///</summary>
        ///<summary>
        /// 드래그 고스트 부모 RectTransform 결정
        ///</summary>
        private RectTransform ResolveDragGhostParentRectTransform()
        {
            ResolveCanvas();

            if ( targetCanvas == null )
            {
                return null;
            }

            Transform rootTransform = targetCanvas.transform.root;
            Transform interactionCanvasTransform = rootTransform.Find( "Canvas_InteractionUI" );

            if ( interactionCanvasTransform == null )
            {
                interactionCanvasTransform = rootTransform.Find( "Canvas/Canvas_InteractionUI" );
            }

            RectTransform interactionCanvasRectTransform = interactionCanvasTransform as RectTransform;

            if ( interactionCanvasRectTransform != null )
            {
                return interactionCanvasRectTransform;
            }

            RectTransform fallbackRectTransform = targetCanvas.transform as RectTransform;
            return fallbackRectTransform;
        }

        ///<summary>
        /// 드래그 좌표 변환 카메라 결정
        ///</summary>
        private Camera ResolveDragEventCamera( RectTransform _dragGhostParentRectTransform )
        {
            if ( _dragGhostParentRectTransform == null )
            {
                return null;
            }

            Canvas parentCanvas = _dragGhostParentRectTransform.GetComponent<Canvas>();

            if ( parentCanvas == null )
            {
                parentCanvas = targetCanvas;
            }

            if ( parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay )
            {
                return null;
            }

            Camera result = parentCanvas.worldCamera;
            return result;
        }
    }
}
