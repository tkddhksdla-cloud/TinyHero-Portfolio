using System.Collections.Generic;

namespace TinyHero.HotfixContracts
{
    ///<summary>
    /// Hotfix 실행 요청 문맥 데이터
    ///</summary>
    public sealed class CHotfixExecutionContext
    {
        private readonly Dictionary<string, string> stringValueByKey = new Dictionary<string, string>();
        private readonly Dictionary<string, int> intValueByKey = new Dictionary<string, int>();
        private readonly Dictionary<string, float> floatValueByKey = new Dictionary<string, float>();

        private string moduleId;
        private string commandId;
        private int version;

        ///<summary>Hotfix 실행 문맥 생성</summary>
        public CHotfixExecutionContext( string _moduleId, string _commandId, int _version )
        {
            moduleId = string.IsNullOrWhiteSpace( _moduleId ) ? string.Empty : _moduleId.Trim();
            commandId = string.IsNullOrWhiteSpace( _commandId ) ? string.Empty : _commandId.Trim();
            version = _version;
        }

        ///<summary>Hotfix 모듈 식별자 반환</summary>
        public string GetModuleId()
        {
            string result = moduleId;
            return result;
        }

        ///<summary>Hotfix 명령 식별자 반환</summary>
        public string GetCommandId()
        {
            string result = commandId;
            return result;
        }

        ///<summary>Hotfix 문맥 버전 반환</summary>
        public int GetVersion()
        {
            int result = version;
            return result;
        }

        ///<summary>문자열 값 설정</summary>
        public void SetStringValue( string _key, string _value )
        {
            if ( string.IsNullOrWhiteSpace( _key ) )
            {
                return;
            }

            string normalizedKey = _key.Trim();
            stringValueByKey[ normalizedKey ] = _value != null ? _value : string.Empty;
        }

        ///<summary>정수 값 설정</summary>
        public void SetIntValue( string _key, int _value )
        {
            if ( string.IsNullOrWhiteSpace( _key ) )
            {
                return;
            }

            string normalizedKey = _key.Trim();
            intValueByKey[ normalizedKey ] = _value;
        }

        ///<summary>실수 값 설정</summary>
        public void SetFloatValue( string _key, float _value )
        {
            if ( string.IsNullOrWhiteSpace( _key ) )
            {
                return;
            }

            string normalizedKey = _key.Trim();
            floatValueByKey[ normalizedKey ] = _value;
        }

        ///<summary>문자열 값 조회 시도</summary>
        public bool TryGetStringValue( string _key, out string _value )
        {
            _value = string.Empty;

            if ( string.IsNullOrWhiteSpace( _key ) )
            {
                return false;
            }

            string normalizedKey = _key.Trim();
            bool result = stringValueByKey.TryGetValue( normalizedKey, out _value );
            return result;
        }

        ///<summary>정수 값 조회 시도</summary>
        public bool TryGetIntValue( string _key, out int _value )
        {
            _value = 0;

            if ( string.IsNullOrWhiteSpace( _key ) )
            {
                return false;
            }

            string normalizedKey = _key.Trim();
            bool result = intValueByKey.TryGetValue( normalizedKey, out _value );
            return result;
        }

        ///<summary>실수 값 조회 시도</summary>
        public bool TryGetFloatValue( string _key, out float _value )
        {
            _value = 0.0f;

            if ( string.IsNullOrWhiteSpace( _key ) )
            {
                return false;
            }

            string normalizedKey = _key.Trim();
            bool result = floatValueByKey.TryGetValue( normalizedKey, out _value );
            return result;
        }
    }
}
