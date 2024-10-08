namespace Unit_Test
{
    public class StringValidatorTest
    {
        [Fact]
        public void Test1()
        {
            string input = "W6542";
            string D = string.Empty;
            string A = string.Empty;
            Assert.True(StringValidator.SplitAndValidateString(input, out D, out A));
            Assert.Equal("W", D);
            Assert.Equal("6542", A);
        }
        [Fact]
        public void Test2()
        {

            string input = "ZRF12E";
            string D = string.Empty;
            string A = string.Empty;
            Assert.True(StringValidator.SplitAndValidateString(input, out D, out A));
            Assert.Equal("ZR", D);
            Assert.Equal("F12E", A);
        }
        [Fact]
        public void Test3()
        {
            string input = "Z1";
            string D = string.Empty;
            string A = string.Empty;
            Assert.True(StringValidator.SplitAndValidateString(input, out D, out A));
            Assert.Equal("Z", D);
            Assert.Equal("1", A);
        }
        [Fact]
        public void Test4()
        {
            string input = "Z1R";
            string D = string.Empty;
            string A = string.Empty;
            Assert.False(StringValidator.SplitAndValidateString(input, out D, out A));
        }
        [Fact]
        public void Test5()
        {
            string input = "XFF32D";
            string D = string.Empty;
            string A = string.Empty;
            Assert.False(StringValidator.SplitAndValidateString(input, out D, out A));
        }
    }
}