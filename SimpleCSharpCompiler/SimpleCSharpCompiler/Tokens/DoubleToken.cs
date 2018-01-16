using System.Text;

namespace SimpleCSharpCompiler.Tokens
{
	public class DoubleToken: LiteralToken
	{
		public double value;
		
		public DoubleToken(int line, int column, double value): base(line, column) {
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
