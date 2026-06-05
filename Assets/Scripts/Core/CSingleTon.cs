using UnityEngine;

namespace TinyHero.Core
{
    /// <summary>
    /// 씬 전역에서 하나만 유지되는 컴포넌트 싱글톤 기반 클래스이다.
    /// </summary>
    public abstract class CSingleTon<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object SyncRoot = new object();
        private static bool isApplicationQuitting;

        /// <summary>
        /// 현재 활성화된 싱글톤 인스턴스를 반환한다.
        /// </summary>
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

        /// <summary>
        /// 싱글톤 인스턴스를 초기화하고 중복 생성을 방지한다.
        /// </summary>
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

        /// <summary>
        /// 애플리케이션 종료 시 싱글톤 재생성을 차단한다.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            isApplicationQuitting = true;
        }

        /// <summary>
        /// 오브젝트 파괴 시 내부 인스턴스 참조를 정리한다.
        /// </summary>
        protected virtual void OnDestroy()
        {
            T currentInstance = this as T;

            if ( ReferenceEquals( instance, currentInstance ) )
            {
                instance = null;
            }
        }

        /// <summary>
        /// 씬에서 기존 인스턴스를 찾거나 없으면 새 오브젝트를 생성한다.
        /// </summary>
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
