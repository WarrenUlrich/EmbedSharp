using NUnit.Framework;
using EmbedSharp;

namespace EmbedSharp.Tests2
{
    public class Tests
    {

        [EmbedFile(Path = "C:\\HelloWorld.txt")]
        private static string _testString;

        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Test1()
        {
            if (TestString != "bingus wow!")
                Assert.Fail();

            Assert.Pass();
        }
    }
}