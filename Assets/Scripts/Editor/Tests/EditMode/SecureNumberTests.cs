using System.Reflection;
using NUnit.Framework;
using TinyHero.Core;

namespace TinyHero.Tests
{
    ///<summary>
    /// Secure 숫자 타입 EditMode 검증
    ///</summary>
    public sealed class SecureNumberTests
    {
        ///<summary>
        /// CSecureInt 값 보존과 암시적 변환 검증
        ///</summary>
        [Test]
        public void SecureInt_KeepsValueAndImplicitConversion()
        {
            CSecureInt secureValue = new CSecureInt( 12345 );
            int convertedValue = secureValue;

            Assert.AreEqual( 12345, secureValue.Value );
            Assert.AreEqual( 12345, convertedValue );
            Assert.IsTrue( secureValue.TryGetValue( out int resolvedValue ) );
            Assert.AreEqual( 12345, resolvedValue );
        }

        ///<summary>
        /// CSecureInt 내부 값 변조 감지 검증
        ///</summary>
        [Test]
        public void SecureInt_DetectsEncryptedValueTamper()
        {
            CSecureInt secureValue = new CSecureInt( 77 );
            CSecureInt tamperedValue = TamperSecureField<CSecureInt, int>( secureValue, "encryptedValue", 1 );

            Assert.IsTrue( tamperedValue.IsTampered() );
            Assert.IsFalse( tamperedValue.TryGetValue( out int resolvedValue ) );
            Assert.AreEqual( 0, resolvedValue );
            Assert.AreEqual( 0, tamperedValue.Value );
        }

        ///<summary>
        /// CSecureLong 값 보존과 암시적 변환 검증
        ///</summary>
        [Test]
        public void SecureLong_KeepsValueAndImplicitConversion()
        {
            CSecureLong secureValue = new CSecureLong( 9876543210L );
            long convertedValue = secureValue;

            Assert.AreEqual( 9876543210L, secureValue.Value );
            Assert.AreEqual( 9876543210L, convertedValue );
            Assert.IsTrue( secureValue.TryGetValue( out long resolvedValue ) );
            Assert.AreEqual( 9876543210L, resolvedValue );
        }

        ///<summary>
        /// CSecureLong 내부 값 변조 감지 검증
        ///</summary>
        [Test]
        public void SecureLong_DetectsEncryptedValueTamper()
        {
            CSecureLong secureValue = new CSecureLong( 9876543210L );
            CSecureLong tamperedValue = TamperSecureField<CSecureLong, long>( secureValue, "encryptedValue", 1L );

            Assert.IsTrue( tamperedValue.IsTampered() );
            Assert.IsFalse( tamperedValue.TryGetValue( out long resolvedValue ) );
            Assert.AreEqual( 0L, resolvedValue );
            Assert.AreEqual( 0L, tamperedValue.Value );
        }

        ///<summary>
        /// CSecureFloat 값 보존과 암시적 변환 검증
        ///</summary>
        [Test]
        public void SecureFloat_KeepsValueAndImplicitConversion()
        {
            CSecureFloat secureValue = new CSecureFloat( 123.5f );
            float convertedValue = secureValue;

            Assert.AreEqual( 123.5f, secureValue.Value, 0.0001f );
            Assert.AreEqual( 123.5f, convertedValue, 0.0001f );
            Assert.IsTrue( secureValue.TryGetValue( out float resolvedValue ) );
            Assert.AreEqual( 123.5f, resolvedValue, 0.0001f );
        }

        ///<summary>
        /// CSecureFloat 내부 값 변조 감지 검증
        ///</summary>
        [Test]
        public void SecureFloat_DetectsEncryptedValueTamper()
        {
            CSecureFloat secureValue = new CSecureFloat( 12.25f );
            CSecureFloat tamperedValue = TamperSecureField<CSecureFloat, int>( secureValue, "encryptedValue", 1 );

            Assert.IsTrue( tamperedValue.IsTampered() );
            Assert.IsFalse( tamperedValue.TryGetValue( out float resolvedValue ) );
            Assert.AreEqual( 0.0f, resolvedValue, 0.0001f );
            Assert.AreEqual( 0.0f, tamperedValue.Value, 0.0001f );
        }

        ///<summary>
        /// Secure 타입 private 필드 변조 결과 반환
        ///</summary>
        private static TSecure TamperSecureField<TSecure, TField>( TSecure _secureValue, string _fieldName, TField _xorMask ) where TSecure : struct
        {
            object boxedSecureValue = _secureValue;
            FieldInfo fieldInfo = typeof( TSecure ).GetField( _fieldName, BindingFlags.NonPublic | BindingFlags.Instance );
            Assert.IsNotNull( fieldInfo );

            object currentValue = fieldInfo.GetValue( boxedSecureValue );

            if ( currentValue is int currentIntValue && _xorMask is int intMask )
            {
                int tamperedIntValue = currentIntValue ^ intMask;
                fieldInfo.SetValue( boxedSecureValue, tamperedIntValue );
            }
            else if ( currentValue is long currentLongValue && _xorMask is long longMask )
            {
                long tamperedLongValue = currentLongValue ^ longMask;
                fieldInfo.SetValue( boxedSecureValue, tamperedLongValue );
            }
            else
            {
                Assert.Fail( $"Unsupported secure field type. FieldName: {_fieldName}" );
            }

            TSecure result = ( TSecure )boxedSecureValue;
            return result;
        }
    }
}
