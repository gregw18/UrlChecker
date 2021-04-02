using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using GAWUrlChecker;

namespace tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            int result = 4;
            Assert.Equal(4, result);
        }

        [Fact]
        public void Test2()
        {
            Task<bool> result = TimerTriggerCSharp1.SendSnsMessage("topic1", "test msg");
            Assert.Equal(false, result.Result);
        }

    }
}
