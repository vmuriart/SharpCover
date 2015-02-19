// Copyright (c) 2013 Dominion Enterprises, 2015 Oliver Brown
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void TestSetup()
        {
            onDotNet = Type.GetType("Mono.Runtime") == null;
			testTargetExePath = Path.Combine("..", "..", "..", "Gaillard.SharpCover.Tests.TestTarget", "bin", "Debug", "Gaillard.SharpCover.Tests.TestTarget.exe");
			string buildCommand;
			if (onDotNet) {
                buildCommand = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
            } else {
                buildCommand = "xbuild";
            }
			string testTargetProjectPath = Path.Combine("..", "..", "..", "Gaillard.SharpCover.Tests.TestTarget", "Gaillard.SharpCover.Tests.TestTarget.csproj");

			var process = new Process();
			//process.StartInfo.FileName = "pwd";
			process.StartInfo.FileName = buildCommand;
			process.StartInfo.Arguments = testTargetProjectPath;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			var error = process.StandardError.ReadToEnd();
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			Assert.IsEmpty(error);
			Assert.IsTrue(!output.Contains("error"));
            Assert.AreEqual(0, process.ExitCode);
        }

        [Test]
        public void NoBody()
        {
            var config =
				@"{""assemblies"": [""../../../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""], ""typeInclude"": "".*Tests.*Event.*""}";

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
                @"{""assemblies"": [""bin/Debug/TestTarget.exe""], ""typeInclude"": "".*TestTarget"", ""methodInclude"": "".*Covered.*""}";

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
			var config = @"{""assemblies"": [""../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""], ""methodInclude"": "".*UncoveredIf.*""}";

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
			var config = @"{""assemblies"": [""../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""], ""methodInclude"": "".*UncoveredLeave.*""}";

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
			var config = @"{""assemblies"": [""../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""], ""typeInclude"": "".*Nested""}";

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
    ""assemblies"": [""../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""],
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

            Assert.AreEqual(0, Program.Main(new []{ "check" }));

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
    ""assemblies"": [""../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""],
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
			var config = @"{""assemblies"": [""../Gaillard.SharpCover.Tests.TestTarget/bin/Debug/TestTarget.exe""], ""typeInclude"": "".*Constrained""}";

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
