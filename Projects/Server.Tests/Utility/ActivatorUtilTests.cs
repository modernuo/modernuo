using Server.Utilities;
using Xunit;

namespace Server.Tests
{
    public class ActivatorUtilTests
    {
        private class TestZeroParamsClass
        {
        }

        private class TestTwoParamsClass
        {
            public int Amount;
            public string Name;

            public TestTwoParamsClass(int amount, string name)
            {
                Amount = amount;
                Name = name;
            }
        }

        [Fact]
        public void TestZeroParamsActivator()
        {
            var result = typeof(TestZeroParamsClass).CreateInstance<TestZeroParamsClass>();
            Assert.IsType<TestZeroParamsClass>(result);
        }

        [Fact]
        public void TestTwoParamsActivator()
        {
            var result = typeof(TestTwoParamsClass).CreateInstance<TestTwoParamsClass>(10, "ModernUO");
            Assert.IsType<TestTwoParamsClass>(result);
            Assert.Equal(10, result.Amount);
            Assert.Equal("ModernUO", result.Name);
        }
    }
}
