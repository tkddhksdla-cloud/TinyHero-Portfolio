using System;

namespace TinyHero.Core
{
    ///<summary>
    /// Secure 숫자 타입 공용 암호화 보조 유틸리티
    ///</summary>
    internal static class CSecureNumberUtility
    {
        private const int IntFallbackCryptoKey = unchecked( ( int )0x5A17C3D1 );
        private const long LongFallbackCryptoKey = unchecked( ( long )0x5A17C3D15A17C3D1 );

        private static readonly object randomLockObject = new object();
        private static readonly Random random = new Random( Environment.TickCount );

        ///<summary>
        /// int 암호화 키 생성
        ///</summary>
        internal static int CreateIntCryptoKey()
        {
            int cryptoKey = 0;

            lock ( randomLockObject )
            {
                int upperValue = random.Next();
                int lowerValue = random.Next();
                cryptoKey = upperValue ^ ( lowerValue << 16 );
            }

            if ( cryptoKey == 0 )
            {
                cryptoKey = IntFallbackCryptoKey;
            }

            int result = cryptoKey;
            return result;
        }

        ///<summary>
        /// long 암호화 키 생성
        ///</summary>
        internal static long CreateLongCryptoKey()
        {
            long cryptoKey = 0L;

            lock ( randomLockObject )
            {
                long firstValue = random.Next();
                long secondValue = random.Next();
                long thirdValue = random.Next();
                cryptoKey = firstValue | ( secondValue << 31 ) | ( thirdValue << 48 );
            }

            if ( cryptoKey == 0L )
            {
                cryptoKey = LongFallbackCryptoKey;
            }

            long result = cryptoKey;
            return result;
        }

        ///<summary>
        /// float 값을 int 비트로 변환
        ///</summary>
        internal static int ConvertFloatToIntBits( float _value )
        {
            byte[] byteArray = BitConverter.GetBytes( _value );
            int result = BitConverter.ToInt32( byteArray, 0 );
            return result;
        }

        ///<summary>
        /// int 비트를 float 값으로 변환
        ///</summary>
        internal static float ConvertIntBitsToFloat( int _value )
        {
            byte[] byteArray = BitConverter.GetBytes( _value );
            float result = BitConverter.ToSingle( byteArray, 0 );
            return result;
        }
    }
}
