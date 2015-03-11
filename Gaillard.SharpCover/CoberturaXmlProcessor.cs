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
		protected Dictionary<string, PackageData> Data = new Dictionary<string, PackageData>();
		protected LineAndBranchData LineAndBranchData = new LineAndBranchData();

		private string _fileName;
		public CoberturaXmlProcessor(string fileName)
		{
			_fileName = fileName;
		}

		public void Process()
		{
			var pwd = Directory.GetCurrentDirectory();
			foreach (var line in File.ReadAllLines(_fileName))
			{
				try
				{
					var parts = line.Split('|');
					var hit = parts[0];
					var assemblyName = parts[1];
					var fullSignature = parts[2];
					var fileName = parts[3];
					var lineNum = parts[4];
					//var instructionOffset = parts[5];
					//var instruction = parts[6];

					var count = 0;
					if (fileName != "[Unknown]")
					{
						foreach(char c in pwd)
						{
							if (fileName[count]!=c)
							{
								fileName = fileName.Substring(count, fileName.Length-count); 
							}
							count+=1;
						}
					}

					parts = fullSignature.Split(' ');
					//var returnType = parts[0];
					var fullMethodName = parts[1];

					parts = fullMethodName.Split(new string[] { "::" }, StringSplitOptions.None);
					var className = parts[0];
					var methodName = parts[1];

					Save(hit, assemblyName, className, methodName, fileName, lineNum);

				}
				catch (IndexOutOfRangeException)
				{
					System.Console.WriteLine("Problem reading line: {0}", line);
				}
			}
			Interpolate();
		}

		protected void Save(string hit, string packageName, string className, string methodName, string fileName, string lineNum)
		{
			var line = -1;
			int.TryParse(lineNum, out line);
			Save(hit == "HIT", packageName, className, methodName, fileName, line);
		}

		protected void Save(bool hit, string packageName, string className, string methodName, string fileName, int lineNum)
		{
			if (!Data.ContainsKey(packageName))
			{
				Data[packageName] = new PackageData();
			}
			var packageData = Data[packageName];
			if (!packageData.Classes.ContainsKey(className))
			{
				packageData.Classes[className] = new ClassData();
			}
			var classData = packageData.Classes[className];
			if (!classData.Methods.ContainsKey(methodName))
			{
				classData.Methods[methodName] = new MethodData();
			}
			var methodData = classData.Methods[methodName];
			if (!methodData.Files.ContainsKey(fileName))
			{
				methodData.Files[fileName] = new FileData();
			}
			var fileData = methodData.Files[fileName];
			if (!fileData.Lines.ContainsKey(lineNum))
			{
				fileData.Lines[lineNum] = new LineData();
			}
			var lineData = fileData.Lines[lineNum];
			lineData.Instructions.Add(new InstructionData() { Hit = hit });

			lineData.LineTotal++;
			fileData.LineTotal++;
			methodData.LineTotal++;
			classData.LineTotal++;
			packageData.LineTotal++;
			LineAndBranchData.LineTotal++;

			lineData.BranchTotal++;
			fileData.BranchTotal++;
			methodData.BranchTotal++;
			classData.BranchTotal++;
			packageData.BranchTotal++;
			LineAndBranchData.BranchTotal++;
			if (hit)
			{
				lineData.LineHit++;
				fileData.LineHit++;
				methodData.LineHit++;
				classData.LineHit++;
				packageData.LineHit++;
				LineAndBranchData.LineHit++;

				lineData.BranchHit++;
				fileData.BranchHit++;
				methodData.BranchHit++;
				classData.BranchHit++;
				packageData.BranchHit++;
				LineAndBranchData.BranchHit++;
			}
		}

		public void Interpolate()
		{
			foreach (var packageData in Data)
			{
				foreach (var classData in packageData.Value.Classes)
				{
					foreach (var methodData in classData.Value.Methods)
					{
						foreach (var fileData in methodData.Value.Files)
						{
							var lines = fileData.Value.Lines;
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

							for (int i = 0; i < reverseOrderedKeys.Count - 1; i++)
							{
								var thisLineNum = reverseOrderedKeys[i];
								var nextLineNum = reverseOrderedKeys[i + 1];
								var hit = lines[thisLineNum].LineHit > 0;
								for (int l = thisLineNum - 1; l > nextLineNum + 1; l--)
								{
									fileData.Value.LineTotal++;
									methodData.Value.LineTotal++;
									classData.Value.LineTotal++;
									packageData.Value.LineTotal++;
									LineAndBranchData.LineTotal++;
									if (hit)
									{

										fileData.Value.LineHit++;
										methodData.Value.LineHit++;
										classData.Value.LineHit++;
										packageData.Value.LineHit++;
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
			}
		}

		public void Output(string fileName)
		{
			var timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			var writer = new XmlTextWriter(fileName, Encoding.UTF8);
			writer.WriteStartDocument();
			//writer.WriteRaw("<!DOCTYPE coverage SYSTEM 'http://cobertura.sourceforge.net/xml/coverage-03.dtd'>");
			writer.WriteStartElement("coverage");
			writer.WriteAttributeString("branch-rate", LineAndBranchData.BranchRate());
			writer.WriteAttributeString("line-rate", LineAndBranchData.LineRate());
			writer.WriteAttributeString("timestamp", timestamp.ToString());
			writer.WriteAttributeString("version", "SharpCover");
			writer.WriteStartElement("sources");
			writer.WriteElementString("source", ".");
			writer.WriteEndElement();
			writer.WriteStartElement("packages");
			foreach (var packageData in Data)
			{
				writer.WriteStartElement("package");
				writer.WriteAttributeString("branch-rate", packageData.Value.BranchRate());
				writer.WriteAttributeString("complexity", "0");
				writer.WriteAttributeString("line-rate", packageData.Value.LineRate());
				writer.WriteAttributeString("name", packageData.Key);
				writer.WriteStartElement("classes");
				foreach (var classData in packageData.Value.Classes)
				{
					writer.WriteStartElement("class");
					var classFileName = "[Unknown]";
					foreach (var methodData in classData.Value.Methods)
					{
						foreach (var fileData in methodData.Value.Files)
						{
							if (fileData.Key != classFileName)
							{
								classFileName = fileData.Key;
								break;
							}
						}
						if (classFileName != "[Unknown]")
						{
							break;
						}
					}
					writer.WriteAttributeString("branch-rate", classData.Value.BranchRate());
					writer.WriteAttributeString("complexity", "0");
					writer.WriteAttributeString("filename", classFileName);
					writer.WriteAttributeString("line-rate", classData.Value.LineRate());
					writer.WriteAttributeString("name", classData.Key);
					foreach (var methodData in classData.Value.Methods)
					{
						writer.WriteStartElement("method");
						writer.WriteAttributeString("name", methodData.Key);
						writer.WriteAttributeString("signature", string.Empty);
						writer.WriteAttributeString("line-rate", methodData.Value.LineRate());
						writer.WriteAttributeString("branch-rate", methodData.Value.BranchRate());
						foreach (var fileData in methodData.Value.Files)
						{
							foreach (var lineData in fileData.Value.Lines.OrderBy(x => x.Key))
							{
								writer.WriteStartElement("line");
								writer.WriteAttributeString("number", lineData.Key.ToString());
								writer.WriteAttributeString("hits", lineData.Value.LineHit > 0 ? "1" : "0");
								if (lineData.Value.Instructions.Count > 1)
								{
									var conditionCoverage = string.Format("{0}% ({1}/{2})", (int)(100 * lineData.Value.LineHit / lineData.Value.LineTotal), lineData.Value.LineHit, lineData.Value.LineTotal);
									writer.WriteAttributeString("branch", "true");
									writer.WriteAttributeString("condition-coverage", conditionCoverage);
									writer.WriteStartElement("condition");
									writer.WriteAttributeString("coverage", string.Format("{0}%", (int)(100 * lineData.Value.LineHit / lineData.Value.LineTotal)));
									writer.WriteAttributeString("number", "0");
									writer.WriteAttributeString("type","jump");
									writer.WriteEndElement();							
								}
								else
								{
									writer.WriteAttributeString("branch", "false");
								}
								writer.WriteEndElement();
							}
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
				writer.WriteEndElement();
				writer.WriteEndElement();
			}
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

	public class PackageData : LineAndBranchData
	{
		public IDictionary<string, ClassData> Classes = new Dictionary<string, ClassData>();
	}

	public class ClassData : LineAndBranchData
	{
		public IDictionary<string, MethodData> Methods = new Dictionary<string, MethodData>();
	}

	public class MethodData : LineAndBranchData
	{
		public IDictionary<string, FileData> Files = new Dictionary<string, FileData>();
	}

	public class FileData : LineAndBranchData
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

