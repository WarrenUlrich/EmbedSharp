using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbedSharp;
using System;

namespace EmbedSharp.Tests
{
    [TestClass]
    public partial class UnitTest1
    {
        [EmbedFile(Path = "C:\\HelloWorld.txt")]
        public static string _testString;

        [TestMethod]
        public void EmbedStringTest()
        {
            Console.WriteLine(TestString);
            if (TestString != "bingus wow!")
                Assert.Fail();
        }
    }
}
