using System.Reflection;
using NUnit.Framework;
using TinyHero.Core;
using UnityEngine;

namespace TinyHero.Tests
{
    ///<summary>
    /// 저장 보호 포맷 EditMode 검증
    ///</summary>
    public sealed class SaveProtectionTests
    {
        private CSaveManager saveManager;
        private GameObject saveManagerObject;

        ///<summary>
        /// 테스트용 저장 매니저 생성
        ///</summary>
        [SetUp]
        public void SetUp()
        {
            saveManagerObject = new GameObject( "SaveProtectionTests_CSaveManager" );
            saveManager = saveManagerObject.AddComponent<CSaveManager>();
        }

        ///<summary>
        /// 테스트용 저장 매니저 정리
        ///</summary>
        [TearDown]
        public void TearDown()
        {
            if ( saveManagerObject != null )
            {
                Object.DestroyImmediate( saveManagerObject );
            }
        }

        ///<summary>
        /// 보호 저장 문자열 생성과 복호화 검증
        ///</summary>
        [Test]
        public void ProtectedSaveText_RoundTripsReadableJson()
        {
            string sourceJsonText = "{\"saveVersion\":1,\"mapId\":\"TestMap\"}";

            bool isProtected = InvokeTryProtectSaveJsonText( sourceJsonText, out string protectedSaveText );
            bool isResolved = InvokeTryResolveReadableSaveJsonText( protectedSaveText, out string resolvedJsonText );

            Assert.IsTrue( isProtected );
            Assert.IsFalse( string.IsNullOrWhiteSpace( protectedSaveText ) );
            Assert.IsTrue( protectedSaveText.Contains( "TinyHeroSaveProtectedV1" ) );
            Assert.IsFalse( protectedSaveText.Contains( "\"mapId\":\"TestMap\"" ) );
            Assert.IsTrue( isResolved );
            Assert.AreEqual( sourceJsonText, resolvedJsonText );
        }

        ///<summary>
        /// 보호 저장 문자열 변조 차단 검증
        ///</summary>
        [Test]
        public void ProtectedSaveText_RejectsTamperedPayload()
        {
            string sourceJsonText = "{\"saveVersion\":1,\"mapId\":\"TestMap\"}";

            bool isProtected = InvokeTryProtectSaveJsonText( sourceJsonText, out string protectedSaveText );
            Assert.IsTrue( isProtected );

            string tamperedSaveText = protectedSaveText.Replace( "\"hmac\": \"", "\"hmac\": \"A" );
            bool isResolved = InvokeTryResolveReadableSaveJsonText( tamperedSaveText, out string resolvedJsonText );

            Assert.IsFalse( isResolved );
            Assert.AreEqual( string.Empty, resolvedJsonText );
        }

        ///<summary>
        /// 구버전 평문 저장 문자열 호환 검증
        ///</summary>
        [Test]
        public void LegacyPlainSaveText_ResolvesAndMigratesQuantityField()
        {
            string legacyJsonText = "{\"saveVersion\":1,\"quantity\":7}";

            bool isResolved = InvokeTryResolveReadableSaveJsonText( legacyJsonText, out string resolvedJsonText );
            string migratedJsonText = InvokeMigrateLegacySaveJsonText( resolvedJsonText );

            Assert.IsTrue( isResolved );
            Assert.AreEqual( legacyJsonText, resolvedJsonText );
            Assert.IsTrue( migratedJsonText.Contains( "\"quantityValue\":7" ) );
        }

        ///<summary>
        /// 저장 JSON 보호 private 메서드 호출
        ///</summary>
        private bool InvokeTryProtectSaveJsonText( string _sourceJsonText, out string _protectedSaveText )
        {
            MethodInfo methodInfo = typeof( CSaveManager ).GetMethod( "TryProtectSaveJsonText", BindingFlags.NonPublic | BindingFlags.Instance );
            Assert.IsNotNull( methodInfo );

            object[] argumentArray = new object[] { _sourceJsonText, string.Empty };
            bool result = ( bool )methodInfo.Invoke( saveManager, argumentArray );
            _protectedSaveText = argumentArray[ 1 ] as string;

            return result;
        }

        ///<summary>
        /// 저장 JSON 읽기 private 메서드 호출
        ///</summary>
        private bool InvokeTryResolveReadableSaveJsonText( string _serializedSaveText, out string _readableJsonText )
        {
            MethodInfo methodInfo = typeof( CSaveManager ).GetMethod( "TryResolveReadableSaveJsonText", BindingFlags.NonPublic | BindingFlags.Instance );
            Assert.IsNotNull( methodInfo );

            object[] argumentArray = new object[] { _serializedSaveText, string.Empty };
            bool result = ( bool )methodInfo.Invoke( saveManager, argumentArray );
            _readableJsonText = argumentArray[ 1 ] as string;

            return result;
        }

        ///<summary>
        /// 구버전 저장 JSON 마이그레이션 private 메서드 호출
        ///</summary>
        private string InvokeMigrateLegacySaveJsonText( string _serializedJsonText )
        {
            MethodInfo methodInfo = typeof( CSaveManager ).GetMethod( "MigrateLegacySaveJsonText", BindingFlags.NonPublic | BindingFlags.Instance );
            Assert.IsNotNull( methodInfo );

            string result = methodInfo.Invoke( saveManager, new object[] { _serializedJsonText } ) as string;
            return result;
        }
    }
}
