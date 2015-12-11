using System.Collections.Generic;

namespace ProgramUpdated
{
    public class CoverageSummary
    {
        public List<CodeCoverageLine> ReportedMethods  = new List<CodeCoverageLine>();

        public double Covered { get; set; }
    }

    public class CodeCoverageLine
    {
        public string MethodSignature { get; set; }

        public double Covered { get; set; }
    }
}