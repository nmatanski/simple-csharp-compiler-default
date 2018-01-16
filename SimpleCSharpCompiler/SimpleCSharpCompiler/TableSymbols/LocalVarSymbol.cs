using System.Reflection;
using System.Text;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler.TableSymbols
{
	public class LocalVarSymbol: TableSymbol
	{
		public LocalVariableInfo LocalVariableInfo { get; set; }
		
		public LocalVarSymbol(IdentToken token, LocalVariableInfo localVariableInfo): base(token.line, token.column, token.value)
		{
			LocalVariableInfo = localVariableInfo;
		}
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3} localvartype={4} localindex={5}", line, column, value, GetType(), LocalVariableInfo.LocalType, LocalVariableInfo.LocalIndex);
			return s.ToString();
		}
	}
}
