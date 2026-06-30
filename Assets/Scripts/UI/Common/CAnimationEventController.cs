using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyHero.UI
{
    ///<summary>
    /// 문자열 기반 애니메이션 이벤트 중계 컴포넌트
    ///</summary>
    public sealed class CAnimationEventController : MonoBehaviour
    {
        private readonly Dictionary<string, Action> eventActionDictionary = new Dictionary<string, Action>();

        ///<summary>
        /// 애니메이션 이벤트 액션 등록
        ///</summary>
        public void RegisterEventAction( string _eventName, Action _eventAction )
        {
            if ( string.IsNullOrWhiteSpace( _eventName ) || _eventAction == null )
            {
                return;
            }

            string normalizedEventName = _eventName.Trim();

            if ( eventActionDictionary.TryGetValue( normalizedEventName, out Action currentAction ) )
            {
                currentAction -= _eventAction;
                currentAction += _eventAction;
                eventActionDictionary[ normalizedEventName ] = currentAction;
                return;
            }

            eventActionDictionary.Add( normalizedEventName, _eventAction );
        }

        ///<summary>
        /// 애니메이션 이벤트 액션 해제
        ///</summary>
        public void UnregisterEventAction( string _eventName, Action _eventAction )
        {
            if ( string.IsNullOrWhiteSpace( _eventName ) || _eventAction == null )
            {
                return;
            }

            string normalizedEventName = _eventName.Trim();

            if ( eventActionDictionary.TryGetValue( normalizedEventName, out Action currentAction ) == false )
            {
                return;
            }

            currentAction -= _eventAction;

            if ( currentAction == null )
            {
                eventActionDictionary.Remove( normalizedEventName );
                return;
            }

            eventActionDictionary[ normalizedEventName ] = currentAction;
        }

        ///<summary>
        /// 모든 애니메이션 이벤트 액션 해제
        ///</summary>
        public void ClearEventActions()
        {
            eventActionDictionary.Clear();
        }

        ///<summary>
        /// 문자열 애니메이션 이벤트 수신
        ///</summary>
        public void OnAnimationEvent( string _eventName )
        {
            if ( string.IsNullOrWhiteSpace( _eventName ) )
            {
                return;
            }

            string normalizedEventName = _eventName.Trim();

            if ( eventActionDictionary.TryGetValue( normalizedEventName, out Action eventAction ) == false || eventAction == null )
            {
                return;
            }

            eventAction.Invoke();
        }
    }
}
