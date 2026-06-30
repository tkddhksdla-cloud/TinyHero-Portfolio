using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core
{
    ///<summary>
    /// 공용 오브젝트 풀 대여 및 반납 매니저
    ///</summary>
    public sealed class CObjectPoolManager : CSingleTon<CObjectPoolManager>
    {
        ///<summary>
        /// 기존 오브젝트 풀 매니저 인스턴스 조회 시도
        ///</summary>
        public static bool TryGetInstance( out CObjectPoolManager _objectPoolManager )
        {
            bool result = TryGetExistingInstance( out _objectPoolManager );
            return result;
        }

        ///<summary>
        /// 안전한 풀 등록 시도
        ///</summary>
        public static bool TryEnsurePoolRegistered<T>( string _poolKey, Func<T> _createItemHandler, Action<T> _onGetItemHandler, Action<T> _onReleaseItemHandler, Action<T> _onDestroyItemHandler = null ) where T : class
        {
            bool hasObjectPoolManager = TryGetInstance( out CObjectPoolManager objectPoolManager );

            if ( hasObjectPoolManager == false || objectPoolManager == null )
            {
                return false;
            }

            bool result = objectPoolManager.EnsurePoolRegistered<T>( _poolKey, _createItemHandler, _onGetItemHandler, _onReleaseItemHandler, _onDestroyItemHandler );
            return result;
        }

        ///<summary>
        /// 안전한 풀 오브젝트 대여 시도
        ///</summary>
        public static bool TryGet<T>( string _poolKey, out T _item ) where T : class
        {
            _item = null;
            bool hasObjectPoolManager = TryGetInstance( out CObjectPoolManager objectPoolManager );

            if ( hasObjectPoolManager == false || objectPoolManager == null )
            {
                return false;
            }

            _item = objectPoolManager.Get<T>( _poolKey );
            bool hasItem = _item != null;
            return hasItem;
        }

        ///<summary>
        /// 안전한 풀 오브젝트 반환 시도
        ///</summary>
        public static bool TryRelease<T>( string _poolKey, T _item ) where T : class
        {
            bool hasObjectPoolManager = TryGetInstance( out CObjectPoolManager objectPoolManager );

            if ( hasObjectPoolManager == false || objectPoolManager == null )
            {
                return false;
            }

            bool result = objectPoolManager.Release( _poolKey, _item );
            return result;
        }

        ///<summary>
        /// 안전한 풀 정리 시도
        ///</summary>
        public static bool TryClearPool( string _poolKey )
        {
            bool hasObjectPoolManager = TryGetInstance( out CObjectPoolManager objectPoolManager );

            if ( hasObjectPoolManager == false || objectPoolManager == null )
            {
                return false;
            }

            bool result = objectPoolManager.ClearPool( _poolKey );
            return result;
        }

        private interface IObjectPoolEntry
        {
            ///<summary>
            /// 풀 엔트리 정리
            ///</summary>
            void Clear();

            ///<summary>
            /// 엔트리 아이템 타입 반환
            ///</summary>
            Type GetItemType();
        }

        private sealed class CObjectPoolEntry<T> : IObjectPoolEntry where T : class
        {
            private readonly CObjectPool<T> pool;

            ///<summary>
            /// 풀 엔트리 초기화
            ///</summary>
            public CObjectPoolEntry( Func<T> _createItemHandler, Action<T> _onGetItemHandler, Action<T> _onReleaseItemHandler, Action<T> _onDestroyItemHandler )
            {
                pool = new CObjectPool<T>( _createItemHandler, _onGetItemHandler, _onReleaseItemHandler, _onDestroyItemHandler );
            }

            ///<summary>
            /// 엔트리 아이템 대여
            ///</summary>
            public T Get()
            {
                T result = pool.Get();
                return result;
            }

            ///<summary>
            /// 엔트리 아이템 반납
            ///</summary>
            public void Release( T _item )
            {
                pool.Release( _item );
            }

            ///<summary>
            /// 풀 엔트리 정리
            ///</summary>
            public void Clear()
            {
                pool.Clear();
            }

            ///<summary>
            /// 엔트리 아이템 타입 반환
            ///</summary>
            public Type GetItemType()
            {
                Type result = typeof( T );
                return result;
            }
        }

        private readonly Dictionary<string, IObjectPoolEntry> poolEntryByKey = new Dictionary<string, IObjectPoolEntry>();

        ///<summary>
        /// 풀 등록 보장
        ///</summary>
        public bool EnsurePoolRegistered<T>( string _poolKey, Func<T> _createItemHandler, Action<T> _onGetItemHandler, Action<T> _onReleaseItemHandler, Action<T> _onDestroyItemHandler = null ) where T : class
        {
            if ( string.IsNullOrWhiteSpace( _poolKey ) )
            {
                return false;
            }

            string trimmedPoolKey = _poolKey.Trim();

            if ( poolEntryByKey.TryGetValue( trimmedPoolKey, out IObjectPoolEntry existingEntry ) )
            {
                bool isValidEntryType = existingEntry is CObjectPoolEntry<T>;

                if ( isValidEntryType == false )
                {
                    Debug.LogError( $"Pool key type mismatch: {trimmedPoolKey} ({existingEntry.GetItemType().Name} != {typeof( T ).Name})." );
                    return false;
                }

                return true;
            }

            CObjectPoolEntry<T> createdEntry = new CObjectPoolEntry<T>( _createItemHandler, _onGetItemHandler, _onReleaseItemHandler, _onDestroyItemHandler );
            poolEntryByKey.Add( trimmedPoolKey, createdEntry );
            return true;
        }

        ///<summary>
        /// 풀 아이템 대여
        ///</summary>
        public T Get<T>( string _poolKey ) where T : class
        {
            CObjectPoolEntry<T> poolEntry = GetTypedPoolEntry<T>( _poolKey );

            if ( poolEntry == null )
            {
                return null;
            }

            T result = poolEntry.Get();
            return result;
        }

        ///<summary>
        /// 풀 아이템 반납
        ///</summary>
        public bool Release<T>( string _poolKey, T _item ) where T : class
        {
            CObjectPoolEntry<T> poolEntry = GetTypedPoolEntry<T>( _poolKey );

            if ( poolEntry == null )
            {
                return false;
            }

            poolEntry.Release( _item );
            return true;
        }

        ///<summary>
        /// 단일 풀 정리
        ///</summary>
        public bool ClearPool( string _poolKey )
        {
            if ( string.IsNullOrWhiteSpace( _poolKey ) )
            {
                return false;
            }

            string trimmedPoolKey = _poolKey.Trim();
            bool hasPool = poolEntryByKey.TryGetValue( trimmedPoolKey, out IObjectPoolEntry poolEntry );

            if ( hasPool == false || poolEntry == null )
            {
                return false;
            }

            poolEntry.Clear();
            poolEntryByKey.Remove( trimmedPoolKey );
            return true;
        }

        ///<summary>
        /// 전체 풀 정리
        ///</summary>
        public void ClearAllPools()
        {
            List<string> poolKeyList = new List<string>( poolEntryByKey.Keys );
            int poolCount = poolKeyList.Count;

            for ( int index = 0; index < poolCount; index++ )
            {
                string poolKey = poolKeyList[ index ];
                ClearPool( poolKey );
            }
        }

        ///<summary>
        /// 종료 시 풀 정리
        ///</summary>
        protected override void OnDestroy()
        {
            ClearAllPools();
            base.OnDestroy();
        }

        ///<summary>
        /// 타입 일치 풀 엔트리 반환
        ///</summary>
        private CObjectPoolEntry<T> GetTypedPoolEntry<T>( string _poolKey ) where T : class
        {
            if ( string.IsNullOrWhiteSpace( _poolKey ) )
            {
                return null;
            }

            string trimmedPoolKey = _poolKey.Trim();
            bool hasPool = poolEntryByKey.TryGetValue( trimmedPoolKey, out IObjectPoolEntry poolEntry );

            if ( hasPool == false || poolEntry == null )
            {
                return null;
            }

            CObjectPoolEntry<T> typedEntry = poolEntry as CObjectPoolEntry<T>;

            if ( typedEntry == null )
            {
                Debug.LogError( $"Pool key type mismatch: {trimmedPoolKey} ({poolEntry.GetItemType().Name} != {typeof( T ).Name})." );
                return null;
            }

            return typedEntry;
        }
    }
}
