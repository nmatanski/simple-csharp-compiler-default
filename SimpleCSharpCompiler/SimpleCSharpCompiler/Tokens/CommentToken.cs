using System.Text;

namespace SimpleCSharpCompiler.Tokens
{
	public class CommentToken: Token
	{
		public CommentToken(int line, int column, string value, bool isInline): base(line, column) {
			this.value = value;
			this.isInline = isInline;
		}

		public string value;
		public bool isInline;
		
		public override string ToString()
		{
			var s = new StringBuilder();
			s.AppendFormat("line {0}, column {1}: {2} - {3}", line, column, value, GetType());
			return s.ToString();
		}	
	}
}
