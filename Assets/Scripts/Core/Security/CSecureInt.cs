using System;

namespace TinyHero.Core
{
    ///<summary>
    /// int 런타임 수치 메모리 변조 방어 타입
    ///</summary>
    [Serializable]
    public struct CSecureInt
    {
        private const int IntegritySalt = unchecked( ( int )0x37A4F19D );

        private int encryptedValue;
        private int cryptoKey;
        private int integrityHash;

        ///<summary>
        /// 원본 값 기반 SecureInt 초기화
        ///</summary>
        public CSecureInt( int _value )
        {
            encryptedValue = 0;
            cryptoKey = 0;
            integrityHash = 0;
            SetValue( _value );
        }

        ///<summary>
        /// 원본 int 값 반환 또는 설정
        ///</summary>
        public int Value
        {
            get
            {
                int result = GetValue();
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
        public bool TryGetValue( out int _value )
        {
            if ( IsIntegrityValid() == false )
            {
                _value = 0;
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
            int currentValue = GetValue();
            SetValue( currentValue );
        }

        ///<summary>
        /// 원본 int 값으로 암시적 변환
        ///</summary>
        public static implicit operator int( CSecureInt _secureValue )
        {
            int result = _secureValue.Value;
            return result;
        }

        ///<summary>
        /// int 값으로 SecureInt 암시적 변환
        ///</summary>
        public static implicit operator CSecureInt( int _value )
        {
            CSecureInt result = new CSecureInt( _value );
            return result;
        }

        ///<summary>
        /// 문자열 표현 반환
        ///</summary>
        public override string ToString()
        {
            int value = Value;
            string result = value.ToString();
            return result;
        }

        ///<summary>
        /// 원본 값 반환
        ///</summary>
        private int GetValue()
        {
            if ( IsIntegrityValid() == false )
            {
                return 0;
            }

            int result = DecryptValue();
            return result;
        }

        ///<summary>
        /// 원본 값 설정
        ///</summary>
        private void SetValue( int _value )
        {
            cryptoKey = CSecureNumberUtility.CreateIntCryptoKey();
            encryptedValue = EncryptValue( _value, cryptoKey );
            integrityHash = CalculateIntegrityHash( encryptedValue, cryptoKey );
        }

        ///<summary>
        /// 암호화 값 생성
        ///</summary>
        private static int EncryptValue( int _value, int _cryptoKey )
        {
            int result = _value ^ _cryptoKey;
            return result;
        }

        ///<summary>
        /// 원본 값 복호화
        ///</summary>
        private int DecryptValue()
        {
            int result = encryptedValue ^ cryptoKey;
            return result;
        }

        ///<summary>
        /// 무결성 해시 생성
        ///</summary>
        private static int CalculateIntegrityHash( int _encryptedValue, int _cryptoKey )
        {
            int result = _encryptedValue ^ _cryptoKey ^ IntegritySalt;
            return result;
        }

        ///<summary>
        /// 무결성 상태 유효 여부 반환
        ///</summary>
        private bool IsIntegrityValid()
        {
            if ( encryptedValue == 0 && cryptoKey == 0 && integrityHash == 0 )
            {
                return true;
            }

            int expectedHash = CalculateIntegrityHash( encryptedValue, cryptoKey );
            bool result = expectedHash == integrityHash;
            return result;
        }
    }
}
