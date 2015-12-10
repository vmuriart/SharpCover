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




        public void Fill(string path)
        {
            var allText = File.ReadAllText(path);
            CommonFill(allText);
        }

        private void CommonFill(string content)
        {
            var breakInLine = content.Split('\n');
            foreach (var oneLine in breakInLine.Where(s=>!String.IsNullOrWhiteSpace(s)))
            {
                if (oneLine.Contains("MISS !"))
                {
                    //We need To Process the miss.
                }
            }
        }

        public void Parse(string content)
        {
            CommonFill(content);
        }


    }
}
