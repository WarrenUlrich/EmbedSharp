using System;
using System.Text;
using EmbedSharp;
using Microsoft.CodeAnalysis;

namespace ConsoleApp
{
    public partial class Strings
    {
        [EmbedFile(Path = "TextFile1.txt", Accessibility = Accessibility.Public)]
        private static byte[] _jews;
    }


    partial class Program
    {
        [EmbedFile(Path = "C:\\HelloWorld.txt", Accessibility = Accessibility.Private)]
        private static byte[] _meme;

        static void Main(string[] args)
        {
            Console.WriteLine(Meme[0]);
        }
    }
}
