using System.Text;

namespace SimpleCSharpCompiler.Tokens
{
	public class OtherToken: Token
	{
		public string value;
		
		public OtherToken(int line, int column, string value): base(line, column) {
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