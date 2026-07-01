using System;

namespace TinyHero.Core
{
    ///<summary>
    /// long 런타임 수치 메모리 변조 방어 타입
    ///</summary>
    [Serializable]
    public struct CSecureLong
    {
        private const long IntegritySalt = unchecked( ( long )0x682F9B37682F9B37 );

        private long encryptedValue;
        private long cryptoKey;
        private long integrityHash;

        ///<summary>
        /// 원본 값 기반 SecureLong 초기화
        ///</summary>
        public CSecureLong( long _value )
        {
            encryptedValue = 0L;
            cryptoKey = 0L;
            integrityHash = 0L;
            SetValue( _value );
        }

        ///<summary>
        /// 원본 long 값 반환 또는 설정
        ///</summary>
        public long Value
        {
            get
            {
                long result = GetValue();
                return result;
            }
            set
            {
                SetValue( value );
            }
        }

        ///<summary>
        /// 메모리 변조 감지 여부 반환
        ///</summary>
        public bool IsTampered()
        {
            bool result = IsIntegrityValid() == false;
            return result;
        }

        ///<summary>
        /// 원본 값 복원 시도
        ///</summary>
        public bool TryGetValue( out long _value )
        {
            if ( IsIntegrityValid() == false )
            {
                _value = 0L;
                return false;
            }

            _value = DecryptValue();
            return true;
        }

        ///<summary>
        /// 암호화 키 재생성
        ///</summary>
        public void RefreshCryptoKey()
        {
            long currentValue = GetValue();
            SetValue( currentValue );
        }

        ///<summary>
        /// 원본 long 값으로 암시적 변환
        ///</summary>
        public static implicit operator long( CSecureLong _secureValue )
        {
            long result = _secureValue.Value;
            return result;
        }

        ///<summary>
        /// long 값으로 SecureLong 암시적 변환
        ///</summary>
        public static implicit operator CSecureLong( long _value )
        {
            CSecureLong result = new CSecureLong( _value );
            return result;
        }

        ///<summary>
        /// 문자열 표현 반환
        ///</summary>
        public override string ToString()
        {
            long value = Value;
            string result = value.ToString();
            return result;
        }

        ///<summary>
        /// 원본 값 반환
        ///</summary>
        private long GetValue()
        {
            if ( IsIntegrityValid() == false )
            {
                return 0L;
            }

            long result = DecryptValue();
            return result;
        }

        ///<summary>
        /// 원본 값 설정
        ///</summary>
        private void SetValue( long _value )
        {
            cryptoKey = CSecureNumberUtility.CreateLongCryptoKey();
            encryptedValue = EncryptValue( _value, cryptoKey );
            integrityHash = CalculateIntegrityHash( encryptedValue, cryptoKey );
        }

        ///<summary>
        /// 암호화 값 생성
        ///</summary>
        private static long EncryptValue( long _value, long _cryptoKey )
        {
            long result = _value ^ _cryptoKey;
            return result;
        }

        ///<summary>
        /// 원본 값 복호화
        ///</summary>
        private long DecryptValue()
        {
            long result = encryptedValue ^ cryptoKey;
            return result;
        }

        ///<summary>
        /// 무결성 해시 생성
        ///</summary>
        private static long CalculateIntegrityHash( long _encryptedValue, long _cryptoKey )
        {
            long result = _encryptedValue ^ _cryptoKey ^ IntegritySalt;
            return result;
        }

        ///<summary>
        /// 무결성 상태 유효 여부 반환
        ///</summary>
        private bool IsIntegrityValid()
        {
            if ( encryptedValue == 0L && cryptoKey == 0L && integrityHash == 0L )
            {
                return true;
            }

            long expectedHash = CalculateIntegrityHash( encryptedValue, cryptoKey );
            bool result = expectedHash == integrityHash;
            return result;
        }
    }
}
