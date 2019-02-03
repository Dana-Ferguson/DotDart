using System;
using System.IO;

using Xunit;

using DotDart;
using Shouldly;
using static Tests.Utility;

namespace Tests
{
    public static class Utility
    {
        private const string preamble = "../../../";
        public static void VerifyDill(string file)
        {
            var source = $"{preamble}scripts/{file}.dart";
            var target = $"{preamble}scripts/{file}.dill";

            if (!File.Exists(source)) throw new Exception($"Source file {source} is missing.");
            // We optimistically believe the kernel file is correct
            if (File.Exists(target)) return;

            Dart.CompileDill(source, target);
        }

        public static ComponentFile Load(string file)
        {
            var target = $"{preamble}scripts/{file}.dill";
            return ComponentFile.Load(target);
        }
    }

    public class UnitTest1
    {
        // A setup where we verify dill files & versions?
        // Run tests against dill files?
        // Do we have tests where we load fill files? And then we have dependent tests of those
        public UnitTest1()
        {
            VerifyDill("hello");
        }

        [Fact]
        public void ComponentFilesMustLoad()
        {
            var hello = Load("hello");
            hello.ShouldNotBeNull();
        }
    }
}