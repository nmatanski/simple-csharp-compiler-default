using System;
using System.Reflection.Emit;
using System.Text;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler.TableSymbols
{
	public class FormalParamSymbol: TableSymbol
	{
		public Type ParamType { get; set; }
		public ParameterBuilder ParameterInfo { get; set; }
		
		public FormalParamSymbol(IdentToken token, Type paramType, ParameterBuilder parameterInfo): base(token.line, token.column, token.value)
		{
			ParamType = paramType;
			ParameterInfo = parameterInfo;
		}
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3} formalparamtype={4}", line, column, value, GetType(), ParamType);
			return s.ToString();
		}
	}
}
