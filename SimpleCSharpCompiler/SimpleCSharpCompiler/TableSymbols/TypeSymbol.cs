using System;
using System.Text;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler.TableSymbols
{
	public abstract class TypeSymbol: TableSymbol
	{
		public Type type;
		
		public TypeSymbol(IdentToken token, Type type): base(token.line, token.column, token.value)
		{
			this.type = type;
		}
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3} type={4}", line, column, value, GetType(), type.FullName);
			return s.ToString();
		}
	}
}
