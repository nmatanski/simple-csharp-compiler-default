using System.Reflection;
using System.Text;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler.TableSymbols
{
	public class FieldSymbol: TableSymbol 
	{
		public FieldInfo FieldInfo { get; set; }
		
		public FieldSymbol(IdentToken token, FieldInfo fieldInfo): base(token.line, token.column, token.value)
		{
			FieldInfo = fieldInfo;
		}
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3} fieldtype={4}", line, column, value, GetType(), FieldInfo.FieldType);
			return s.ToString();
		}
	}
}
