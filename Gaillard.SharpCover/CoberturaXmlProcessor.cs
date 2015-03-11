// // Copyright (c) 2015 Oliver Brown
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Management;
using System.Linq;
using System.Xml;
using System.Text;
using System.Xml.Linq;

namespace Gaillard.SharpCover
{
	public class CoberturaXmlProcessor
	{
		protected Dictionary<string, ClassData> Data = new Dictionary<string, ClassData>();
		protected LineAndBranchData LineAndBranchData = new LineAndBranchData();

		private string _fileName;
		public CoberturaXmlProcessor(string fileName)
		{
			_fileName = fileName;
		}

		public void Process()
		{
			foreach (var line in File.ReadAllLines(_fileName))
			{
				try
				{
					var parts = line.Split('|');
					var hit = parts[0];
					var fullSignature = parts[1];
					var lineNum = parts[2];
					//var instructionOffset = parts[3];
					//var instruction = parts[4];

					parts = fullSignature.Split(' ');
					//var returnType = parts[0];
					var fullMethodName = parts[1];

					parts = fullMethodName.Split(new string[] { "::" }, StringSplitOptions.None);
					var className = parts[0];
					var methodName = parts[1];

					Save(hit, className, methodName, lineNum);

				}
				catch (IndexOutOfRangeException)
				{
					System.Console.WriteLine("Problem reading line: {0}", line);
				}
			}
			Interpolate();
		}

		protected void Save(string hit, string className, string methodName, string lineNum)
		{
			var line = -1;
			int.TryParse(lineNum, out line);
			Save(hit == "HIT", className, methodName, line);
		}

		protected void Save(bool hit, string className, string methodName, int lineNum)
		{
			if (!Data.ContainsKey(className))
			{
				Data[className] = new ClassData();
			}
			var classData = Data[className];
			if (!classData.Methods.ContainsKey(methodName))
			{
				classData.Methods[methodName] = new MethodData();
			}
			var methodData = classData.Methods[methodName];
			if (!methodData.Lines.ContainsKey(lineNum))
			{
				methodData.Lines[lineNum] = new LineData();
			}
			var lineData = methodData.Lines[lineNum];
			lineData.Instructions.Add(new InstructionData() { Hit = hit });

			lineData.LineTotal++;
			methodData.LineTotal++;
			classData.LineTotal++;
			LineAndBranchData.LineTotal++;
			lineData.BranchTotal++;
			methodData.BranchTotal++;
			classData.BranchTotal++;
			LineAndBranchData.BranchTotal++;
			if (hit)
			{
				lineData.LineHit++;
				methodData.LineHit++;
				classData.LineHit++;
				LineAndBranchData.LineHit++;
				lineData.BranchHit++;
				methodData.BranchHit++;
				classData.BranchHit++;
				LineAndBranchData.BranchHit++;
			}
		}

		public void Interpolate()
		{
			var c = 0;

			foreach (var classData in Data)
			{
				c++;
				foreach (var methodData in classData.Value.Methods)
				{
					var lines = methodData.Value.Lines;
					var keys = lines.Keys.Where(x => x != -1);
					if (keys.Count() < 1)
					{
						continue;
					}

					var reverseOrderedKeys = keys.OrderByDescending(x => x).ToList();
					if (reverseOrderedKeys[0] - reverseOrderedKeys[reverseOrderedKeys.Count - 1] == reverseOrderedKeys.Count - 1)
					{
						continue;
					}

					for (int i=0; i<reverseOrderedKeys.Count-1; i++)
					{
						var thisLineNum = reverseOrderedKeys[i];
						var nextLineNum = reverseOrderedKeys[i + 1];
						var hit = lines[thisLineNum].LineHit > 0;
						for (int l = thisLineNum - 1; l > nextLineNum + 1; l--)
						{
							methodData.Value.LineTotal++;
							classData.Value.LineTotal++;
							LineAndBranchData.LineTotal++;
							if (hit)
							{

								methodData.Value.LineHit++;
								classData.Value.LineHit++;
								LineAndBranchData.LineHit++;
								lines.Add(l, LineData.HitLineData);
							}
							else
							{
								lines.Add(l, LineData.MissLineData);
							}
						}
					}
				}
			}
		}

		public void Output(string fileName)
		{
			var writer = new XmlTextWriter(fileName, Encoding.UTF8);
			writer.WriteStartDocument();
			//writer.WriteRaw("<!DOCTYPE coverage SYSTEM 'http://cobertura.sourceforge.net/xml/coverage-03.dtd'>");
			writer.WriteStartElement("coverage");
			writer.WriteAttributeString("line-rate", LineAndBranchData.LineRate());
			writer.WriteAttributeString("branch-rate", LineAndBranchData.BranchRate());
			writer.WriteAttributeString("version", "SharpCover");
			writer.WriteAttributeString("timestamp", "0");
			writer.WriteStartElement("sources");
			writer.WriteElementString("source", ".");
			writer.WriteEndElement();
			writer.WriteStartElement("packages");
			writer.WriteStartElement("package");
			writer.WriteAttributeString("name", "[Unknown]");
			writer.WriteAttributeString("line-rate", LineAndBranchData.LineRate());
			writer.WriteAttributeString("branch-rate", LineAndBranchData.BranchRate());
			writer.WriteAttributeString("complexity", "0");
			writer.WriteStartElement("classes");
			foreach (var classData in Data)
			{
				writer.WriteStartElement("class");
				writer.WriteAttributeString("name", classData.Key);
				writer.WriteAttributeString("filename", classData.Key);
				writer.WriteAttributeString("line-rate", classData.Value.LineRate());
				writer.WriteAttributeString("branch-rate", classData.Value.BranchRate());
				writer.WriteAttributeString("complexity", "0");
				foreach (var methodData in classData.Value.Methods)
				{
					writer.WriteStartElement("method");
					writer.WriteAttributeString("name", methodData.Key);
					writer.WriteAttributeString("signature", string.Empty);
					writer.WriteAttributeString("line-rate", methodData.Value.LineRate());
					writer.WriteAttributeString("branch-rate", methodData.Value.BranchRate());
					foreach (var lineData in methodData.Value.Lines.OrderBy(x => x.Key))
					{
						writer.WriteStartElement("line");
						writer.WriteAttributeString("number", lineData.Key.ToString());
						writer.WriteAttributeString("hits", lineData.Value.LineHit > 0 ? "1" : "0");
						if (lineData.Value.Instructions.Count > 1)
						{
							var conditionCoverage = string.Format("{0}% ({1}/{2})", (int)(100 * lineData.Value.LineHit / lineData.Value.LineTotal), lineData.Value.LineHit, lineData.Value.LineTotal);
							writer.WriteAttributeString("branch", "true");
							writer.WriteAttributeString("condition-coverage", conditionCoverage);
						}
						else
						{
							writer.WriteAttributeString("branch", "false");
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.WriteEndElement();
			writer.Close();

			var doc = XDocument.Load(fileName);
			var formatted = doc.ToString();
			File.WriteAllText(fileName, formatted);
		}
	}

	public class LineAndBranchData : ILineRate, IBranchRate
	{
		public int LineTotal { get; set; }
		public int LineHit { get; set; }
		public int BranchTotal { get; set; }
		public int BranchHit { get; set; }
	}

	public class ClassData : LineAndBranchData
	{
		public IDictionary<string, MethodData> Methods = new Dictionary<string, MethodData>();
	}

	public class MethodData : LineAndBranchData
	{
		public IDictionary<int, LineData> Lines = new Dictionary<int, LineData>();
	}

	public class LineData : LineAndBranchData
	{
		public List<InstructionData> Instructions = new List<InstructionData>();

		public static LineData HitLineData = new LineData()
		{
			LineTotal = 1,
			LineHit = 1,
			Instructions = InstructionData.HitInstructionDataList
		};
		public static LineData MissLineData = new LineData()
		{
			LineTotal = 1,
			LineHit = 0,
			Instructions = InstructionData.MissInstructionDataList
		};
	}

	public class InstructionData
	{
		public bool Hit;

		public static InstructionData HitInstructionData = new InstructionData() { Hit = true };
		public static InstructionData MissInstructionData = new InstructionData() { Hit = false };
		public static List<InstructionData> HitInstructionDataList;
		public static List<InstructionData> MissInstructionDataList;

		static InstructionData()
		{
			HitInstructionDataList = new List<InstructionData>() { InstructionData.HitInstructionData };
			MissInstructionDataList = new List<InstructionData>() { InstructionData.MissInstructionData };		
		}
	}

	public interface ILineRate
	{
		int LineTotal { get; }
		int LineHit { get; }
	}

	public interface IBranchRate
	{
		int BranchTotal { get; }
		int BranchHit { get; }
	}

	public static class RateExtensions
	{
		public static string LineRate(this ILineRate self)
		{
			if (self.LineTotal == 0)
			{
				return "0";
			}
			var rate = (float)self.LineHit / self.LineTotal;
			return rate.ToString();
		}

		public static string BranchRate(this IBranchRate self)
		{
			if (self.BranchTotal == 0)
			{
				return "0";
			}
			var rate = (float)self.BranchHit / self.BranchTotal;
			return rate.ToString();
		}
	}

}

