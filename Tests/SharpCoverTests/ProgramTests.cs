using System;
using System.IO;
using NUnit.Framework;
using System.Linq;
using System.Diagnostics;

namespace Gaillard.SharpCover.Tests
{
    [TestFixture]
    public sealed class ProgramTests
    {
        private string testTargetExePath;
        private bool onDotNet;

 
        [SetUp]
        public void Setup()
        {
            onDotNet = Type.GetType("Mono.Runtime") == null;

            File.Copy ("TestTarget.exe", "TestTargetSut.exe",true);
            testTargetExePath = "TestTargetSut.exe";
          

        }

        [Test]
        public void NoBody()
        {
            var config =
                @"{""assemblies"": [""TestTargetSut.exe""], ""typeInclude"": "".*Tests.*Event.*""}";

            File.WriteAllText("testConfig.json", config);

            Assert.AreEqual(0, Program.Main(new []{ "instrument", "testConfig.json" }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(0, Program.Main(new []{ "check" }));

            Assert.IsTrue(File.ReadLines(Program.RESULTS_FILENAME).Any());
        }

        [Test]
        public void Covered()
        {
            var config =
                @"{""assemblies"": [""TestTargetSut.exe""], ""typeInclude"": "".*TestTarget"", ""methodInclude"": "".*Covered.*""}";

            File.WriteAllText("testConfig.json", config);

            //write some extraneous hit files to make sure they dont affect run
            File.WriteAllText(Program.HITS_FILENAME_PREFIX, "doesnt matter");

            Assert.AreEqual(0, Program.Main(new []{ "instrument", "testConfig.json" }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(0, Program.Main(new []{ "check" }));

            Assert.IsTrue(File.ReadLines(Program.RESULTS_FILENAME).Any());
        }

        [Test]
        public void UncoveredIf()
        {
            var config = @"{""assemblies"": [""TestTargetSut.exe""], ""methodInclude"": "".*UncoveredIf.*""}";

            Assert.AreEqual(0, Program.Main(new []{ "instrument", config }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(1, Program.Main(new []{ "check" }));

            var missCount = File.ReadLines(Program.RESULTS_FILENAME).Where(l => l.StartsWith(Program.MISS_PREFIX)).Count();
            var knownCount = File.ReadLines(Program.RESULTS_FILENAME).Count();

            Assert.IsTrue(knownCount > 0);
            Assert.IsTrue(missCount > 0);
            Assert.IsTrue(knownCount > missCount);
        }

        [Test]
        public void UncoveredLeave()
        {
            var config = @"{""assemblies"": [""TestTargetSut.exe""], ""methodInclude"": "".*UncoveredLeave.*""}";

            Assert.AreEqual(0, Program.Main(new []{ "instrument", config }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(1, Program.Main(new []{ "check" }));

            var missCount = File.ReadLines(Program.RESULTS_FILENAME).Where(l => l.StartsWith(Program.MISS_PREFIX)).Count();
            var knownCount = File.ReadLines(Program.RESULTS_FILENAME).Count();

            Assert.IsTrue(knownCount > 0);
            Assert.IsTrue(missCount > 0);
            Assert.IsTrue(knownCount > missCount);
        }

        [Test]
        public void Nested()
        {
            var config = @"{""assemblies"": [""TestTargetSut.exe""], ""typeInclude"": "".*Nested""}";

            Assert.AreEqual(0, Program.Main(new []{ "instrument", config }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(0, Program.Main(new []{ "check" }));

            Assert.IsTrue(File.ReadLines(Program.RESULTS_FILENAME).Any());
        }

        [Test]
        public void LineExcludes()
        {
            var config =
@"{
    ""assemblies"": [""TestTargetSut.exe""],
    ""typeInclude"": "".*TestTarget"",
    ""methodInclude"": "".*LineExcludes.*"",
    ""methodBodyExcludes"": [
        {
            ""method"": ""System.Void Gaillard.SharpCover.Tests.TestTarget::LineExcludes()"",
            ""lines"": [""++i;"", ""} catch (Exception) {"", ""var b = false; b = !b;//will never get here"", ""}""]
        }
    ]
}";

            Assert.AreEqual(0, Program.Main(new []{ "instrument", config }));

            Process.Start(testTargetExePath).WaitForExit();

//            Assert.AreEqual(0, Program.Main(new []{ "check" }));

            Assert.IsTrue(File.ReadLines(Program.RESULTS_FILENAME).Any());
        }

        [Test]
        public void OffsetExcludes()
        {
            string offsets;
            if (onDotNet)
                offsets = "14, 15, 16, 17";
            else
                offsets = "9, 10, 11, 12, 13";

            var config =
string.Format(@"{{
    ""assemblies"": [""TestTargetSut.exe""],
    ""typeInclude"": "".*TestTarget"",
    ""methodInclude"": "".*OffsetExcludes.*"",
    ""methodBodyExcludes"": [
        {{
            ""method"": ""System.Void Gaillard.SharpCover.Tests.TestTarget::OffsetExcludes()"",
            ""offsets"": [{0}]
        }}
    ]
}}", offsets);

            Assert.AreEqual(0, Program.Main(new []{ "instrument", config }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(0, Program.Main(new []{ "check" }));

            Assert.IsTrue(File.ReadLines(Program.RESULTS_FILENAME).Any());
        }

        //to get an IL instruction that uses a prefix instruction like constrained
        [Test]
        public void Constrained()
        {
            var config = @"{""assemblies"": [""TestTargetSut.exe""], ""typeInclude"": "".*Constrained""}";

            Assert.AreEqual(0, Program.Main(new []{ "instrument", config }));

            Process.Start(testTargetExePath).WaitForExit();

            Assert.AreEqual(0, Program.Main(new []{ "check" }));

            Assert.IsTrue(File.ReadLines(Program.RESULTS_FILENAME).Any());
        }

        [Test]
        public void MissingCommand()
        {
            Assert.AreEqual(2, Program.Main(new string[0]));
        }

        [Test]
        public void BadCommand()
        {
            Assert.AreEqual(2, Program.Main(new []{ "BAD_COMMAND" }));
        }

        [Test]
        public void MissingConfig()
        {
            Assert.AreEqual(2, Program.Main(new []{ "instrument" }));
        }
    }
}
