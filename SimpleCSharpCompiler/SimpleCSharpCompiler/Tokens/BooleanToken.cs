using System.Text;

namespace SimpleCSharpCompiler.Tokens
{
	public class BooleanToken: LiteralToken
	{
		public bool value;
		
		public BooleanToken(int line, int column, bool value): base(line, column) {
			this.value = value;
		}
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3}", line, column, value, GetType());
			return s.ToString();
		}
	}
}
