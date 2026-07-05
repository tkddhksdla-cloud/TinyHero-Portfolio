using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TinyHero.Core;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.Quest;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TinyHero.UI
{
    ///<summary>
    /// NPC 상호작용 진행 매니저
    ///</summary>
    public sealed class CNPCInteractionManager : CSingleTon<CNPCInteractionManager>
    {
        private const string GameplaySceneName = "SceneMap";
        private const string CanvasObjectName = "Canvas_NPCInteraction";
        private const string DialogueObjectPath = "Dialogue";
        private const string NameObjectPath = "Dialogue/Name";
        private const string DialogueTextObjectPath = "Dialogue/DialogueAreaBG/DialogueText";
        private const float DefaultCharacterRevealInterval = 0.03f;

        [SerializeField] private Canvas npcInteractionCanvas;
        [SerializeField] private GameObject dialogueObject;
        [SerializeField] private TMP_Text npcNameText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private float characterRevealInterval = DefaultCharacterRevealInterval;

        private readonly List<CNPCInteractionRange> activeInteractionRangeList = new List<CNPCInteractionRange>();
        private CNPCObject activeNpcObject;
        private CNPCInteractionData activeInteractionData;
        private CNPCInteractionActionEntry activeActionEntry;
        private CNPCDialoguePreset activeDialoguePreset;
        private Action<bool> pendingQuestInteractionCompleted;
        private PlayerController pendingQuestPlayerController;
        private Coroutine revealDialogueRoutine;
        private string currentFullDialogueLine = string.Empty;
        private string pendingQuestInteractionQuestId = string.Empty;
        private int currentActionEntryIndex = -1;
        private int currentDialogueLineIndex = -1;
        private bool isDialogueLineFullyRevealed;
        private bool isPendingQuestInteractionExecution;
        private bool hasShownPendingQuestInteractionDialogue;
        private bool pendingQuestInteractionShouldProcessQuest = true;

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ResolveSceneReferences();
            SetDialogueVisible( false );
        }

        ///<summary>
        /// 인스턴스 조회 시도
        ///</summary>
        public static bool TryGetInstance( out CNPCInteractionManager _instance )
        {
            CNPCInteractionManager resolvedInstance = Instance;
            _instance = resolvedInstance;
            bool hasInstance = _instance != null;
            return hasInstance;
        }

        ///<summary>
        /// 플레이어 제어 차단 여부 반환
        ///</summary>
        public bool IsInteractionInProgress()
        {
            bool result = activeNpcObject != null;
            return result;
        }

        ///<summary>
        /// 상호작용 범위 등록
        ///</summary>
        public void RegisterInteractionRange( CNPCInteractionRange _interactionRange )
        {
            if ( _interactionRange == null )
            {
                return;
            }

            if ( activeInteractionRangeList.Contains( _interactionRange ) )
            {
                return;
            }

            activeInteractionRangeList.Add( _interactionRange );
        }

        ///<summary>
        /// 상호작용 범위 해제
        ///</summary>
        public void UnregisterInteractionRange( CNPCInteractionRange _interactionRange )
        {
            if ( _interactionRange == null )
            {
                return;
            }

            activeInteractionRangeList.Remove( _interactionRange );

            if ( activeNpcObject == null )
            {
                return;
            }

            CNPCObject rangeNpcObject = _interactionRange.GetOwnerNpcObject();

            if ( rangeNpcObject != activeNpcObject )
            {
                return;
            }

            bool isNpcUnavailable = rangeNpcObject == null || rangeNpcObject.gameObject.activeInHierarchy == false;

            if ( isNpcUnavailable )
            {
                EndInteraction();
            }
        }

        ///<summary>
        /// 상호작용 입력 처리
        ///</summary>
        public bool TryProcessInteractionInput( bool _isInteractionDown )
        {
            if ( _isInteractionDown == false )
            {
                return false;
            }

            ResolveSceneReferences();

            if ( activeNpcObject != null )
            {
                HandleAdvanceInput();
                return true;
            }

            if ( IsBlockedByVisibleUi() )
            {
                return true;
            }

            CNPCObject nearestNpcObject = ResolveNearestInteractableNpcObject();

            if ( nearestNpcObject == null )
            {
                return false;
            }

            BeginInteraction( nearestNpcObject );
            return true;
        }

        ///<summary>
        /// 퀘스트 UI 전용 상호작용 처리 시작
        ///</summary>
        public void ProcessQuestUiInteraction( CNPCObject _npcObject, PlayerController _playerController, string _questId, Action<bool> _completedHandler )
        {
            if ( _npcObject == null || _playerController == null || string.IsNullOrWhiteSpace( _questId ) )
            {
                if ( _completedHandler != null )
                {
                    _completedHandler( false );
                }

                return;
            }

            EndInteraction();
            activeNpcObject = _npcObject;
            pendingQuestInteractionCompleted = _completedHandler;
            pendingQuestPlayerController = _playerController;
            pendingQuestInteractionQuestId = _questId.Trim();
            isPendingQuestInteractionExecution = true;
            hasShownPendingQuestInteractionDialogue = false;
            pendingQuestInteractionShouldProcessQuest = true;
            ExecutePendingQuestInteraction();
        }

        ///<summary>
        /// 퀘스트 선택 대화 출력 처리 시작
        ///</summary>
        public void ShowQuestSelectionDialogue( CNPCObject _npcObject, PlayerController _playerController, string _questId, Action<bool> _completedHandler )
        {
            if ( _npcObject == null || _playerController == null || string.IsNullOrWhiteSpace( _questId ) )
            {
                if ( _completedHandler != null )
                {
                    _completedHandler( false );
                }

                return;
            }

            EndInteraction();
            activeNpcObject = _npcObject;
            pendingQuestInteractionCompleted = _completedHandler;
            pendingQuestPlayerController = _playerController;
            pendingQuestInteractionQuestId = _questId.Trim();
            isPendingQuestInteractionExecution = true;
            hasShownPendingQuestInteractionDialogue = false;
            pendingQuestInteractionShouldProcessQuest = false;
            ExecutePendingQuestInteraction();
        }

        ///<summary>
        /// 씬 로드 후 상호작용 상태 초기화
        ///</summary>
        private void HandleSceneLoaded( Scene _scene, LoadSceneMode _loadSceneMode )
        {
            ResolveSceneReferences();
            EndInteraction();
        }

        ///<summary>
        /// 표시 중인 UI에 의한 NPC 상호작용 차단 여부 반환
        ///</summary>
        private bool IsBlockedByVisibleUi()
        {
            if ( PopupQuestList.IsAnyUiBlockingNpcInteraction() )
            {
                return true;
            }

            CShopUiManager shopUiManager = CShopUiManager.Instance;
            bool isShopVisible = shopUiManager != null && shopUiManager.IsShopVisible();

            if ( isShopVisible )
            {
                return true;
            }

            return false;
        }

        ///<summary>
        /// 씬 참조 결정
        ///</summary>
        private void ResolveSceneReferences()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            if ( activeScene.name != GameplaySceneName )
            {
                npcInteractionCanvas = null;
                dialogueObject = null;
                npcNameText = null;
                dialogueText = null;
                return;
            }

            if ( npcInteractionCanvas == null )
            {
                GameObject canvasObject = GameObject.Find( CanvasObjectName );

                if ( canvasObject != null )
                {
                    Canvas resolvedCanvas = canvasObject.GetComponent<Canvas>();
                    npcInteractionCanvas = resolvedCanvas;
                }
            }

            if ( npcInteractionCanvas == null )
            {
                return;
            }

            Transform canvasTransform = npcInteractionCanvas.transform;

            if ( dialogueObject == null )
            {
                Transform dialogueTransform = canvasTransform.Find( DialogueObjectPath );

                if ( dialogueTransform != null )
                {
                    GameObject resolvedDialogueObject = dialogueTransform.gameObject;
                    dialogueObject = resolvedDialogueObject;
                }
            }

            if ( npcNameText == null )
            {
                Transform npcNameTransform = canvasTransform.Find( NameObjectPath );

                if ( npcNameTransform != null )
                {
                    TMP_Text resolvedNpcNameText = npcNameTransform.GetComponent<TMP_Text>();
                    npcNameText = resolvedNpcNameText;
                }
            }

            if ( dialogueText == null )
            {
                Transform dialogueTextTransform = canvasTransform.Find( DialogueTextObjectPath );

                if ( dialogueTextTransform != null )
                {
                    TMP_Text resolvedDialogueText = dialogueTextTransform.GetComponent<TMP_Text>();
                    dialogueText = resolvedDialogueText;
                }
            }
        }

        ///<summary>
        /// 상호작용 시작
        ///</summary>
        private void BeginInteraction( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return;
            }

            CNPCInteractionData interactionData = _npcObject.GetInteractionData();

            if ( interactionData == null )
            {
                Debug.LogWarning( $"NPC interaction data was not assigned on '{_npcObject.name}'.", _npcObject );
                return;
            }

            List<CNPCInteractionActionEntry> actionEntryList = interactionData.GetActionEntryList();

            if ( actionEntryList == null || actionEntryList.Count == 0 )
            {
                Debug.LogWarning( $"NPC interaction action entry was empty on '{_npcObject.name}'.", _npcObject );
                return;
            }

            activeNpcObject = _npcObject;
            activeInteractionData = interactionData;
            currentActionEntryIndex = 0;
            FaceActiveNpcTowardPlayer();
            ExecuteCurrentActionEntry();
        }

        ///<summary>
        /// 진행 중 엔트리 실행
        ///</summary>
        private void ExecuteCurrentActionEntry()
        {
            if ( activeInteractionData == null )
            {
                EndInteraction();
                return;
            }

            List<CNPCInteractionActionEntry> actionEntryList = activeInteractionData.GetActionEntryList();

            if ( actionEntryList == null || currentActionEntryIndex < 0 || currentActionEntryIndex >= actionEntryList.Count )
            {
                EndInteraction();
                return;
            }

            CNPCInteractionActionEntry currentActionEntry = actionEntryList[ currentActionEntryIndex ];
            activeActionEntry = currentActionEntry;

            if ( activeActionEntry == null )
            {
                AdvanceToNextActionEntry();
                return;
            }

            bool useDialogue = activeActionEntry.GetUseDialogue();
            List<CNPCDialoguePreset> dialoguePresetList = activeActionEntry.GetDialoguePresetList();

            if ( useDialogue && dialoguePresetList != null && dialoguePresetList.Count > 0 )
            {
                BeginDialogueSequence();
                return;
            }

            ExecuteActionAfterDialogue();
        }

        ///<summary>
        /// 대화 시퀀스 시작
        ///</summary>
        private void BeginDialogueSequence()
        {
            if ( activeNpcObject == null || activeActionEntry == null )
            {
                EndInteraction();
                return;
            }

            List<CNPCDialoguePreset> dialoguePresetList = activeActionEntry.GetDialoguePresetList();

            if ( dialoguePresetList == null || dialoguePresetList.Count == 0 )
            {
                ExecuteActionAfterDialogue();
                return;
            }

            int presetIndex = activeNpcObject.ResolveNextDialoguePresetIndex( currentActionEntryIndex, dialoguePresetList.Count );

            if ( presetIndex < 0 || presetIndex >= dialoguePresetList.Count )
            {
                ExecuteActionAfterDialogue();
                return;
            }

            CNPCDialoguePreset dialoguePreset = dialoguePresetList[ presetIndex ];
            activeDialoguePreset = dialoguePreset;

            if ( npcNameText != null )
            {
                npcNameText.text = activeNpcObject.GetDisplayName();
            }

            currentDialogueLineIndex = 0;
            SetDialogueVisible( true );
            ShowCurrentDialogueLine();
        }

        ///<summary>
        /// 지정 대화 프리셋 시퀀스 시작
        ///</summary>
        private void BeginDialoguePresetSequence( CNPCDialoguePreset _dialoguePreset )
        {
            if ( _dialoguePreset == null || activeNpcObject == null )
            {
                ExecuteActionAfterDialogue();
                return;
            }

            activeDialoguePreset = _dialoguePreset;

            if ( npcNameText != null )
            {
                npcNameText.text = activeNpcObject.GetDisplayName();
            }

            currentDialogueLineIndex = 0;
            SetDialogueVisible( true );
            ShowCurrentDialogueLine();
        }

        ///<summary>
        /// 현재 대화 라인 표시
        ///</summary>
        private void ShowCurrentDialogueLine()
        {
            if ( activeDialoguePreset == null )
            {
                ExecuteActionAfterDialogue();
                return;
            }

            List<string> dialogueLineList = activeDialoguePreset.GetDialogueLineList();

            if ( dialogueLineList == null || currentDialogueLineIndex < 0 || currentDialogueLineIndex >= dialogueLineList.Count )
            {
                FinishDialogueSequence();
                return;
            }

            string rawDialogueLine = activeDialoguePreset.GetDialogueText( currentDialogueLineIndex );
            string resolvedDialogueLine = string.IsNullOrWhiteSpace( rawDialogueLine ) ? string.Empty : rawDialogueLine;
            currentFullDialogueLine = resolvedDialogueLine;
            isDialogueLineFullyRevealed = false;
            StartRevealDialogueRoutine( currentFullDialogueLine );
        }

        ///<summary>
        /// 대화 진행 입력 처리
        ///</summary>
        private void HandleAdvanceInput()
        {
            if ( isDialogueLineFullyRevealed == false )
            {
                CompleteCurrentDialogueLine();
                return;
            }

            if ( activeDialoguePreset == null )
            {
                ExecuteActionAfterDialogue();
                return;
            }

            currentDialogueLineIndex++;
            List<string> dialogueLineList = activeDialoguePreset.GetDialogueLineList();

            if ( dialogueLineList != null && currentDialogueLineIndex < dialogueLineList.Count )
            {
                ShowCurrentDialogueLine();
                return;
            }

            FinishDialogueSequence();
        }

        ///<summary>
        /// 대화 시퀀스 종료
        ///</summary>
        private void FinishDialogueSequence()
        {
            StopRevealDialogueRoutine();
            currentDialogueLineIndex = -1;
            activeDialoguePreset = null;
            SetDialogueVisible( false );
            ExecuteActionAfterDialogue();
        }

        ///<summary>
        /// 대화 이후 액션 실행
        ///</summary>
        private void ExecuteActionAfterDialogue()
        {
            if ( isPendingQuestInteractionExecution )
            {
                ExecutePendingQuestInteraction();
                return;
            }

            if ( activeActionEntry == null )
            {
                AdvanceToNextActionEntry();
                return;
            }

            eNPCInteractionAction actionType = activeActionEntry.GetActionType();

            switch ( actionType )
            {
                case eNPCInteractionAction.DIALOGUE:
                    AdvanceToNextActionEntry();
                    break;

                case eNPCInteractionAction.QUEST:
                    HandleQuestAction();
                    break;

                case eNPCInteractionAction.SHOP:
                    HandleShopAction();
                    break;

                default:
                    EndInteraction();
                    break;
            }
        }

        ///<summary>
        /// 퀘스트 액션 자리 처리
        ///</summary>
        private void HandleQuestAction()
        {
            if ( activeNpcObject == null )
            {
                EndInteraction();
                return;
            }

            PlayerController playerController = ResolvePlayerControllerForNpc( activeNpcObject );
            CQuestUiManager questUiManager = CQuestUiManager.Instance;

            if ( playerController != null && questUiManager != null )
            {
                questUiManager.ShowNpcQuestListUi( activeNpcObject, playerController );
            }

            EndInteraction();
        }

        ///<summary>
        /// 상점 액션 자리 처리
        ///</summary>
        private void HandleShopAction()
        {
            if ( activeNpcObject == null || activeActionEntry == null )
            {
                EndInteraction();
                return;
            }

            PlayerController playerController = ResolvePlayerControllerForNpc( activeNpcObject );
            CPlayerInventoryManager inventoryManager = playerController != null ? playerController.GetInventoryManager() : null;
            CShopUiManager shopUiManager = CShopUiManager.Instance;

            if ( inventoryManager == null || shopUiManager == null )
            {
                EndInteraction();
                return;
            }

            string shopId = activeActionEntry.GetLinkedShopId();
            shopUiManager.OpenShop( shopId, activeNpcObject.GetDisplayName(), inventoryManager );

            EndInteraction();
        }

        ///<summary>
        /// 다음 엔트리 진행
        ///</summary>
        private void AdvanceToNextActionEntry()
        {
            currentActionEntryIndex++;
            activeActionEntry = null;
            ExecuteCurrentActionEntry();
        }

        ///<summary>
        /// 상호작용 종료
        ///</summary>
        private void EndInteraction()
        {
            StopRevealDialogueRoutine();
            activeNpcObject = null;
            activeInteractionData = null;
            activeActionEntry = null;
            activeDialoguePreset = null;
            pendingQuestInteractionCompleted = null;
            pendingQuestPlayerController = null;
            currentActionEntryIndex = -1;
            currentDialogueLineIndex = -1;
            currentFullDialogueLine = string.Empty;
            pendingQuestInteractionQuestId = string.Empty;
            isDialogueLineFullyRevealed = true;
            isPendingQuestInteractionExecution = false;
            hasShownPendingQuestInteractionDialogue = false;
            pendingQuestInteractionShouldProcessQuest = true;

            if ( dialogueText != null )
            {
                dialogueText.text = string.Empty;
            }

            SetDialogueVisible( false );
        }

        ///<summary>
        /// 퀘스트 UI 전용 상호작용 실행
        ///</summary>
        private void ExecutePendingQuestInteraction()
        {
            if ( isPendingQuestInteractionExecution == false || activeNpcObject == null || pendingQuestPlayerController == null || string.IsNullOrWhiteSpace( pendingQuestInteractionQuestId ) )
            {
                NotifyPendingQuestInteractionCompleted( false );
                EndInteraction();
                return;
            }

            CQuestManager questManager = pendingQuestPlayerController.GetQuestManager();

            if ( questManager == null )
            {
                NotifyPendingQuestInteractionCompleted( false );
                EndInteraction();
                return;
            }

            if ( hasShownPendingQuestInteractionDialogue == false )
            {
                bool hasQuestDialogue = questManager.TryGetQuestDialoguePreset( activeNpcObject, pendingQuestInteractionQuestId, out CNPCDialoguePreset dialoguePreset, out eQuestNpcInteractionType interactionType );
                hasShownPendingQuestInteractionDialogue = true;

                if ( hasQuestDialogue )
                {
                    BeginDialoguePresetSequence( dialoguePreset );
                    return;
                }
            }

            if ( pendingQuestInteractionShouldProcessQuest == false )
            {
                NotifyPendingQuestInteractionCompleted( true );
                EndInteraction();
                return;
            }

            bool processResult = questManager.ProcessNpcQuestInteraction( activeNpcObject, pendingQuestInteractionQuestId );
            NotifyPendingQuestInteractionCompleted( processResult );
            EndInteraction();
        }

        /// 퀘스트 UI 상호작용 완료 콜백 호출
        ///</summary>
        private void NotifyPendingQuestInteractionCompleted( bool _result )
        {
            Action<bool> completedHandler = pendingQuestInteractionCompleted;

            if ( completedHandler == null )
            {
                return;
            }

            completedHandler( _result );
        }

        ///<summary>
        /// 상호작용 가능한 최근접 NPC 결정
        ///</summary>
        private CNPCObject ResolveNearestInteractableNpcObject()
        {
            CleanupInvalidInteractionRanges();
            CNPCInteractionRange nearestRange = null;
            float nearestDistance = float.MaxValue;

            for ( int index = 0; index < activeInteractionRangeList.Count; index++ )
            {
                CNPCInteractionRange interactionRange = activeInteractionRangeList[ index ];

                if ( interactionRange == null || interactionRange.IsPlayerInRange() == false )
                {
                    continue;
                }

                PlayerController playerController = interactionRange.GetCurrentPlayerController();
                CNPCObject npcObject = interactionRange.GetOwnerNpcObject();

                if ( playerController == null || npcObject == null )
                {
                    continue;
                }

                Vector3 playerPosition = playerController.transform.position;
                Vector3 npcPosition = npcObject.transform.position;
                float currentDistance = Vector3.SqrMagnitude( playerPosition - npcPosition );

                if ( currentDistance >= nearestDistance )
                {
                    continue;
                }

                nearestDistance = currentDistance;
                nearestRange = interactionRange;
            }

            if ( nearestRange == null )
            {
                return null;
            }

            CNPCObject result = nearestRange.GetOwnerNpcObject();
            return result;
        }

        ///<summary>
        /// 대화 표시 상태 설정
        ///</summary>
        private void SetDialogueVisible( bool _isVisible )
        {
            if ( dialogueObject == null )
            {
                return;
            }

            if ( dialogueObject.activeSelf == _isVisible )
            {
                return;
            }

            dialogueObject.SetActive( _isVisible );

            if ( _isVisible )
            {
                BringDialogueToFront();
            }
        }

        ///<summary>
        /// 대화 타이핑 연출 시작
        ///</summary>
        private void StartRevealDialogueRoutine( string _dialogueLine )
        {
            StopRevealDialogueRoutine();

            if ( dialogueText == null )
            {
                return;
            }

            dialogueText.text = string.Empty;
            revealDialogueRoutine = StartCoroutine( IE_RevealDialogueText( _dialogueLine ) );
        }

        ///<summary>
        /// 대화 타이핑 연출 중단
        ///</summary>
        private void StopRevealDialogueRoutine()
        {
            if ( revealDialogueRoutine == null )
            {
                return;
            }

            StopCoroutine( revealDialogueRoutine );
            revealDialogueRoutine = null;
        }

        ///<summary>
        /// 현재 대화 라인 즉시 완성
        ///</summary>
        private void CompleteCurrentDialogueLine()
        {
            StopRevealDialogueRoutine();

            if ( dialogueText != null )
            {
                dialogueText.text = currentFullDialogueLine;
            }

            isDialogueLineFullyRevealed = true;
        }

        ///<summary>
        /// 대화 텍스트 타이핑 연출 코루틴
        ///</summary>
        private IEnumerator IE_RevealDialogueText( string _dialogueLine )
        {
            if ( dialogueText == null )
            {
                yield break;
            }

            string resolvedDialogueLine = _dialogueLine ?? string.Empty;
            float revealInterval = Mathf.Max( 0.001f, characterRevealInterval );

            for ( int index = 0; index < resolvedDialogueLine.Length; index++ )
            {
                string currentText = resolvedDialogueLine.Substring( 0, index + 1 );
                dialogueText.text = currentText;
                yield return new WaitForSeconds( revealInterval );
            }

            dialogueText.text = resolvedDialogueLine;
            revealDialogueRoutine = null;
            isDialogueLineFullyRevealed = true;
        }

        ///<summary>
        /// 무효 범위 목록 정리
        ///</summary>
        private void CleanupInvalidInteractionRanges()
        {
            for ( int index = activeInteractionRangeList.Count - 1; index >= 0; index-- )
            {
                CNPCInteractionRange interactionRange = activeInteractionRangeList[ index ];

                if ( interactionRange != null )
                {
                    continue;
                }

                activeInteractionRangeList.RemoveAt( index );
            }
        }

        ///<summary>
        /// 인스턴스 참조 정리
        ///</summary>
        ///<summary>
        /// 상호작용 NPC의 플레이어 방향 적용
        ///</summary>
        private void FaceActiveNpcTowardPlayer()
        {
            if ( activeNpcObject == null )
            {
                return;
            }

            PlayerController playerController = ResolvePlayerControllerForNpc( activeNpcObject );

            if ( playerController == null )
            {
                return;
            }

            activeNpcObject.FaceTarget( playerController.transform );
        }

        ///<summary>
        /// NPC 기준 플레이어 제어 결정
        ///</summary>
        private PlayerController ResolvePlayerControllerForNpc( CNPCObject _npcObject )
        {
            if ( _npcObject == null )
            {
                return null;
            }

            PlayerController nearestPlayerController = null;
            float nearestDistance = float.MaxValue;

            for ( int index = 0; index < activeInteractionRangeList.Count; index++ )
            {
                CNPCInteractionRange interactionRange = activeInteractionRangeList[ index ];

                if ( interactionRange == null || interactionRange.IsPlayerInRange() == false )
                {
                    continue;
                }

                CNPCObject ownerNpcObject = interactionRange.GetOwnerNpcObject();

                if ( ownerNpcObject != _npcObject )
                {
                    continue;
                }

                PlayerController playerController = interactionRange.GetCurrentPlayerController();

                if ( playerController == null )
                {
                    continue;
                }

                Vector3 playerPosition = playerController.transform.position;
                Vector3 npcPosition = _npcObject.transform.position;
                float currentDistance = Vector3.SqrMagnitude( playerPosition - npcPosition );

                if ( currentDistance >= nearestDistance )
                {
                    continue;
                }

                nearestDistance = currentDistance;
                nearestPlayerController = playerController;
            }

            return nearestPlayerController;
        }

        ///<summary>
        /// NPC 대화창 최상단 정렬
        ///</summary>
        private void BringDialogueToFront()
        {
            RectTransform topLevelWindowRectTransform = ResolveTopLevelDialogueRectTransform();

            if ( topLevelWindowRectTransform == null )
            {
                return;
            }

            topLevelWindowRectTransform.SetAsLastSibling();
        }

        ///<summary>
        /// 대화창 최상위 RectTransform 결정
        ///</summary>
        private RectTransform ResolveTopLevelDialogueRectTransform()
        {
            RectTransform dialogueRectTransform = dialogueObject != null ? dialogueObject.transform as RectTransform : null;

            if ( dialogueRectTransform == null )
            {
                return null;
            }

            RectTransform canvasRectTransform = npcInteractionCanvas != null ? npcInteractionCanvas.transform as RectTransform : null;
            RectTransform currentRectTransform = dialogueRectTransform;

            while ( currentRectTransform != null )
            {
                RectTransform parentRectTransform = currentRectTransform.parent as RectTransform;

                if ( parentRectTransform == null )
                {
                    break;
                }

                if ( parentRectTransform == canvasRectTransform )
                {
                    break;
                }

                Transform grandParentTransform = parentRectTransform.parent;

                if ( grandParentTransform == canvasRectTransform )
                {
                    break;
                }

                currentRectTransform = parentRectTransform;
            }

            RectTransform result = currentRectTransform;
            return result;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDestroy();
        }
    }
}
