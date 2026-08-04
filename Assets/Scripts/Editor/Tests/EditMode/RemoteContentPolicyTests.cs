using NUnit.Framework;
using TinyHero.Core;

namespace TinyHero.Tests
{
    /// <summary>
    /// 필수 원격 콘텐츠의 Resources fallback 차단 정책을 검증합니다.
    /// </summary>
    public sealed class RemoteContentPolicyTests
    {
        [Test]
        public void ShouldBlockRequiredRemoteContentFallback_AllowsFallbackWhenRemoteContentIsOptional()
        {
            bool shouldBlock = CGameUtils.ShouldBlockRequiredRemoteContentFallback( false, false );

            Assert.IsFalse( shouldBlock );
        }

        [Test]
        public void ShouldBlockRequiredRemoteContentFallback_BlocksFallbackWhenRemoteUpdateIsDetected()
        {
            bool shouldBlock = CGameUtils.ShouldBlockRequiredRemoteContentFallback( true, false );

            Assert.IsTrue( shouldBlock );
        }

        [Test]
        public void ShouldBlockRequiredRemoteContentFallback_BlocksFallbackWhenRemoteContentIsRequired()
        {
            bool shouldBlock = CGameUtils.ShouldBlockRequiredRemoteContentFallback( false, true );

            Assert.IsTrue( shouldBlock );
        }
    }
}
