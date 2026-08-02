using System.Collections.Generic;
using TMPro;
using TinyHero.Core.Data;
using TinyHero.Player;
using TinyHero.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.Core
{
    public sealed class CCheatCommandUI : MonoBehaviour
    {
        private static CCheatCommandUI instance;

        [Header( "UI 참조" )]
        [SerializeField] private GameObject contentRootObject;
        [SerializeField] private TMP_InputField levelInputField;
        [SerializeField] private Toggle levelLockToggle;
        [SerializeField] private TMP_InputField itemIdInputField;
        [SerializeField] private TMP_InputField itemCountInputField;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private CButtonEx applyLevelButton;
        [SerializeField] private CButtonEx grantItemButton;
        [SerializeField] private CButtonEx grantAllItemsButton;
        [SerializeField] private CButtonEx closeButton;

        private CPlayerStatManager targetStatManager;
        private CPlayerInventoryManager targetInventoryManager;
        private bool isVisible;

        public static CCheatCommandUI GetOrCreate()
        {
            if ( instance != null ) return instance;
            CCheatCommandUI foundInstance = FindFirstObjectByType<CCheatCommandUI>( FindObjectsInactive.Include );
            if ( foundInstance != null ) { instance = foundInstance; return foundInstance; }
            CResourceManager resourceManager = CResourceManager.Instance;
            GameObject prefab = resourceManager != null ? resourceManager.GetCheatCommandPopupPrefab() : null;
            if ( prefab == null ) { Debug.LogError( "[ CheatCommandUI ] PopupCheatCommand prefab을 찾지 못했습니다." ); return null; }
            GameObject createdObject = Instantiate( prefab );
            CCheatCommandUI createdInstance = createdObject.GetComponent<CCheatCommandUI>();
            instance = createdInstance;
            return createdInstance;
        }

        public static bool IsAnyVisible()
        {
            bool result = instance != null && instance.isVisible;
            return result;
        }

        private void Awake()
        {
            if ( instance != null && instance != this ) { Destroy( gameObject ); return; }
            instance = this;
            DontDestroyOnLoad( gameObject );
            BindUiEvents();
            SetVisible( false );
        }

        private void OnDestroy()
        {
            UnbindUiEvents();
            if ( instance == this ) instance = null;
        }

        public void ToggleVisible()
        {
            SetVisible( isVisible == false );
        }

        public void SetVisible( bool _isVisible )
        {
            ResolveTargets();
            isVisible = _isVisible;
            if ( contentRootObject != null ) contentRootObject.SetActive( _isVisible );
            if ( _isVisible == false ) return;
            if ( levelLockToggle != null && targetStatManager != null ) levelLockToggle.SetIsOnWithoutNotify( targetStatManager.IsLevelLocked() );
            SetStatusMessage( "치트 창 준비 완료" );
            if ( levelInputField != null ) levelInputField.ActivateInputField();
        }

        private void HandleApplyLevelButtonClicked()
        {
            ResolveTargets();
            if ( targetStatManager == null || levelInputField == null || int.TryParse( levelInputField.text, out int parsedLevel ) == false ) { SetStatusMessage( "유효한 레벨 숫자를 입력해 주세요." ); return; }
            targetStatManager.SetCurrentLevel( parsedLevel );
            int resolvedLevel = targetStatManager.GetCurrentLevel();
            float levelStartExp = targetStatManager.GetLevelStartExp( resolvedLevel );
            targetStatManager.SetCurrentExp( levelStartExp );
            SetStatusMessage( $"레벨이 {resolvedLevel}로 변경되었습니다." );
        }

        private void HandleLevelLockToggleValueChanged( bool _isLocked )
        {
            ResolveTargets();
            if ( targetStatManager == null ) { SetStatusMessage( "플레이어 스탯 매니저를 찾지 못했습니다." ); return; }
            targetStatManager.SetLevelLocked( _isLocked );
            SetStatusMessage( _isLocked ? "레벨 고정을 활성화했습니다." : "레벨 고정을 해제했습니다." );
        }

        private void HandleGrantItemButtonClicked()
        {
            ResolveTargets();
            string itemId = itemIdInputField != null ? itemIdInputField.text.Trim() : string.Empty;
            int itemCount = itemCountInputField != null && int.TryParse( itemCountInputField.text, out int parsedCount ) ? Mathf.Max( 1, parsedCount ) : 1;
            CItemDefinition itemDefinition = null;
            bool hasItem = targetInventoryManager != null && CItemDefinitionDatabase.TryGetItemDefinition( itemId, out itemDefinition );
            if ( hasItem == false || itemDefinition == null || targetInventoryManager.TryAddItem( itemDefinition, itemCount ) == false ) { SetStatusMessage( "아이템 지급에 실패했습니다." ); return; }
            CRewardUiManager.Instance.ShowItemReward( itemDefinition, itemCount );
            SetStatusMessage( $"{itemId} x{itemCount} 지급 완료" );
        }

        private void HandleGrantAllItemsButtonClicked()
        {
            ResolveTargets();
            if ( targetInventoryManager == null ) { SetStatusMessage( "플레이어 인벤토리 매니저를 찾지 못했습니다." ); return; }
            IReadOnlyList<CItemDefinition> itemDefinitionList = CItemDefinitionDatabase.GetItemDefinitionList();
            List<CRewardItemData> rewardItemDataList = new List<CRewardItemData>();
            for ( int index = 0; index < itemDefinitionList.Count; index++ )
            {
                CItemDefinition itemDefinition = itemDefinitionList[ index ];
                if ( itemDefinition == null || string.IsNullOrWhiteSpace( itemDefinition.GetItemId() ) ) continue;
                long itemCount = itemDefinition.IsEquipmentItem() ? 1 : itemDefinition.GetMaxStackCount();
                if ( targetInventoryManager.TryAddItem( itemDefinition, itemCount ) ) rewardItemDataList.Add( new CRewardItemData( itemDefinition, itemCount ) );
            }
            CRewardUiManager.Instance.ShowItemRewardList( rewardItemDataList );
            SetStatusMessage( $"전체 아이템 지급 완료: {rewardItemDataList.Count}종" );
        }

        private void ResolveTargets()
        {
            if ( targetStatManager == null ) targetStatManager = FindFirstObjectByType<CPlayerStatManager>();
            if ( targetInventoryManager == null ) targetInventoryManager = FindFirstObjectByType<CPlayerInventoryManager>();
        }

        private void SetStatusMessage( string _message )
        {
            if ( statusText != null ) statusText.text = _message;
        }

        private void BindUiEvents()
        {
            applyLevelButton.onClick.AddListener( HandleApplyLevelButtonClicked );
            levelLockToggle.onValueChanged.AddListener( HandleLevelLockToggleValueChanged );
            grantItemButton.onClick.AddListener( HandleGrantItemButtonClicked );
            grantAllItemsButton.onClick.AddListener( HandleGrantAllItemsButtonClicked );
            closeButton.onClick.AddListener( () => SetVisible( false ) );
        }

        private void UnbindUiEvents()
        {
            if ( applyLevelButton != null ) applyLevelButton.onClick.RemoveListener( HandleApplyLevelButtonClicked );
            if ( levelLockToggle != null ) levelLockToggle.onValueChanged.RemoveListener( HandleLevelLockToggleValueChanged );
            if ( grantItemButton != null ) grantItemButton.onClick.RemoveListener( HandleGrantItemButtonClicked );
            if ( grantAllItemsButton != null ) grantAllItemsButton.onClick.RemoveAllListeners();
            if ( closeButton != null ) closeButton.onClick.RemoveAllListeners();
        }
    }
}
