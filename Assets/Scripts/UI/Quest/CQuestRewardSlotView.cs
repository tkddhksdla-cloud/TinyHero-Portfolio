using TinyHero.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 퀘스트 보상 슬롯 표시 컴포넌트
    ///</summary>
    public sealed class CQuestRewardSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text itemCountText;

        private CItemDefinition currentItemDefinition;
        private CQuestListUIController ownerQuestListUiController;

        ///<summary>
        /// 보상 슬롯 참조 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
        }

        ///<summary>
        /// 보상 슬롯 소유자 설정
        ///</summary>
        public void Configure( CQuestListUIController _ownerQuestListUiController )
        {
            ownerQuestListUiController = _ownerQuestListUiController;
            ResolveReferences();
        }

        ///<summary>
        /// 아이템 보상 정보 표시
        ///</summary>
        public void ShowItemReward( CItemDefinition _itemDefinition, int _quantity )
        {
            ResolveReferences();
            currentItemDefinition = _itemDefinition;
            int displayQuantity = Mathf.Max( 0, _quantity );
            bool hasItem = currentItemDefinition != null;

            if ( itemImage != null )
            {
                itemImage.sprite = hasItem ? currentItemDefinition.GetIconSprite() : null;
                itemImage.enabled = hasItem && itemImage.sprite != null;
                Color imageColor = itemImage.color;
                imageColor.a = hasItem ? 1.0f : 0.0f;
                itemImage.color = imageColor;
            }

            if ( itemCountText != null )
            {
                itemCountText.text = displayQuantity > 1 ? displayQuantity.ToString() : string.Empty;
            }

            gameObject.SetActive( hasItem );
        }

        ///<summary>
        /// 문자 보상 정보 표시
        ///</summary>
        ///<summary>
        /// 스프라이트 보상 정보 표시
        ///</summary>
        public void ShowSpriteReward( Sprite _iconSprite, int _quantity )
        {
            ResolveReferences();
            currentItemDefinition = null;
            int displayQuantity = Mathf.Max( 0, _quantity );
            bool hasIcon = _iconSprite != null;

            if ( itemImage != null )
            {
                itemImage.sprite = _iconSprite;
                itemImage.enabled = hasIcon;
                Color imageColor = itemImage.color;
                imageColor.a = hasIcon ? 1.0f : 0.0f;
                itemImage.color = imageColor;
            }

            if ( itemCountText != null )
            {
                itemCountText.text = displayQuantity > 0 ? displayQuantity.ToString() : string.Empty;
            }

            gameObject.SetActive( hasIcon || displayQuantity > 0 );
        }

        public void ShowTextReward( string _displayText )
        {
            ResolveReferences();
            currentItemDefinition = null;

            if ( itemImage != null )
            {
                itemImage.sprite = null;
                itemImage.enabled = false;
                Color imageColor = itemImage.color;
                imageColor.a = 0.0f;
                itemImage.color = imageColor;
            }

            if ( itemCountText != null )
            {
                itemCountText.text = string.IsNullOrWhiteSpace( _displayText ) ? string.Empty : _displayText.Trim();
            }

            gameObject.SetActive( string.IsNullOrWhiteSpace( _displayText ) == false );
        }

        ///<summary>
        /// 아이콘 포함 문자 보상 정보 표시
        ///</summary>
        public void ShowIconTextReward( Sprite _iconSprite, string _displayText )
        {
            ResolveReferences();
            currentItemDefinition = null;
            bool hasIcon = _iconSprite != null;

            if ( itemImage != null )
            {
                itemImage.sprite = _iconSprite;
                itemImage.enabled = hasIcon;
                Color imageColor = itemImage.color;
                imageColor.a = hasIcon ? 1.0f : 0.0f;
                itemImage.color = imageColor;
            }

            if ( itemCountText != null )
            {
                itemCountText.text = string.IsNullOrWhiteSpace( _displayText ) ? string.Empty : _displayText.Trim();
            }

            bool isVisible = hasIcon || string.IsNullOrWhiteSpace( _displayText ) == false;
            gameObject.SetActive( isVisible );
        }

        ///<summary>
        /// 보상 슬롯 비활성화
        ///</summary>
        public void Hide()
        {
            currentItemDefinition = null;

            if ( itemImage != null )
            {
                itemImage.sprite = null;
                itemImage.enabled = false;
            }

            if ( itemCountText != null )
            {
                itemCountText.text = string.Empty;
            }

            gameObject.SetActive( false );
        }

        ///<summary>
        /// 현재 아이템 보상 데이터 반환
        ///</summary>
        public CItemDefinition GetCurrentItemDefinition()
        {
            CItemDefinition result = currentItemDefinition;
            return result;
        }

        ///<summary>
        /// 보상 슬롯 마우스 진입 처리
        ///</summary>
        public void OnPointerEnter( PointerEventData _eventData )
        {
            if ( ownerQuestListUiController == null || currentItemDefinition == null )
            {
                return;
            }

            ownerQuestListUiController.ShowRewardTooltip( currentItemDefinition );
        }

        ///<summary>
        /// 보상 슬롯 마우스 이탈 처리
        ///</summary>
        public void OnPointerExit( PointerEventData _eventData )
        {
            if ( ownerQuestListUiController == null )
            {
                return;
            }

            ownerQuestListUiController.HideRewardTooltip();
        }

        ///<summary>
        /// 보상 슬롯 하위 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( itemImage == null )
            {
                Transform itemImageTransform = transform.Find( "ItemImage" );
                itemImage = itemImageTransform != null ? itemImageTransform.GetComponent<Image>() : null;
            }

            if ( itemCountText == null )
            {
                Transform itemCountTransform = transform.Find( "ItemCount" );
                itemCountText = itemCountTransform != null ? itemCountTransform.GetComponent<TMP_Text>() : null;
            }
        }
    }
}
