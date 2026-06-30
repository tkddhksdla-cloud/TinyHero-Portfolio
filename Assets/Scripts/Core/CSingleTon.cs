using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 싱글톤 클래스
    ///</summary>
    public abstract class CSingleTon<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object SyncRoot = new object();
        private static bool isApplicationQuitting;

        ///<summary>
        /// 싱글톤 인스턴스 정보
        ///</summary>
        public static T Instance
        {
            get
            {
                if ( isApplicationQuitting )
                {
                    return null;
                }

                if ( instance != null )
                {
                    T cachedInstance = instance;
                    return cachedInstance;
                }

                lock ( SyncRoot )
                {
                    if ( instance == null )
                    {
                        instance = FindOrCreateInstance();
                    }

                    T resolvedInstance = instance;
                    return resolvedInstance;
                }
            }
        }

        ///<summary>
        /// 기존 싱글톤 인스턴스 조회 시도
        ///</summary>
        public static bool TryGetExistingInstance( out T _instance )
        {
            _instance = null;

            if ( isApplicationQuitting )
            {
                return false;
            }

            if ( instance != null )
            {
                _instance = instance;
                return true;
            }

            T foundInstance = Object.FindAnyObjectByType<T>( FindObjectsInactive.Include );

            if ( foundInstance == null )
            {
                return false;
            }

            instance = foundInstance;
            _instance = foundInstance;
            return true;
        }

        ///<summary>
        /// 컴포넌트 초기화
        ///</summary>
        protected virtual void Awake()
        {
            if ( isApplicationQuitting )
            {
                return;
            }

            T currentInstance = this as T;

            if ( currentInstance == null )
            {
                Debug.LogError( $"{nameof( CSingleTon<T> )} type mismatch on {name}." );
                return;
            }

            if ( instance == null )
            {
                instance = currentInstance;
                DontDestroyOnLoad( gameObject );
                return;
            }

            if ( ReferenceEquals( instance, currentInstance ) )
            {
                DontDestroyOnLoad( gameObject );
                return;
            }

            Destroy( gameObject );
        }

        ///<summary>
        /// 종료 상태 기록
        ///</summary>
        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        ///<summary>
        /// 인스턴스 참조 정리
        ///</summary>
        protected virtual void OnDestroy()
        {
            T currentInstance = this as T;

            if ( ReferenceEquals( instance, currentInstance ) )
            {
                instance = null;
            }
        }

        ///<summary>
        /// 기존 또는 신규 인스턴스 탐색
        ///</summary>
        private static T FindOrCreateInstance()
        {
            T foundInstance = Object.FindAnyObjectByType<T>( FindObjectsInactive.Include );

            if ( foundInstance != null )
            {
                return foundInstance;
            }

            GameObject singletonObject = new GameObject( typeof( T ).Name );
            T createdInstance = singletonObject.AddComponent<T>();
            DontDestroyOnLoad( singletonObject );
            return createdInstance;
        }
    }
}


