using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 스킬 툴팁 UI 컴포넌트
    ///</summary>
    public sealed class CSkillTooltipUI : MonoBehaviour
    {
        private const float TooltipOffsetX = 16.0f;
        private const float TooltipOffsetY = -18.0f;

        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text currentLevelTitleText;
        [SerializeField] private TMP_Text currentLevelDescriptionText;
        [SerializeField] private TMP_Text nextLevelTitleText;
        [SerializeField] private TMP_Text nextLevelDescriptionText;

        ///<summary>
        /// 툴팁 초기 구성
        ///</summary>
        private void Awake()
        {
            ResolveReferences();
            SetTooltipContent( string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false );
            DisableRaycastTargets();
            SetVisible( false );
        }

        ///<summary>
        /// 툴팁 데이터 반영
        ///</summary>
        public void SetTooltipContent( string _titleText, string _infoText, string _currentLevelTitle, string _currentLevelDescription, string _nextLevelTitle, string _nextLevelDescription, bool _hasNextLevel )
        {
            ResolveReferences();

            if ( titleText != null )
            {
                titleText.text = string.IsNullOrWhiteSpace( _titleText ) ? string.Empty : _titleText;
            }

            if ( infoText != null )
            {
                infoText.text = string.IsNullOrWhiteSpace( _infoText ) ? string.Empty : _infoText;
                infoText.gameObject.SetActive( string.IsNullOrWhiteSpace( infoText.text ) == false );
            }

            if ( currentLevelTitleText != null )
            {
                currentLevelTitleText.text = string.IsNullOrWhiteSpace( _currentLevelTitle ) ? string.Empty : _currentLevelTitle;
                currentLevelTitleText.gameObject.SetActive( string.IsNullOrWhiteSpace( currentLevelTitleText.text ) == false );
            }

            if ( currentLevelDescriptionText != null )
            {
                currentLevelDescriptionText.text = string.IsNullOrWhiteSpace( _currentLevelDescription ) ? string.Empty : _currentLevelDescription;
                currentLevelDescriptionText.gameObject.SetActive( string.IsNullOrWhiteSpace( currentLevelDescriptionText.text ) == false );
            }

            if ( nextLevelTitleText != null )
            {
                nextLevelTitleText.text = string.IsNullOrWhiteSpace( _nextLevelTitle ) ? string.Empty : _nextLevelTitle;
                nextLevelTitleText.gameObject.SetActive( _hasNextLevel && string.IsNullOrWhiteSpace( nextLevelTitleText.text ) == false );
            }

            if ( nextLevelDescriptionText != null )
            {
                nextLevelDescriptionText.text = string.IsNullOrWhiteSpace( _nextLevelDescription ) ? string.Empty : _nextLevelDescription;
                nextLevelDescriptionText.gameObject.SetActive( _hasNextLevel && string.IsNullOrWhiteSpace( nextLevelDescriptionText.text ) == false );
            }
        }

        ///<summary>
        /// 툴팁 표시 상태 반영
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
                ClampToCanvasBounds();
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
            ClampToCanvasBounds();
        }

        ///<summary>
        /// 이벤트 카메라 결정
        ///</summary>
        private Camera ResolveEventCamera( Canvas _targetCanvas )
        {
            if ( _targetCanvas == null || _targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay )
            {
                return null;
            }

            Camera result = _targetCanvas.worldCamera;
            return result;
        }

        ///<summary>
        /// 툴팁 참조 구성
        ///</summary>
        private void ResolveReferences()
        {
            if ( rootRectTransform == null )
            {
                rootRectTransform = transform as RectTransform;
            }

            if ( titleText == null )
            {
                Transform titleTransform = transform.Find( "ItemNameText" );
                titleText = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
            }

            if ( infoText == null )
            {
                Transform infoTransform = transform.Find( "ItemDescText" );
                infoText = infoTransform != null ? infoTransform.GetComponent<TMP_Text>() : null;
            }

            if ( currentLevelTitleText == null )
            {
                Transform currentLevelTitleTransform = transform.Find( "CurrentLevelTitleText" );
                currentLevelTitleText = currentLevelTitleTransform != null ? currentLevelTitleTransform.GetComponent<TMP_Text>() : null;
            }

            if ( currentLevelDescriptionText == null )
            {
                Transform currentLevelDescriptionTransform = transform.Find( "CurrentLevelDescriptionText" );

                if ( currentLevelDescriptionTransform == null )
                {
                    currentLevelDescriptionTransform = transform.Find( "EquipmentStatText" );
                }

                currentLevelDescriptionText = currentLevelDescriptionTransform != null ? currentLevelDescriptionTransform.GetComponent<TMP_Text>() : null;
            }

            if ( nextLevelTitleText == null )
            {
                Transform nextLevelTitleTransform = transform.Find( "NextLevelTitleText" );
                nextLevelTitleText = nextLevelTitleTransform != null ? nextLevelTitleTransform.GetComponent<TMP_Text>() : null;
            }

            if ( nextLevelDescriptionText == null )
            {
                Transform nextLevelDescriptionTransform = transform.Find( "NextLevelDescriptionText" );
                nextLevelDescriptionText = nextLevelDescriptionTransform != null ? nextLevelDescriptionTransform.GetComponent<TMP_Text>() : null;
            }
        }

        ///<summary>
        /// 툴팁 화면 경계 보정
        ///</summary>
        private void ClampToCanvasBounds()
        {
            if ( rootRectTransform == null )
            {
                return;
            }

            RectTransform parentRectTransform = rootRectTransform.parent as RectTransform;

            if ( parentRectTransform == null )
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate( rootRectTransform );
            Canvas.ForceUpdateCanvases();

            Vector3[] parentWorldCornerArray = new Vector3[ 4 ];
            Vector3[] tooltipWorldCornerArray = new Vector3[ 4 ];
            parentRectTransform.GetWorldCorners( parentWorldCornerArray );
            rootRectTransform.GetWorldCorners( tooltipWorldCornerArray );

            float parentLeft = parentWorldCornerArray[ 0 ].x;
            float parentBottom = parentWorldCornerArray[ 0 ].y;
            float parentRight = parentWorldCornerArray[ 2 ].x;
            float parentTop = parentWorldCornerArray[ 2 ].y;
            float tooltipLeft = tooltipWorldCornerArray[ 0 ].x;
            float tooltipBottom = tooltipWorldCornerArray[ 0 ].y;
            float tooltipRight = tooltipWorldCornerArray[ 2 ].x;
            float tooltipTop = tooltipWorldCornerArray[ 2 ].y;
            float offsetX = 0.0f;
            float offsetY = 0.0f;

            if ( tooltipLeft < parentLeft )
            {
                offsetX = parentLeft - tooltipLeft;
            }
            else if ( tooltipRight > parentRight )
            {
                offsetX = parentRight - tooltipRight;
            }

            if ( tooltipBottom < parentBottom )
            {
                offsetY = parentBottom - tooltipBottom;
            }
            else if ( tooltipTop > parentTop )
            {
                offsetY = parentTop - tooltipTop;
            }

            if ( Mathf.Approximately( offsetX, 0.0f ) && Mathf.Approximately( offsetY, 0.0f ) )
            {
                return;
            }

            Vector3 currentPosition = rootRectTransform.position;
            Vector3 clampedPosition = currentPosition + new Vector3( offsetX, offsetY, 0.0f );
            rootRectTransform.position = clampedPosition;
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
