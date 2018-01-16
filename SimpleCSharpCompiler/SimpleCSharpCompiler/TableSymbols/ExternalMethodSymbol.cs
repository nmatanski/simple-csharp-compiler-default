using System.Reflection;
using System.Text;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler.TableSymbols
{
	public class ExternalMethodSymbol: TableSymbol
	{
		public MethodInfo[] MethodInfo { get; set; }
		
		public ExternalMethodSymbol(IdentToken token, MethodInfo[] methodInfo): base(token.line, token.column, token.value)
		{
			MethodInfo = methodInfo;
		}
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3} methodsignatures:", line, column, value, GetType());
			foreach (var mi in MethodInfo) {
				s.AppendFormat("\t{0} {1}(", mi.ReturnType.FullName, mi.Name);
				foreach (var pi in mi.GetParameters()) {
					s.AppendFormat("{0},", pi.ParameterType.FullName);
				}
				if (mi.GetParameters().Length != 0) s.Remove(s.Length-2, 2);
				s.Append(")\n");
			}
			s.Remove(s.Length-1, 1);
			return s.ToString();
		}
	}
}
