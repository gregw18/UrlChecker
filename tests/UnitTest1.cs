using System;
using Xunit;

using GAWTest1;

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
            bool result = TimerTriggerCSharp1.SendSnsMessage("topic1", "test msg");
            Assert.Equal(false, result);
        }

    }
}
