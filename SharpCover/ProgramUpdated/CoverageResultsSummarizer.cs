using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramUpdated
{
    public class CoverageResultsSummarizer
    {

        public List<ProcessedLine> MethodsNames = new List<ProcessedLine>();


        public void Fill(string path)
        {
            var allText = File.ReadAllText(path);
            CommonFill(allText);
        }

        private void CommonFill(string content)
        {
            MethodsNames.Clear();
            var breakInLine = content.Split('\n');
            foreach (var oneLine in breakInLine.Where(s=>!String.IsNullOrWhiteSpace(s)))
            {
                var pl = new ProcessedLine();
                var currentLine = oneLine.Replace("\r", "");
                currentLine = currentLine.Split(',')[0];
                if (oneLine.Contains("MISS !"))
                {
                    //We need To Process the miss.
                    currentLine = currentLine.Replace("MISS ! ", "");
                    pl.Missing = true;
                }
                else
                {
                    pl.Missing = false;
                }
                currentLine = currentLine.Replace("Method: ", "");
                pl.MethodSignature = currentLine;
                MethodsNames.Add(pl);
            }
        }


        public CoverageSummary GetResults()
        {
            if(MethodsNames.Count < 0)
                throw new AggregateException("Need to load the class first");
            CoverageSummary cs = new CoverageSummary();
            foreach (var methodSignature in MethodsNames.Select(s => s.MethodSignature).Distinct())
            {
                var misses = MethodsNames.Count(s => s.MethodSignature == methodSignature && s.Missing);
                var total = MethodsNames.Count();
                var nonmisses = total - misses;
                var coverage = (nonmisses/(double)total)*100;
                cs.ReportedMethods.Add(new CodeCoverageLine()
                {
                    Covered = coverage,
                    MethodSignature = methodSignature
                });
            }

            cs.Covered = ((MethodsNames.Count -MethodsNames.Count(s => s.Missing))/(double)MethodsNames.Count)*100;
            return cs;
        }

        public void Parse(string content)
        {
            CommonFill(content);
        }


    }
}
