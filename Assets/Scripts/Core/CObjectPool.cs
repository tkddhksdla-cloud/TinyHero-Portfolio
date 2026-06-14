using System;
using System.Collections.Generic;

namespace TinyHero.Core
{
    ///<summary>
    /// 공용 객체 풀 클래스
    ///</summary>
    public sealed class CObjectPool<T> where T : class
    {
        private readonly Stack<T> pooledItemStack = new Stack<T>();
        private readonly Func<T> createItemHandler;
        private readonly Action<T> onGetItemHandler;
        private readonly Action<T> onReleaseItemHandler;
        private readonly Action<T> onDestroyItemHandler;

        ///<summary>
        /// 객체 풀 초기화
        ///</summary>
        public CObjectPool(Func<T> _createItemHandler, Action<T> _onGetItemHandler, Action<T> _onReleaseItemHandler, Action<T> _onDestroyItemHandler = null)
        {
            createItemHandler = _createItemHandler ?? throw new ArgumentNullException( nameof( _createItemHandler ) );
            onGetItemHandler = _onGetItemHandler;
            onReleaseItemHandler = _onReleaseItemHandler;
            onDestroyItemHandler = _onDestroyItemHandler;
        }

        ///<summary>
        /// 풀 아이템 대여
        ///</summary>
        public T Get()
        {
            T pooledItem = null;

            while ( pooledItemStack.Count > 0 && pooledItem == null )
            {
                T stackedItem = pooledItemStack.Pop();
                pooledItem = stackedItem;
            }

            if ( pooledItem == null )
            {
                T createdItem = createItemHandler.Invoke();
                pooledItem = createdItem;
            }

            onGetItemHandler?.Invoke( pooledItem );
            return pooledItem;
        }

        ///<summary>
        /// 풀 아이템 반환
        ///</summary>
        public void Release(T _item)
        {
            if ( _item == null )
            {
                return;
            }

            onReleaseItemHandler?.Invoke( _item );
            pooledItemStack.Push( _item );
        }

        ///<summary>
        /// 풀 비우기
        ///</summary>
        public void Clear()
        {
            while ( pooledItemStack.Count > 0 )
            {
                T pooledItem = pooledItemStack.Pop();

                if ( pooledItem == null )
                {
                    continue;
                }

                onDestroyItemHandler?.Invoke( pooledItem );
            }
        }
    }
}
