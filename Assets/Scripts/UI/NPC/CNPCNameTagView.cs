using TMPro;
using UnityEngine;


    ///<summary>
    /// NPC 이름표 UI 뷰 컴포넌트
    ///</summary>
    public sealed class CNPCNameTagView : MonoBehaviour
    {
        private const string NameTextObjectPath = "NAME/NameText";

        [SerializeField] private RectTransform targetRectTransform;
        [SerializeField] private TMP_Text nameText;

        ///<summary>
        /// 이름표 레이아웃 준비
        ///</summary>
        public void PrepareLayout()
        {
            if ( targetRectTransform == null )
            {
                RectTransform resolvedRectTransform = transform as RectTransform;
                targetRectTransform = resolvedRectTransform;
            }

            if ( nameText == null )
            {
                Transform nameTextTransform = transform.Find( NameTextObjectPath );

                if ( nameTextTransform != null )
                {
                    TMP_Text resolvedNameText = nameTextTransform.GetComponent<TMP_Text>();
                    nameText = resolvedNameText;
                }
            }
        }

        ///<summary>
        /// 이름표 위치 설정
        ///</summary>
        public void SetAnchoredPosition( Vector2 _anchoredPosition )
        {
            if ( targetRectTransform == null )
            {
                PrepareLayout();
            }

            if ( targetRectTransform == null )
            {
                return;
            }

            targetRectTransform.anchoredPosition = _anchoredPosition;
        }

        ///<summary>
        /// 이름표 텍스트 적용
        ///</summary>
        public void ApplyName( string _npcName )
        {
            if ( nameText == null )
            {
                PrepareLayout();
            }

            if ( nameText == null )
            {
                return;
            }

            nameText.text = _npcName;
        }

        ///<summary>
        /// 이름표 초기화
        ///</summary>
        public void ResetView()
        {
            if ( nameText != null )
            {
                nameText.text = string.Empty;
            }

            if ( targetRectTransform != null )
            {
                targetRectTransform.anchoredPosition = Vector2.zero;
            }
        }
    }
