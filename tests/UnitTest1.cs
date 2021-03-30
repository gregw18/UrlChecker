using System;
using Xunit;

namespace tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            int result = 3;
            Assert.Equal(4, result);
        }
    }
}
