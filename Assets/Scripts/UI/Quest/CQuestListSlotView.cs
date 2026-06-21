using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 퀘스트 슬롯 참조 보관 컴포넌트
    ///</summary>
    public sealed class CQuestListSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject slotRootObject;
        [SerializeField] private CButtonEx button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text questNameText;
        [SerializeField] private GameObject selectHighlightObject;


        ///<summary>
        /// 슬롯 루트 오브젝트 반환
        ///</summary>
        public GameObject GetSlotRootObject()
        {
            GameObject result = slotRootObject != null ? slotRootObject : gameObject;
            return result;
        }

        ///<summary>
        /// 슬롯 버튼 반환
        ///</summary>
        public CButtonEx GetButton()
        {
            CButtonEx result = button;
            return result;
        }

        ///<summary>
        /// 슬롯 배경 이미지 반환
        ///</summary>
        public Image GetBackgroundImage()
        {
            Image result = backgroundImage;
            return result;
        }

        ///<summary>
        /// 퀘스트 이름 텍스트 반환
        ///</summary>
        public TMP_Text GetQuestNameText()
        {
            TMP_Text result = questNameText;
            return result;
        }

        ///<summary>
        /// 선택 하이라이트 오브젝트 반환
        ///</summary>
        public GameObject GetSelectHighlightObject()
        {
            GameObject result = selectHighlightObject;
            return result;
        }

        ///<summary>
        /// 선택 하이라이트 표시 상태 반영
        ///</summary>
        public void SetSelected( bool _isSelected )
        {
            if ( selectHighlightObject == null )
            {
                return;
            }

            selectHighlightObject.SetActive( _isSelected );
        }

        ///<summary>
        /// 슬롯 참조 자동 구성
        ///</summary>
        public void AutoAssignReferences()
        {
            if ( slotRootObject == null )
            {
                slotRootObject = gameObject;
            }

            if ( button == null )
            {
                CButtonEx resolvedButton = GetComponent<CButtonEx>();
                button = resolvedButton;
            }

            if ( backgroundImage == null )
            {
                Transform backgroundTransform = transform.Find( "BG" );
                backgroundImage = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
            }

            if ( questNameText == null )
            {
                Transform questNameTransform = transform.Find( "QuestName" );
                questNameText = questNameTransform != null ? questNameTransform.GetComponent<TMP_Text>() : null;
            }

            if ( selectHighlightObject == null )
            {
                Transform highlightTransform = transform.Find( "SelectHighlightObject" );
                selectHighlightObject = highlightTransform != null ? highlightTransform.gameObject : null;
            }
        }

        ///<summary>
        /// 슬롯 참조 유효성 반환
        ///</summary>
        public bool IsValid()
        {
            bool hasRootObject = GetSlotRootObject() != null;
            bool hasButton = GetButton() != null;
            bool hasBackground = GetBackgroundImage() != null;
            bool hasQuestNameText = GetQuestNameText() != null;
            bool result = hasRootObject && hasButton && hasBackground && hasQuestNameText;
            return result;
        }
    }
}
