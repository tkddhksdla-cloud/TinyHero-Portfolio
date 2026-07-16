using System.Collections.Generic;
using TinyHero.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TinyHero.UI
{
    ///<summary>
    /// 토스트 메시지 시스템 매니저
    ///</summary>
    public sealed class CToastMessageSystem : CSingleTon<CToastMessageSystem>
    {
        private const string ToastMessageSystemPrefabResourcePath = "Prefabs/UI/Common/CToastMessageSystem";
        private const string ToastMessagePrefabResourcePath = "Prefabs/UI/Common/ToastMessage";
        private const string ToastMessagePoolKey = "UI.ToastMessageSystem.ToastMessage";
        private const int ToastCanvasSortingOrder = 50;

        [SerializeField] private RectTransform toastCanvasRectTransform;
        [SerializeField] private Transform toastPoolRootRectTransform;
        [SerializeField] private RectTransform toastContentRootRectTransform;

        private readonly CActivePooledObjectTracker<CToastMessage> activeToastMessageTracker = new CActivePooledObjectTracker<CToastMessage>();

        private GameObject toastMessagePrefabObject;

        ///<summary>
        /// 토스트 메시지 시스템 초기화
        ///</summary>
        protected override void Awake()
        {
            base.Awake();

            if ( ReferenceEquals( Instance, this ) == false )
            {
                return;
            }

            EnsureToastSystemReady();
        }

        ///<summary>
        /// 토스트 메시지 시스템 인스턴스 보장
        ///</summary>
        public static CToastMessageSystem EnsureInstance()
        {
            InstantiateSystemPrefabIfNeeded();
            CToastMessageSystem toastMessageSystem = Instance;
            return toastMessageSystem;
        }

        ///<summary>
        /// 토스트 메시지 표시 요청
        ///</summary>
        public static void Show( string _message )
        {
            CToastMessageSystem toastMessageSystem = EnsureInstance();

            if ( toastMessageSystem == null )
            {
                return;
            }

            toastMessageSystem.ShowInternal( _message );
        }

        ///<summary>
        /// 토스트 메시지 표시 처리
        ///</summary>
        private void ShowInternal( string _message )
        {
            if ( string.IsNullOrWhiteSpace( _message ) )
            {
                return;
            }

            bool isReady = EnsureToastSystemReady();

            if ( isReady == false )
            {
                return;
            }

            if ( CObjectPoolManager.TryGet( ToastMessagePoolKey, out CToastMessage toastMessage ) == false || toastMessage == null )
            {
                return;
            }

            activeToastMessageTracker.Track( toastMessage );

            toastMessage.ShowMessage( _message );
        }

        ///<summary>
        /// 토스트 메시지 시스템 프리팹 인스턴스 생성 보장
        ///</summary>
        private static void InstantiateSystemPrefabIfNeeded()
        {
            CToastMessageSystem existingToastMessageSystem = Object.FindAnyObjectByType<CToastMessageSystem>( FindObjectsInactive.Include );

            if ( existingToastMessageSystem != null )
            {
                return;
            }

            GameObject systemPrefabObject = Resources.Load<GameObject>( ToastMessageSystemPrefabResourcePath );

            if ( systemPrefabObject == null )
            {
                return;
            }

            GameObject createdSystemObject = Object.Instantiate( systemPrefabObject );
            createdSystemObject.name = systemPrefabObject.name;
            Object.DontDestroyOnLoad( createdSystemObject );
        }

        ///<summary>
        /// 토스트 메시지 시스템 준비 보장
        ///</summary>
        private bool EnsureToastSystemReady()
        {
            bool isPrefabReady = EnsureToastMessagePrefabLoaded();
            ResolveReferences();
            ApplyCanvasSortingOrder();
            EnsureToastPoolInitialized();
            bool hasPool = CObjectPoolManager.TryEnsurePoolRegistered<CToastMessage>( ToastMessagePoolKey, CreateToastMessage, OnGetToastMessage, OnReleaseToastMessage, OnDestroyToastMessage );
            bool isReady = isPrefabReady && toastCanvasRectTransform != null && toastPoolRootRectTransform != null && toastContentRootRectTransform != null && hasPool;
            return isReady;
        }

        ///<summary>
        /// 토스트 메시지 프리팹 로드 보장
        ///</summary>
        private bool EnsureToastMessagePrefabLoaded()
        {
            if ( toastMessagePrefabObject != null )
            {
                return true;
            }

            GameObject loadedPrefabObject = Resources.Load<GameObject>( ToastMessagePrefabResourcePath );
            toastMessagePrefabObject = loadedPrefabObject;
            bool isLoaded = toastMessagePrefabObject != null;
            return isLoaded;
        }

        ///<summary>
        /// 토스트 메시지 시스템 참조 결정
        ///</summary>
        private void ResolveReferences()
        {
            if ( toastCanvasRectTransform == null )
            {
                Transform canvasTransform = transform.Find( "Canvas_ToastMessage" );
                RectTransform resolvedCanvasRectTransform = canvasTransform as RectTransform;
                toastCanvasRectTransform = resolvedCanvasRectTransform;
            }

            if ( toastPoolRootRectTransform == null && toastCanvasRectTransform != null )
            {
                Transform poolRootTransform = toastCanvasRectTransform.Find( "ToastMessagePool" );
                Transform resolvedPoolRootRectTransform = poolRootTransform;
                toastPoolRootRectTransform = resolvedPoolRootRectTransform;
            }

            if ( toastContentRootRectTransform == null && toastCanvasRectTransform != null )
            {
                Transform contentRootTransform = toastCanvasRectTransform.Find( "ToastMessageContent" );
                RectTransform resolvedContentRootRectTransform = contentRootTransform as RectTransform;
                toastContentRootRectTransform = resolvedContentRootRectTransform;
            }
        }

        ///<summary>
        /// 토스트 캔버스 정렬 순서 반영
        ///</summary>
        private void ApplyCanvasSortingOrder()
        {
            if ( toastCanvasRectTransform == null )
            {
                return;
            }

            Canvas canvasComponent = toastCanvasRectTransform.GetComponent<Canvas>();

            if ( canvasComponent == null )
            {
                return;
            }

            canvasComponent.sortingOrder = ToastCanvasSortingOrder;
        }

        ///<summary>
        /// 토스트 풀 초기화 보장
        ///</summary>
        private void EnsureToastPoolInitialized()
        {
            if ( toastMessagePrefabObject == null || toastPoolRootRectTransform == null || toastContentRootRectTransform == null )
            {
                return;
            }

            CObjectPoolManager.TryEnsurePoolRegistered<CToastMessage>( ToastMessagePoolKey, CreateToastMessage, OnGetToastMessage, OnReleaseToastMessage, OnDestroyToastMessage );
        }

        ///<summary>
        /// 토스트 메시지 인스턴스 생성
        ///</summary>
        private CToastMessage CreateToastMessage()
        {
            if ( toastMessagePrefabObject == null || toastPoolRootRectTransform == null )
            {
                return null;
            }

            GameObject createdToastMessageObject = Instantiate( toastMessagePrefabObject, toastPoolRootRectTransform );
            createdToastMessageObject.name = toastMessagePrefabObject.name;
            CToastMessage createdToastMessage = createdToastMessageObject.GetComponent<CToastMessage>();

            if ( createdToastMessage == null )
            {
                createdToastMessage = createdToastMessageObject.AddComponent<CToastMessage>();
            }

            createdToastMessage.SetReturnToPoolHandler( HandleAutoReturnObjectToToastMessagePool );
            createdToastMessageObject.SetActive( false );
            return createdToastMessage;
        }

        ///<summary>
        /// 토스트 메시지 대여 후처리
        ///</summary>
        private void OnGetToastMessage( CToastMessage _toastMessage )
        {
            if ( _toastMessage == null )
            {
                return;
            }

            _toastMessage.transform.SetParent( toastContentRootRectTransform, false );
            _toastMessage.transform.SetAsLastSibling();
            _toastMessage.gameObject.SetActive( true );
        }

        ///<summary>
        /// 토스트 메시지 반환 후처리
        ///</summary>
        private void OnReleaseToastMessage( CToastMessage _toastMessage )
        {
            if ( _toastMessage == null )
            {
                return;
            }

            activeToastMessageTracker.Untrack( _toastMessage );

            if ( toastPoolRootRectTransform != null )
            {
                _toastMessage.transform.SetParent( toastPoolRootRectTransform, false );
            }

            _toastMessage.gameObject.SetActive( false );
        }

        ///<summary>
        /// 토스트 메시지 파기 후처리
        ///</summary>
        private void OnDestroyToastMessage( CToastMessage _toastMessage )
        {
            if ( _toastMessage == null )
            {
                return;
            }

            Destroy( _toastMessage.gameObject );
        }

        ///<summary>
        /// 자동 반환 토스트 메시지 풀 복귀 처리
        ///</summary>
        private void HandleAutoReturnObjectToToastMessagePool( CAutoPoolReturnObject _autoPoolReturnObject )
        {
            CToastMessage toastMessage = _autoPoolReturnObject as CToastMessage;

            if ( toastMessage == null )
            {
                return;
            }

            CObjectPoolManager.TryRelease( ToastMessagePoolKey, toastMessage );
        }

        ///<summary>
        /// 토스트 활성 목록 일괄 반환
        ///</summary>
        private void ReleaseAllActiveToastMessages()
        {
            List<CToastMessage> copiedActiveToastMessageList = activeToastMessageTracker.CreateSnapshot();
            int activeToastMessageCount = copiedActiveToastMessageList.Count;

            for ( int index = 0; index < activeToastMessageCount; index++ )
            {
                CToastMessage toastMessage = copiedActiveToastMessageList[ index ];

                if ( toastMessage == null )
                {
                    continue;
                }

                CObjectPoolManager.TryRelease( ToastMessagePoolKey, toastMessage );
            }

            activeToastMessageTracker.Clear();
        }

        ///<summary>
        /// 토스트 메시지 시스템 정리
        ///</summary>
        protected override void OnDestroy()
        {
            ReleaseAllActiveToastMessages();
            CObjectPoolManager.TryClearPool( ToastMessagePoolKey );

            base.OnDestroy();
        }
    }
}
