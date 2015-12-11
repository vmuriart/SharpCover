using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ProgramUpdated;

namespace SharpCover.Tests
{

    [TestFixture]
    public class CoverageResultsSumarizerTest
    {
        private CoverageResultsSummarizer _objectInTest;
        private const string Tstresult = "sampleResult.txt";
        [SetUp]
        public void Init()
        {
            _objectInTest = new CoverageResultsSummarizer();
            Assert.IsTrue(File.Exists(Tstresult));
        }

        [Test]
        public void LoadAResultFile()
        {
            _objectInTest.Fill(Tstresult);
             
            Assert.AreEqual(439,_objectInTest.MethodsNames.Count);
        }
    }
}