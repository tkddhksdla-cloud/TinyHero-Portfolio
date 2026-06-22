using System.Text;
using TinyHero.Core.Data;
using TinyHero.Player;
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
        private static readonly ePlayerStatType[] TooltipStatTypeArray =
        {
            ePlayerStatType.HP,
            ePlayerStatType.HR,
            ePlayerStatType.MP,
            ePlayerStatType.MR,
            ePlayerStatType.ATK,
            ePlayerStatType.DEF,
            ePlayerStatType.CRT,
            ePlayerStatType.CRD,
            ePlayerStatType.ACC,
            ePlayerStatType.ATS,
            ePlayerStatType.MOVE
        };

        [SerializeField] private RectTransform rootRectTransform;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescText;
        [SerializeField] private TMP_Text equipmentStatText;

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
        /// 아이템 툴팁 내용 반영
        ///</summary>
        public void SetTooltipContent( CItemDefinition _itemDefinition )
        {
            ResolveReferences();
            string itemName = string.Empty;
            string itemDescription = string.Empty;
            string resolvedEquipmentStatText = string.Empty;

            if ( _itemDefinition != null )
            {
                itemName = _itemDefinition.GetItemName();
                itemDescription = _itemDefinition.GetDescription();
                resolvedEquipmentStatText = BuildEquipmentStatText( _itemDefinition );
            }

            if ( itemNameText != null )
            {
                itemNameText.text = itemName;
            }

            if ( itemDescText != null )
            {
                itemDescText.text = itemDescription;
            }

            ApplyEquipmentStatText( resolvedEquipmentStatText );
        }

        ///<summary>
        /// 문자 툴팁 내용 반영
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

            ApplyEquipmentStatText( string.Empty );
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
        /// 장비 스탯 표시 텍스트 반영
        ///</summary>
        private void ApplyEquipmentStatText( string _equipmentStatText )
        {
            if ( equipmentStatText == null )
            {
                return;
            }

            string resolvedEquipmentStatText = string.IsNullOrWhiteSpace( _equipmentStatText ) ? string.Empty : _equipmentStatText;
            equipmentStatText.text = resolvedEquipmentStatText;
            equipmentStatText.gameObject.SetActive( string.IsNullOrWhiteSpace( resolvedEquipmentStatText ) == false );
        }

        ///<summary>
        /// 장비 스탯 문자열 구성
        ///</summary>
        private string BuildEquipmentStatText( CItemDefinition _itemDefinition )
        {
            if ( _itemDefinition == null || _itemDefinition.IsEquipmentItem() == false )
            {
                return string.Empty;
            }

            CPlayerStatRuntimeData equipmentStatBonus = _itemDefinition.GetEquipmentStatBonus();

            if ( equipmentStatBonus == null )
            {
                return string.Empty;
            }

            StringBuilder statTextBuilder = new StringBuilder();

            for ( int index = 0; index < TooltipStatTypeArray.Length; index++ )
            {
                ePlayerStatType statType = TooltipStatTypeArray[ index ];
                float statValue = equipmentStatBonus.GetStatValue( statType );

                if ( Mathf.Approximately( statValue, 0.0f ) )
                {
                    continue;
                }

                string statLine = BuildEquipmentStatLine( statType, statValue );

                if ( statTextBuilder.Length > 0 )
                {
                    statTextBuilder.AppendLine();
                }

                statTextBuilder.Append( statLine );
            }

            string result = statTextBuilder.ToString();
            return result;
        }

        ///<summary>
        /// 장비 스탯 한 줄 문자열 구성
        ///</summary>
        private string BuildEquipmentStatLine( ePlayerStatType _statType, float _statValue )
        {
            string statLabel = ResolveEquipmentStatLabel( _statType );
            string statValueText = FormatEquipmentStatValue( _statType, _statValue );
            string result = $"{statLabel} <b>{statValueText}</b>";
            return result;
        }

        ///<summary>
        /// 장비 스탯 라벨 결정
        ///</summary>
        private string ResolveEquipmentStatLabel( ePlayerStatType _statType )
        {
            switch ( _statType )
            {
                case ePlayerStatType.HP:
                    return "HP";

                case ePlayerStatType.HR:
                    return "HR";

                case ePlayerStatType.MP:
                    return "MP";

                case ePlayerStatType.MR:
                    return "MR";

                case ePlayerStatType.ATK:
                    return "ATK";

                case ePlayerStatType.DEF:
                    return "DEF";

                case ePlayerStatType.CRT:
                    return "CRT";

                case ePlayerStatType.CRD:
                    return "CRD";

                case ePlayerStatType.ACC:
                    return "ACC";

                case ePlayerStatType.ATS:
                    return "ATS";

                case ePlayerStatType.MOVE:
                    return "MOVE";
            }

            string result = _statType.ToString();
            return result;
        }

        ///<summary>
        /// 장비 스탯 수치 포맷
        ///</summary>
        private string FormatEquipmentStatValue( ePlayerStatType _statType, float _statValue )
        {
            string prefixText = _statValue >= 0.0f ? "+" : string.Empty;
            bool isPercentStat = IsPercentEquipmentStat( _statType );
            string numberText = isPercentStat ? $"{prefixText}{_statValue:0.##}%" : $"{prefixText}{_statValue:0.##}";
            return numberText;
        }

        ///<summary>
        /// 퍼센트 스탯 여부 판단
        ///</summary>
        private bool IsPercentEquipmentStat( ePlayerStatType _statType )
        {
            bool result = _statType == ePlayerStatType.CRT || _statType == ePlayerStatType.CRD;
            return result;
        }

        ///<summary>
        /// 툴팁 화면 내부 보정
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
