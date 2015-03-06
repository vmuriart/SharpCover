// // Copyright (c) 2015 Oliver Brown
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
using System;
using System.IO;
using System.Collections.Generic;
using System.Management;

namespace Gaillard.SharpCover
{
	public class CoberturaXmlProcessor
	{
		protected Dictionary<string, Dictionary<string, List<bool>>> Data = new Dictionary<string, Dictionary<string, List<bool>>>();

		private string _fileName;
		public CoberturaXmlProcessor(string fileName)
		{
			_fileName = fileName;
		}

		public void Process()
		{
			foreach (var line in File.ReadAllLines(_fileName))
			{
				var parts = line.Split('|');
				var hit = parts[0];
				var fullSignature = parts[1];
				var lineNum = parts[2];
				var instructionOffset = parts[3];
				var instruction = parts[4];

				parts = fullSignature.Split(' ');
				var returnType = parts[0];
				var fullMethodName = parts[1];

				parts = fullMethodName.Split(new string[] { "::" }, StringSplitOptions.None);
				var className = parts[0];
				var methodName = parts[1];

				Save(hit, className, methodName, lineNum);
			}

			int stop = 1;
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
				Data[className] = new Dictionary<string, List<bool>>();
			}
			var classData = Data[className];
			if (!classData.ContainsKey(methodName))
			{
				classData[methodName] = new List<bool>();
			}
			var methodData = classData[methodName];
			methodData.Add(hit);
		}
	}
}

