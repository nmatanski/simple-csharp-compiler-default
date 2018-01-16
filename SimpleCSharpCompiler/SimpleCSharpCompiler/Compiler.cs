using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleCSharpCompiler
{
	public static class Compiler
	{
		private static List<string> references = new List<string>();
		
		public static bool Compile(string file, string assemblyName)
		{
			return Compile(file, assemblyName, new DefaultDiagnostics());
		}
		
		public static bool Compile(string file, string assemblyName, Diagnostics diag)
		{
			TextReader reader = new StreamReader(file);
			var scanner = new Scanner(reader);
			var symbolTable = new Table(references);
			var emit = new Emit(assemblyName, symbolTable);
			var parser = new Parser(scanner, emit, symbolTable, diag);
			
			diag.BeginSourceFile(file);
			var isProgram = parser.Parse();
			diag.EndSourceFile();
			
			if (isProgram) {
				emit.WriteExecutable();
				return true;
			}
			else {
				return false;
			}
			
//			Scanner scanner = new Scanner(reader);
//			Token t = scanner.Next();
//			while (!(t is EOFToken)) {
//				Console.WriteLine(t.ToString());
//				t = scanner.Next();
//			}
		}
		
		public static void AddReferences(List<string> references)
		{
			Compiler.references.AddRange(references);
		}
	}
}
