using TinyHero.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 인벤토리 아이템 툴팁 UI
    ///</summary>
    public sealed class CItemTooltipUI : MonoBehaviour
    {
        private const float TooltipOffsetX = 16.0f;
        private const float TooltipOffsetY = -18.0f;

        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescText;

        ///<summary>
        /// 툴팁 참조 초기화
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            SetTooltipContent( string.Empty, string.Empty );
            DisableRaycastTargets();
            SetVisible( false );
        }

        ///<summary>
        /// 툴팁 내용 반영
        ///</summary>
        public void SetTooltipContent( CItemDefinition _itemDefinition )
        {
            ResolveReferences();
            string itemName = string.Empty;
            string itemDescription = string.Empty;

            if ( _itemDefinition != null )
            {
                itemName = _itemDefinition.GetItemName();
                itemDescription = _itemDefinition.GetDescription();
            }

            if ( itemNameText != null )
            {
                itemNameText.text = itemName;
            }

            if ( itemDescText != null )
            {
                itemDescText.text = itemDescription;
            }
        }

        ///<summary>
        /// 툴팁 문자열 내용 반영
        ///</summary>
        public void SetTooltipContent( string _titleText, string _descriptionText )
        {
            ResolveReferences();
            string resolvedTitleText = string.IsNullOrWhiteSpace( _titleText ) ? string.Empty : _titleText;
            string resolvedDescriptionText = string.IsNullOrWhiteSpace( _descriptionText ) ? string.Empty : _descriptionText;

            if ( itemNameText != null )
            {
                itemNameText.text = resolvedTitleText;
            }

            if ( itemDescText != null )
            {
                itemDescText.text = resolvedDescriptionText;
            }
        }

        ///<summary>
        /// 툴팁 표시 상태 설정
        ///</summary>
        public void SetVisible( bool _isVisible )
        {
            gameObject.SetActive( _isVisible );

            if ( _isVisible == false || rootRectTransform == null )
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate( rootRectTransform );
        }

        ///<summary>
        /// 툴팁 위치 갱신
        ///</summary>
        public void SetScreenPosition( Vector2 _screenPosition, Canvas _targetCanvas )
        {
            ResolveReferences();

            if ( rootRectTransform == null || _targetCanvas == null )
            {
                return;
            }

            Vector2 tooltipScreenPosition = _screenPosition + new Vector2( TooltipOffsetX, TooltipOffsetY );
            Camera eventCamera = ResolveEventCamera( _targetCanvas );

            if ( _targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay )
            {
                rootRectTransform.position = tooltipScreenPosition;
                return;
            }

            RectTransform parentRectTransform = rootRectTransform.parent as RectTransform;

            if ( parentRectTransform == null )
            {
                return;
            }

            Vector3 worldPoint;
            bool isConverted = RectTransformUtility.ScreenPointToWorldPointInRectangle( parentRectTransform, tooltipScreenPosition, eventCamera, out worldPoint );

            if ( isConverted == false )
            {
                return;
            }

            rootRectTransform.position = worldPoint;
        }

        ///<summary>
        /// 툴팁 이벤트 카메라 결정
        ///</summary>
        private Camera ResolveEventCamera( Canvas _targetCanvas )
        {
            if ( _targetCanvas == null )
            {
                return null;
            }

            if ( _targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay )
            {
                return null;
            }

            Camera result = _targetCanvas.worldCamera;
            return result;
        }

        ///<summary>
        /// 툴팁 하위 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( rootRectTransform == null )
            {
                rootRectTransform = transform as RectTransform;
            }

            if ( itemNameText == null )
            {
                Transform itemNameTransform = transform.Find( "ItemNameText" );
                itemNameText = itemNameTransform != null ? itemNameTransform.GetComponent<TMP_Text>() : null;
            }

            if ( itemDescText == null )
            {
                Transform itemDescTransform = transform.Find( "ItemDescText" );
                itemDescText = itemDescTransform != null ? itemDescTransform.GetComponent<TMP_Text>() : null;
            }
        }

        ///<summary>
        /// 툴팁 레이캐스트 비활성화
        ///</summary>
        private void DisableRaycastTargets()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>( true );

            for ( int index = 0; index < graphics.Length; index++ )
            {
                Graphic graphic = graphics[ index ];

                if ( graphic == null )
                {
                    continue;
                }

                graphic.raycastTarget = false;
            }
        }
    }
}
