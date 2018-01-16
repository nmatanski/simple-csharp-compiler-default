using System;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler.TableSymbols
{
	public class PrimitiveTypeSymbol: TypeSymbol
	{
		public PrimitiveTypeSymbol(IdentToken token, Type type): base(token, type) {}
	}
}
