using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.Core
{
    /// <summary>
    /// 월드 대상과 풀링 UI 뷰의 일대일 바인딩 수명주기를 관리합니다.
    /// 풀의 생성, 뷰 표시, 좌표 갱신은 호출자가 담당합니다.
    /// </summary>
    public sealed class CTrackedUiBindingMap<TKey, TView>
        where TKey : UnityEngine.Object
        where TView : UnityEngine.Object
    {
        private readonly Dictionary<TKey, TView> viewByTarget = new Dictionary<TKey, TView>();

        public bool TryGetOrCreate( TKey _target, Func<TView> _createViewHandler, out TView _view )
        {
            _view = null;

            if ( ReferenceEquals( _target, null ) || _createViewHandler == null )
            {
                return false;
            }

            bool hasView = viewByTarget.TryGetValue( _target, out TView existingView );

            if ( hasView && existingView != null )
            {
                _view = existingView;
                return true;
            }

            TView createdView = _createViewHandler.Invoke();

            if ( createdView == null )
            {
                return false;
            }

            viewByTarget[ _target ] = createdView;
            _view = createdView;
            return true;
        }

        public bool TryGet( TKey _target, out TView _view )
        {
            _view = null;

            if ( ReferenceEquals( _target, null ) )
            {
                return false;
            }

            bool hasView = viewByTarget.TryGetValue( _target, out TView resolvedView );

            if ( hasView == false || resolvedView == null )
            {
                return false;
            }

            _view = resolvedView;
            return true;
        }

        public bool TryRelease( TKey _target, Action<TView> _releaseViewHandler )
        {
            if ( ReferenceEquals( _target, null ) || _releaseViewHandler == null )
            {
                return false;
            }

            bool hasView = viewByTarget.TryGetValue( _target, out TView view );

            if ( hasView == false )
            {
                return false;
            }

            viewByTarget.Remove( _target );
            _releaseViewHandler.Invoke( view );
            return true;
        }

        public List<TKey> CreateTargetSnapshot()
        {
            List<TKey> targetList = new List<TKey>( viewByTarget.Keys );
            return targetList;
        }
    }
}
