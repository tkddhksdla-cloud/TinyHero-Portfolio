using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core
{
    /// <summary>
    /// 자동 반환되는 풀링 오브젝트의 활성 인스턴스를 중복 없이 추적합니다.
    /// </summary>
    public sealed class CActivePooledObjectTracker<TObject> where TObject : Object
    {
        private readonly List<TObject> activeObjectList = new List<TObject>();

        public void Track( TObject _object )
        {
            if ( _object == null || activeObjectList.Contains( _object ) )
            {
                return;
            }

            activeObjectList.Add( _object );
        }

        public void Untrack( TObject _object )
        {
            activeObjectList.Remove( _object );
        }

        public List<TObject> CreateSnapshot()
        {
            List<TObject> result = new List<TObject>( activeObjectList );
            return result;
        }

        public void Clear()
        {
            activeObjectList.Clear();
        }
    }
}
