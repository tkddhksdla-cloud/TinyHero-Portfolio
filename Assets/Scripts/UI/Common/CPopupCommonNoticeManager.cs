using TinyHero.Core;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 공용 안내 팝업 관리 컴포넌트
    ///</summary>
    public sealed class CPopupCommonNoticeManager : CSingleTon<CPopupCommonNoticeManager>
    {
        private const string PopupPrefabResourcePath = "Prefabs/UI/Common/PopupCommonNotice";

        private GameObject popupPrefabObject;
        private CPopupCommonNotice popupCommonNoticeInstance;

        ///<summary>
        /// 공용 안내 팝업 표시 처리
        ///</summary>
        public bool ShowNotice( string _descriptionText, string _positiveButtonText, System.Action _positiveButtonAction, string _negativeButtonText, System.Action _negativeButtonAction )
        {
            bool hasPopupInstance = EnsurePopupInstance();

            if ( hasPopupInstance == false || popupCommonNoticeInstance == null )
            {
                return false;
            }

            popupCommonNoticeInstance.Show( _descriptionText, _positiveButtonText, _positiveButtonAction, _negativeButtonText, _negativeButtonAction );
            return true;
        }

        ///<summary>
        /// 팝업 인스턴스 생성 보장
        ///</summary>
        private bool EnsurePopupInstance()
        {
            if ( popupCommonNoticeInstance != null )
            {
                return true;
            }

            if ( popupPrefabObject == null )
            {
                GameObject loadedPrefabObject = Resources.Load<GameObject>( PopupPrefabResourcePath );
                popupPrefabObject = loadedPrefabObject;
            }

            CUINavigationController navigationController = CUINavigationController.Instance;

            if ( popupPrefabObject == null || navigationController == null )
            {
                return false;
            }

            CPopupCommonNotice createdPopupCommonNotice = navigationController.AddPopup<CPopupCommonNotice>( popupPrefabObject, true );
            popupCommonNoticeInstance = createdPopupCommonNotice;
            return popupCommonNoticeInstance != null;
        }
    }
}
