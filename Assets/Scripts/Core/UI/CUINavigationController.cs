using System;
using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyHero.UI
{
    ///<summary>
    /// 전역 UI 네비게이션 관리 매니저
    ///</summary>
    public sealed class CUINavigationController : CSingleTon<CUINavigationController>
    {
        private const string RootCanvasObjectName = "Canvas";
        private const string InteractionCanvasObjectName = "Canvas_InteractionUI";

        private readonly List<CUILayer> popupLayerList = new List<CUILayer>();
        private readonly List<CUILayer> viewLayerList = new List<CUILayer>();
        private readonly Dictionary<GameObject, CUILayer> cachedPopupDictionary = new Dictionary<GameObject, CUILayer>();
        private readonly Dictionary<GameObject, CUILayer> cachedViewDictionary = new Dictionary<GameObject, CUILayer>();
        private readonly Dictionary<eResourceKey, List<Action<CUILayer>>> pendingPopupHandlerDictionary = new Dictionary<eResourceKey, List<Action<CUILayer>>>();
        private readonly HashSet<eResourceKey> loadingPopupResourceKeySet = new HashSet<eResourceKey>();

        ///<summary>
        /// ESC 입력 감시 처리
        ///</summary>
        private void Update()
        {
            CleanupClosedLayers();

            if ( Input.GetKeyDown( KeyCode.Escape ) == false )
            {
                return;
            }

            TryCloseTopmostLayer();
        }

        ///<summary>
        /// 팝업 UI 동적 생성
        ///</summary>
        public T AddPopup<T>( GameObject _prefabObject, bool _shouldReuseExistingInstance = false ) where T : CUIPopup
        {
            T createdPopup = AddLayerInternal<T>( _prefabObject, popupLayerList, cachedPopupDictionary, InteractionCanvasObjectName, _shouldReuseExistingInstance );
            return createdPopup;
        }

        ///<summary>
        /// 팝업 UI 비동기 동적 생성
        ///</summary>
        public void AddPopupAsync<T>( eResourceKey _resourceKey, bool _shouldReuseExistingInstance, Action<T> _onCompleted ) where T : CUIPopup
        {
            if ( _resourceKey == eResourceKey.NONE )
            {
                InvokePopupCompletedHandler( _onCompleted, null );
                return;
            }

            AddPendingPopupHandler( _resourceKey, ( CUILayer _createdLayer ) =>
            {
                T typedLayer = _createdLayer as T;
                InvokePopupCompletedHandler( _onCompleted, typedLayer );
            } );

            if ( loadingPopupResourceKeySet.Contains( _resourceKey ) )
            {
                return;
            }

            loadingPopupResourceKeySet.Add( _resourceKey );
            CResourceManager resourceManager = CResourceManager.Instance;

            if ( resourceManager == null )
            {
                loadingPopupResourceKeySet.Remove( _resourceKey );
                FlushPendingPopupHandlers( _resourceKey, null );
                return;
            }

            resourceManager.LoadPrefabAsync( _resourceKey, ( GameObject _loadedPrefabObject ) =>
            {
                loadingPopupResourceKeySet.Remove( _resourceKey );
                T createdPopup = AddPopup<T>( _loadedPrefabObject, _shouldReuseExistingInstance );
                FlushPendingPopupHandlers( _resourceKey, createdPopup );
            } );
        }

        ///<summary>
        /// 리소스 키 기반 팝업 표시 요청
        ///</summary>
        public bool ShowPopup<T>( eResourceKey _resourceKey, bool _shouldReuseExistingInstance, Action<T> _configureAction ) where T : CUIPopup
        {
            if ( _resourceKey == eResourceKey.NONE )
            {
                return false;
            }

            AddPopupAsync<T>( _resourceKey, _shouldReuseExistingInstance, ( T _popup ) =>
            {
                if ( _popup == null || _configureAction == null )
                {
                    return;
                }

                _configureAction.Invoke( _popup );
            } );

            return true;
        }

        ///<summary>
        /// 공용 안내 팝업 표시 요청
        ///</summary>
        public bool ShowCommonNotice( string _descriptionText, string _positiveButtonText, Action _positiveButtonAction, string _negativeButtonText, Action _negativeButtonAction )
        {
            bool result = ShowPopup<CPopupCommonNotice>( eResourceKey.POPUP_COMMON_NOTICE, true, ( CPopupCommonNotice _popup ) =>
            {
                _popup.Show( _descriptionText, _positiveButtonText, _positiveButtonAction, _negativeButtonText, _negativeButtonAction );
            } );
            return result;
        }

        ///<summary>
        /// 공용 입력 팝업 표시 요청
        ///</summary>
        public bool ShowCommonInputField( string _descriptionText, string _initialText, string _placeholderText, string _positiveButtonText, Action<string> _submitAction, Action _closeAction )
        {
            bool result = ShowPopup<CPopupCommonInputField>( eResourceKey.POPUP_COMMON_INPUT_FIELD, true, ( CPopupCommonInputField _popup ) =>
            {
                _popup.Show( _descriptionText, _initialText, _placeholderText, _positiveButtonText, _submitAction, _closeAction );
            } );
            return result;
        }

        ///<summary>
        /// 전체 화면 UI 동적 생성
        ///</summary>
        public T AddViewController<T>( GameObject _prefabObject, bool _shouldReuseExistingInstance = false ) where T : CUIView
        {
            T createdView = AddLayerInternal<T>( _prefabObject, viewLayerList, cachedViewDictionary, InteractionCanvasObjectName, _shouldReuseExistingInstance );
            return createdView;
        }

        ///<summary>
        /// 기존 팝업 UI 등록
        ///</summary>
        public bool RegisterPopup( CUIPopup _popupLayer )
        {
            bool isRegistered = RegisterLayerInternal( _popupLayer, popupLayerList );
            return isRegistered;
        }

        ///<summary>
        /// 기존 전체 화면 UI 등록
        ///</summary>
        public bool RegisterViewController( CUIView _viewLayer )
        {
            bool isRegistered = RegisterLayerInternal( _viewLayer, viewLayerList );
            return isRegistered;
        }

        ///<summary>
        /// 네비게이션 레이어 등록 해제
        ///</summary>
        public void UnregisterLayer( CUILayer _layer )
        {
            if ( _layer == null )
            {
                return;
            }

            RemoveLayerInternal( popupLayerList, _layer );
            RemoveLayerInternal( viewLayerList, _layer );
        }

        ///<summary>
        /// 최상단 네비게이션 레이어 닫기 시도
        ///</summary>
        public bool TryCloseTopmostLayer()
        {
            CleanupClosedLayers();
            CUILayer popupLayer = FindTopmostClosableLayer( popupLayerList );

            if ( popupLayer != null )
            {
                popupLayer.CloseNavigationLayer();
                CleanupClosedLayers();
                return true;
            }

            CUILayer viewLayer = FindTopmostClosableLayer( viewLayerList );

            if ( viewLayer != null )
            {
                viewLayer.CloseNavigationLayer();
                CleanupClosedLayers();
                return true;
            }

            return false;
        }

        ///<summary>
        /// 네비게이션 레이어 동적 생성 내부 처리
        ///</summary>
        private T AddLayerInternal<T>( GameObject _prefabObject, List<CUILayer> _targetLayerList, Dictionary<GameObject, CUILayer> _cachedLayerDictionary, string _preferredCanvasObjectName, bool _shouldReuseExistingInstance ) where T : CUILayer
        {
            if ( _prefabObject == null )
            {
                return null;
            }

            CleanupCachedLayerDictionary( _cachedLayerDictionary );

            if ( _shouldReuseExistingInstance )
            {
                bool hasCachedLayer = _cachedLayerDictionary.TryGetValue( _prefabObject, out CUILayer cachedLayerBase );
                T cachedLayer = cachedLayerBase as T;

                if ( hasCachedLayer && cachedLayer != null )
                {
                    RegisterLayerInternal( cachedLayer, _targetLayerList );
                    cachedLayer.SetLayerVisible( true );
                    cachedLayer.BringLayerToFront();
                    return cachedLayer;
                }
            }

            RectTransform parentRectTransform = ResolveParentRectTransform( _preferredCanvasObjectName );

            if ( parentRectTransform == null )
            {
                return null;
            }

            GameObject createdObject = Instantiate( _prefabObject, parentRectTransform );
            createdObject.name = _prefabObject.name;
            T createdLayer = createdObject.GetComponent<T>();

            if ( createdLayer == null )
            {
                createdLayer = createdObject.AddComponent<T>();
            }

            bool isRegistered = RegisterLayerInternal( createdLayer, _targetLayerList );

            if ( isRegistered == false )
            {
                Destroy( createdObject );
                return null;
            }

            if ( _shouldReuseExistingInstance )
            {
                _cachedLayerDictionary[ _prefabObject ] = createdLayer;
            }

            createdLayer.SetLayerVisible( true );
            createdLayer.BringLayerToFront();
            return createdLayer;
        }

        ///<summary>
        /// 네비게이션 레이어 등록 내부 처리
        ///</summary>
        private bool RegisterLayerInternal( CUILayer _layer, List<CUILayer> _targetLayerList )
        {
            if ( _layer == null )
            {
                return false;
            }

            RemoveLayerInternal( _targetLayerList, _layer );
            _targetLayerList.Add( _layer );
            _layer.BringLayerToFront();
            return true;
        }

        ///<summary>
        /// 닫힘 네비게이션 레이어 정리
        ///</summary>
        private void CleanupClosedLayers()
        {
            CleanupLayerList( popupLayerList );
            CleanupLayerList( viewLayerList );
            CleanupCachedLayerDictionary( cachedPopupDictionary );
            CleanupCachedLayerDictionary( cachedViewDictionary );
        }

        ///<summary>
        /// 네비게이션 레이어 목록 정리
        ///</summary>
        private void CleanupLayerList( List<CUILayer> _targetLayerList )
        {
            for ( int index = _targetLayerList.Count - 1; index >= 0; index-- )
            {
                CUILayer layer = _targetLayerList[ index ];

                if ( layer == null )
                {
                    _targetLayerList.RemoveAt( index );
                    continue;
                }

                if ( layer.IsNavigationVisible() )
                {
                    continue;
                }

                _targetLayerList.RemoveAt( index );
            }
        }

        ///<summary>
        /// 최상단 닫기 가능 레이어 탐색
        ///</summary>
        private CUILayer FindTopmostClosableLayer( List<CUILayer> _targetLayerList )
        {
            for ( int index = _targetLayerList.Count - 1; index >= 0; index-- )
            {
                CUILayer layer = _targetLayerList[ index ];

                if ( layer == null )
                {
                    continue;
                }

                if ( layer.IsNavigationVisible() == false )
                {
                    continue;
                }

                if ( layer.CanCloseByEscape() == false )
                {
                    continue;
                }

                return layer;
            }

            return null;
        }

        ///<summary>
        /// 네비게이션 레이어 제거 처리
        ///</summary>
        private void RemoveLayerInternal( List<CUILayer> _targetLayerList, CUILayer _layer )
        {
            if ( _layer == null )
            {
                return;
            }

            for ( int index = _targetLayerList.Count - 1; index >= 0; index-- )
            {
                CUILayer targetLayer = _targetLayerList[ index ];

                if ( targetLayer != _layer )
                {
                    continue;
                }

                _targetLayerList.RemoveAt( index );
            }
        }

        ///<summary>
        /// 캐시 레이어 사전 정리
        ///</summary>
        private void CleanupCachedLayerDictionary( Dictionary<GameObject, CUILayer> _cachedLayerDictionary )
        {
            if ( _cachedLayerDictionary == null || _cachedLayerDictionary.Count == 0 )
            {
                return;
            }

            List<GameObject> removeKeyList = null;

            foreach ( KeyValuePair<GameObject, CUILayer> cachedLayerEntry in _cachedLayerDictionary )
            {
                if ( cachedLayerEntry.Value != null )
                {
                    continue;
                }

                if ( removeKeyList == null )
                {
                    removeKeyList = new List<GameObject>();
                }

                removeKeyList.Add( cachedLayerEntry.Key );
            }

            if ( removeKeyList == null )
            {
                return;
            }

            for ( int index = 0; index < removeKeyList.Count; index++ )
            {
                GameObject removeKey = removeKeyList[ index ];
                _cachedLayerDictionary.Remove( removeKey );
            }
        }

        ///<summary>
        /// 팝업 생성 대기 콜백 등록
        ///</summary>
        private void AddPendingPopupHandler( eResourceKey _resourceKey, Action<CUILayer> _onCompleted )
        {
            if ( _onCompleted == null )
            {
                return;
            }

            bool hasHandlerList = pendingPopupHandlerDictionary.TryGetValue( _resourceKey, out List<Action<CUILayer>> handlerList );

            if ( hasHandlerList == false || handlerList == null )
            {
                handlerList = new List<Action<CUILayer>>();
                pendingPopupHandlerDictionary[ _resourceKey ] = handlerList;
            }

            handlerList.Add( _onCompleted );
        }

        ///<summary>
        /// 팝업 생성 대기 콜백 일괄 호출
        ///</summary>
        private void FlushPendingPopupHandlers( eResourceKey _resourceKey, CUILayer _createdLayer )
        {
            bool hasHandlerList = pendingPopupHandlerDictionary.TryGetValue( _resourceKey, out List<Action<CUILayer>> handlerList );

            if ( hasHandlerList == false || handlerList == null )
            {
                return;
            }

            pendingPopupHandlerDictionary.Remove( _resourceKey );

            for ( int index = 0; index < handlerList.Count; index++ )
            {
                Action<CUILayer> handler = handlerList[ index ];

                if ( handler == null )
                {
                    continue;
                }

                handler.Invoke( _createdLayer );
            }
        }

        ///<summary>
        /// 팝업 생성 완료 콜백 호출
        ///</summary>
        private void InvokePopupCompletedHandler<T>( Action<T> _onCompleted, T _createdPopup ) where T : CUIPopup
        {
            if ( _onCompleted == null )
            {
                return;
            }

            _onCompleted.Invoke( _createdPopup );
        }

        ///<summary>
        /// 레이어 타입별 부모 RectTransform 결정
        ///</summary>
        private RectTransform ResolveParentRectTransform( string _preferredCanvasObjectName )
        {
            GameObject preferredCanvasObject = GameObject.Find( _preferredCanvasObjectName );

            if ( preferredCanvasObject != null )
            {
                RectTransform preferredRectTransform = preferredCanvasObject.transform as RectTransform;

                if ( preferredRectTransform != null )
                {
                    return preferredRectTransform;
                }
            }

            GameObject rootCanvasObject = GameObject.Find( RootCanvasObjectName );

            if ( rootCanvasObject != null )
            {
                Transform nestedCanvasTransform = rootCanvasObject.transform.Find( _preferredCanvasObjectName );
                RectTransform nestedCanvasRectTransform = nestedCanvasTransform as RectTransform;

                if ( nestedCanvasRectTransform != null )
                {
                    return nestedCanvasRectTransform;
                }

                RectTransform rootRectTransform = rootCanvasObject.transform as RectTransform;

                if ( rootRectTransform != null )
                {
                    return rootRectTransform;
                }
            }

            Canvas fallbackCanvas = Object.FindFirstObjectByType<Canvas>( FindObjectsInactive.Include );
            RectTransform fallbackRectTransform = fallbackCanvas != null ? fallbackCanvas.transform as RectTransform : null;
            return fallbackRectTransform;
        }
    }
}
