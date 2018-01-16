using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using SimpleCSharpCompiler.TableSymbols;
using SimpleCSharpCompiler.Tokens;
using StringToken = SimpleCSharpCompiler.Tokens.StringToken;

namespace SimpleCSharpCompiler
{
	public class Parser
	{
		private Scanner scanner;
		private Emit emit;
		private Table symbolTable;
		private Token token;
		private Diagnostics diag;
		
		private Stack<Label> breakStack = new Stack<Label>();
		private Stack<Label> continueStack = new Stack<Label>();

		
		public Parser(Scanner scanner, Emit emit, Table symbolTable, Diagnostics diag)
		{
			this.scanner = scanner;
			this.emit = emit;
			this.symbolTable = symbolTable;
			this.diag = diag;
		}
		
		public void AddPredefinedSymbols()
		{
			symbolTable.AddToUniverse(new PrimitiveTypeSymbol(new IdentToken(-1,-1, "int"), typeof(Int32)));
			symbolTable.AddToUniverse(new PrimitiveTypeSymbol(new IdentToken(-1,-1, "bool"), typeof(Boolean)));
			symbolTable.AddToUniverse(new PrimitiveTypeSymbol(new IdentToken(-1,-1, "double"), typeof(Double)));
			symbolTable.AddToUniverse(new PrimitiveTypeSymbol(new IdentToken(-1,-1, "char"), typeof(Char)));
			symbolTable.AddToUniverse(new PrimitiveTypeSymbol(new IdentToken(-1,-1, "string"), typeof(String)));
		}
		
		public bool Parse()
		{
			ReadNextToken();
			AddPredefinedSymbols();
			return IsProgram() && token is EOFToken;
		}
		
		public void ReadNextToken()
		{
			token = scanner.Next();
		}
		
		public bool CheckKeyword(string keyword)
		{
			var result = (token is KeywordToken) && ((KeywordToken)token).value==keyword;
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckSpecialSymbol(string symbol)
		{
			var result = (token is SpecialSymbolToken) && ((SpecialSymbolToken)token).value==symbol;
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckIdent()
		{
			var result = (token is IdentToken);
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckNumber()
		{
			var result = (token is NumberToken);
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckDouble()
		{
			var result = (token is DoubleToken);
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckBoolean()
		{
			var result = (token is BooleanToken);
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckChar()
		{
			var result = (token is CharToken);
			if (result) ReadNextToken();
			return result;
		}
		
		public bool CheckString()
		{
			var result = (token is StringToken);
			if (result) ReadNextToken();
			return result;
		}
		
		void SkipUntilSemiColon() {
			Token Tok;
			do {
				Tok = scanner.Next();
			} while (!((Tok is EOFToken) ||
				  	   (Tok is SpecialSymbolToken) && ((Tok as SpecialSymbolToken).value == ";")));
		}
		
		public void Error(string message)
		{
			diag.Error(token.line, token.column, message);
			SkipUntilSemiColon();
		}
		
		public void Error(string message, Token token)
		{
			diag.Error(token.line, token.column, message);
			SkipUntilSemiColon();
		}
		
		public void Error(string message, Token token, params object[] par)
		{
			diag.Error(token.line, token.column, string.Format(message, par));
			SkipUntilSemiColon();
		}
		
		public void Warning(string message)
		{
			diag.Warning(token.line, token.column, message);
		}
		
		public void Warning(string message, Token token)
		{
			diag.Warning(token.line, token.column, message);
		}
		
		public void Warning(string message, Token token, params object[] par)
		{
			diag.Warning(token.line, token.column, string.Format(message, par));
		}
		
		public void Note(string message)
		{
			diag.Note(token.line, token.column, message);
		}
		
		public void Note(string message, Token token)
		{
			diag.Note(token.line, token.column, message);
		}
		
		public void Note(string message, Token token, params object[] par)
		{
			diag.Note(token.line, token.column, string.Format(message, par));
		}
		
		// [1] Program = {UsingClause} 'class' Ident '{' {FieldDecl | MethodDecl} '}'.
		public bool IsProgram()
		{
			while (IsUsingClause()) ;
			if (!CheckKeyword("class")) Error("Очаквам ключова дума 'class'");
			var id = token as IdentToken;
			if (!CheckIdent()) Error("Очаквам идентификатор");
			symbolTable.AddToUniverse(new PrimitiveTypeSymbol(id, emit.InitProgramClass(id.value)));
			if (!CheckSpecialSymbol("{")) Error("Очаквам специален символ '{'");
			while (IsFieldDeclOrMethodDecl()) ;
			if (!CheckSpecialSymbol("}")) Error("Очаквам специален символ '}'");
			
			Debug.WriteLine(symbolTable.ToString());
			
			return diag.GetErrorCount() == 0;
		}
		
		// [2] UsingClause = 'using' Ident ';'.
		public bool IsUsingClause()
		{
			IdentToken usingNamespace;
			
			if (!CheckKeyword("using")) return false;
			usingNamespace = token as IdentToken; // Запомня идентификатора
			if (!CheckIdent()) Error("Очаквам идентификатор");
			if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
			
			// Emit
			symbolTable.AddUsingNamespace(usingNamespace.value);
			
			return true;
		}
		
//		// [?]  Namespace = Ident {'.' Ident}.
//		public bool IsNamespace(out string ns)
//		{
//			ns = "";
//			
//			IdentToken id = token as IdentToken;
//			if (!CheckIdent()) return false;
//			
//			StringBuilder sb = new StringBuilder(id.value);
//			while (CheckSpecialSymbol(".")) {
//				sb.Append(".");
//				id = token as IdentToken;
//				if (!CheckIdent()) Error("Очаквам идентификатор");
//				sb.Append(id.value);
//			}
//			ns = sb.ToString();
//			
//			return true;
//		}
		
		// [3] FieldDecl = Type (Ident | Ident '[' Number ']' ) ';'.
		// [4] MethodDecl = (Type | 'void') Ident '(' [Type Ident {',' Type Ident}] ')' Block.
		public bool IsFieldDeclOrMethodDecl()
		{
			IdentToken name;
			IdentToken paramName;
			var formalParams = new List<FormalParamSymbol>();
			var formalParamTypes = new List<Type>();
			Type paramType;
			long arraySize = 0;
			Type type;
			
			if (!IsType(out type)) return false;
			name = token as IdentToken;
			if (!CheckIdent()) Error("Очаквам идентификатор");
			if (CheckSpecialSymbol("[")) {
				arraySize = ((NumberToken)token).value;
				if (!CheckNumber()) Error("Очаквам цяло число");
				if (!CheckSpecialSymbol("]")) Error("Очаквам специален символ ']'");
				
				type = type.MakeArrayType();
			} else if (CheckSpecialSymbol("(")) {
				// Семантична грешка - редеклариран ли е методът повторно?
				if (symbolTable.ExistCurrentScopeSymbol(name.value)) Error("Метода {0} е редеклариран", name, name.value);
				// Emit
				var methodToken = symbolTable.AddMethod(name, type, formalParams.ToArray(), null);
				symbolTable.BeginScope();
				                                                
				while (IsType(out paramType)) {
					paramName = token as IdentToken;
					if (!CheckIdent()) Error("Очаквам идентификатор");
					// Семантична грешка - редеклариран ли е формалният параметър повторно?
					if (symbolTable.ExistCurrentScopeSymbol(paramName.value)) Error("Формалния параметър {0} е редеклариран", paramName, paramName.value);
					var formalParam = symbolTable.AddFormalParam(paramName, paramType, null);
					formalParams.Add(formalParam);
					formalParamTypes.Add(paramType);
					if (!CheckSpecialSymbol(",")) break;
				}
				if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')'");
				
				methodToken.methodInfo = emit.AddMethod(name.value, type, formalParamTypes.ToArray());
				for (var i=0; i<formalParams.Count; i++) {
					formalParams[i].ParameterInfo = emit.AddParam(formalParams[i].value, i+1, formalParamTypes[i]);
				}
				methodToken.formalParams = formalParams.ToArray();
				
				if (!IsBlock(false)) Error("Очаквам блок");
				
				symbolTable.EndScope();
				
				return true;
			}
			
			if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
			
			// Семантична грешка - редекларирано ли е полето повторно?
			if (symbolTable.ExistCurrentScopeSymbol(name.value)) Error("Полето {0} е редекларирано", name, name.value);
			if (type==typeof(void)) Error("Полето {0} не може да е от тип void", name, name.value);
			
			// Emit (field)
			symbolTable.AddField(name, emit.AddField(name.value, type, arraySize));
			
			return true;
		}
		
		// [5] Block = '{' {VarDecl | Statement} '}'.
		public bool IsBlock(bool newScope)
		{
			if (!CheckSpecialSymbol("{")) return false;
			
			// Emit
			if (newScope) {
				symbolTable.BeginScope();
				emit.BeginScope();
			}
			
			while (IsVarDecl() || IsStatement()) ;

			// Emit
			if (newScope) {
				emit.EndScope();
				symbolTable.EndScope();
			}
			
			if (!CheckSpecialSymbol("}")) Error("Очаквам специален символ '}'");
			
			return true;
		}
		
		// [6] VarDecl = Type Ident ';'.
		public bool IsVarDecl()
		{
			Type type;
			IdentToken name;
			
			if (!IsType(out type)) return false;
			name = token as IdentToken;
			if (!CheckIdent()) Error("Очаквам идентификатор");
			if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
			
			// Семантична грешка - редекларирана ли е локалната променлива повторно?
			if (symbolTable.ExistCurrentScopeSymbol(name.value)) Error("Локалната променлива {0} е редекларирана", name, name.value);
			// Emit
			symbolTable.AddLocalVar(name, emit.AddLocalVar(name.value, type));
			
			return true;
		}
		
		// [7] Type = 'int' | 'bool' | 'double' | 'char' | 'string' | 'void' | 'object' | Ident.
		public bool IsType(out Type type)
		{
//			if (CheckKeyword("int")) {
//				type = typeof(System.Int32);
//				return true;
//			}
//			if (CheckKeyword("bool")) {
//				type = typeof(System.Boolean);
//				return true;
//			}
//			if (CheckKeyword("double")) {
//				type = typeof(System.Double);
//				return true;
//			}
//			if (CheckKeyword("char")) {
//				type = typeof(System.Char);
//				return true;
//			}
//			if (CheckKeyword("string")) {
//				type = typeof(System.String);
//				return true;
//			}
			if (CheckKeyword("object")) {
				type = typeof(Object);
				return true;
			}
			if (CheckKeyword("void")) {
				type = typeof(void);
				return true;
			}
			var typeIdent = token as IdentToken;
			if (typeIdent != null) {
				var ts = symbolTable.GetSymbol(typeIdent.value) as TypeSymbol;
				if (ts != null)
					type = ts.type;
				else
					type = symbolTable.ResolveExternalType(typeIdent.value);
				
				if (type != null) {
					ReadNextToken();
					return true;
				}
			}
			
			type = null;
			return false;
		}
		
		// [8] Statement = [Location '='] Expression ';' |
		//		'if' '(' Expression ')' Statement ['else' Statement] |
		//		'while' '(' Expression ')' Statement |
		//		'return' [Expression] ';' |
		//		'break' ';' |
		//		'continue' ';' |
		//		Block.
		public bool IsStatement()
		{
			Type type;
			LocationInfo location;
				
			if (IsLocation(out location)) {
				var fs = location.id as FieldSymbol;
				if (fs != null) {
					if (CheckSpecialSymbol("=")) {
						if (!IsExpression(null, out type)) Error("Очаквам израз");
						if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
						
						// Emit
						if (location.isArray) {
							if (!AssignableTypes(fs.FieldInfo.FieldType.GetElementType(), type)) Error("Несъвместими типове", location.id);
							emit.AddAssignCast(fs.FieldInfo.FieldType.GetElementType(), type);
							emit.AddArrayAssigment(fs.FieldInfo);
						} else {
							if (!AssignableTypes(fs.FieldInfo.FieldType, type)) Error("Несъвместими типове", location.id);
							emit.AddAssignCast(fs.FieldInfo.FieldType, type);
							emit.AddFieldAssigment(fs.FieldInfo);
						}
						
						return true;
					}
				}
				
				var lvs = location.id as LocalVarSymbol;
				if (lvs != null) {
					if (CheckSpecialSymbol("=")) {
						if (!IsExpression(null, out type)) Error("Очаквам израз");
						if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
						
						// Emit
						if (!AssignableTypes(lvs.LocalVariableInfo.LocalType, type)) Error("Несъвместими типове", location.id);
						emit.AddAssignCast(lvs.LocalVariableInfo.LocalType, type);
						emit.AddLocalVarAssigment(lvs.LocalVariableInfo);
						
						return true;
					}
				}
				
				var fps = location.id as FormalParamSymbol;
				if (fps != null) {
					if (CheckSpecialSymbol("=")) {
						if (!IsExpression(null, out type)) Error("Очаквам израз");
						if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
						
						// Emit
						if (!AssignableTypes(fps.ParamType, type)) Error("Несъвместими типове", location.id);
						emit.AddAssignCast(fps.ParamType, type);
						emit.AddParameterAssigment(fps.ParameterInfo);
						
						return true;
					}
				}
				
				if (!IsExpression(location, out type)) Error("Неочакван вид идентификатор", location.id);
				if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");

				// Emit
				if (type!=typeof(void)) emit.AddPop();
				
			} else if (IsExpression(null, out type)) {
				if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
				
				// Emit
				if (type!=typeof(void)) emit.AddPop();
				
			} else if (CheckKeyword("if")) {
				// 'if' '(' Expression ')' Statement ['else' Statement]
				if (!CheckSpecialSymbol("(")) Error("Очаквам специален символ '('");
				if (!IsExpression(null, out type)) Error("Очаквам израз");
				if (!AssignableTypes(typeof(Boolean), type)) Error("Типа на изразът трябва да е Boolean");
				if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')'");
				
				// Emit
				var labelElse = emit.GetLabel();
				emit.AddCondBranch(labelElse);
				
				if (!IsStatement()) Error("Очаквам Statement");
				if (CheckKeyword("else")) {
					// Emit
					var labelEnd = emit.GetLabel();
					emit.AddBranch(labelEnd);
					emit.MarkLabel(labelElse);
					
					if (!IsStatement()) Error("Очаквам Statement");
					
					// Emit
					emit.MarkLabel(labelEnd);
				} else {
					// Emit
					emit.MarkLabel(labelElse);
				}
				
			} else if (CheckKeyword("while")) {
				// 'while' '(' Expression ')' Statement
				
				// Emit
				var labelContinue = emit.GetLabel();
				var labelBreak = emit.GetLabel();
				breakStack.Push(labelBreak);
				continueStack.Push(labelContinue);
				
				emit.MarkLabel(labelContinue);
				
				if (!CheckSpecialSymbol("(")) Error("Очаквам специален символ '('");
				if (!IsExpression(null, out type)) Error("Очаквам израз");
				if (!AssignableTypes(typeof(Boolean), type)) Error("Типа на изразът трябва да е Boolean");
				if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')'");
				
				// Emit
				emit.AddCondBranch(labelBreak);
				
				if (!IsStatement()) Error("Очаквам Statement");
				
				// Emit
				emit.AddBranch(labelContinue);
				emit.MarkLabel(labelBreak);
				
				breakStack.Pop();
				continueStack.Pop();
				
			} else if (CheckKeyword("return")) {
				var retType = emit.GetMethodReturnType();
				if (retType!=typeof(void)) {
					IsExpression(null, out type);
					if (!AssignableTypes(retType, type)) Error("Типа на резултата трябва да е съвместим с типа на метода");
				}
				if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
				
				// Emit
				emit.AddReturn();
				
			} else if (CheckKeyword("break")) {
				if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
				
				// Emit
				emit.AddBranch((Label)breakStack.Peek());
				
			} else if (CheckKeyword("continue")) {
				if (!CheckSpecialSymbol(";")) Error("Очаквам специален символ ';'");
				
				// Emit
				emit.AddBranch((Label)continueStack.Peek());
				
			} else if (IsBlock(true)) {
				// 
			} else {
				return false;
			}
			
			return true;
		}
		
		// [9] Location = Ident | Ident '[' Expression ']'.
		public bool IsLocation(out LocationInfo location)
		{
			var id = token as IdentToken;
			Type type;
			
			if (!CheckIdent()) {
				location = null;
				return false;
			}
			location = new LocationInfo();
			location.id = symbolTable.GetSymbol(id.value);
			// Семантична грешка - деклариран ли е вече идентификатора?
			if (location.id==null) Error("Недеклариран идентификатор {0}", id, id.value);
			
			var fs = location.id as FieldSymbol;
			if (fs != null) {
				if (CheckSpecialSymbol("[")) {
					// Emit
					emit.AddLoadArray(fs.FieldInfo);
					
					if (!IsExpression(null, out type)) Error("Очаквам израз");
					if (!CheckSpecialSymbol("]")) Error("Очаквам специален символ ']'");
					if (!(type==typeof(Int32))) Error("Типа на индекса трябва да е Integer");
					location.isArray = true;
				}
			}
			
			return true;
		}

		// [10] Expression = AdditiveExpr [(('<' | '<=' | '==' | '!=' | '>=' | '>') AdditiveExpr) | (('as' | 'is') Type)].
		public bool IsExpression(LocationInfo location, out Type type)
		{
			if (!IsAdditiveExpr(location, out type)) return false;
			var opToken = token as SpecialSymbolToken;
			if (CheckSpecialSymbol("<") || CheckSpecialSymbol("<=") || CheckSpecialSymbol("==") || CheckSpecialSymbol("!=") || CheckSpecialSymbol(">=") || CheckSpecialSymbol(">")) {
				Type type1;
				if (!IsAdditiveExpr(null, out type1)) Error("Очаквам адитивен израз");
				if (type!=type1) Error("Несъвместими типове за сравнение");
				
				//Emit
				emit.AddConditionOp(opToken.value);
				
				type = typeof(Boolean);
				
			} else if (CheckKeyword("as")) {
				Type type1;
				if (!IsType(out type1)) Error("Очаквам тип");
				
				if (type1.IsValueType) Error("Операторът 'as' не може да се ползва със този тип");
					
				//Emit
				emit.AddAsOp(type, type1);
				
				type = type1;
				
			} else if (CheckKeyword("is")) {
				Type type1;
				if (!IsType(out type1)) Error("Очаквам тип");
				
				//Emit
				emit.AddIsOp(type1);
				
				type = typeof(Boolean);
			}
			
			return true;
		}
		
		// [11] AdditiveExpr = ['+' | '-'] MultiplicativeExpr {('+' | '-' | '|' | '||' | '^') MultiplicativeExpr}.
		public bool IsAdditiveExpr(LocationInfo location, out Type type)
		{
			var opToken = token as SpecialSymbolToken;
			var unaryMinus = false;
			var unaryOp = false;
			
			if (location==null) {
				if (CheckSpecialSymbol("+") || CheckSpecialSymbol("-")) {
					unaryMinus = ((SpecialSymbolToken)token).value == "-";
					unaryOp = true;
				}
			}
			if (!IsMultiplicativeExpr(location, out type)) {
				if (unaryOp) Error("Очаквам мултипликативен израз");
				else return false;
			}
			
			// Emit
			if (unaryMinus) {
				emit.AddUnaryOp("-");
			}
				
			opToken = token as SpecialSymbolToken;
			while (CheckSpecialSymbol("+") || CheckSpecialSymbol("-") || CheckSpecialSymbol("|") || CheckSpecialSymbol("||") || CheckSpecialSymbol("^")) {
				Type type1;
				if (!IsMultiplicativeExpr(null, out type1)) Error("Очаквам мултипликативен израз");

				// Types check
				if (opToken.value == "||") {
					if (type==typeof(Boolean) && type1==typeof(Boolean)) {
						;
					} else {
						Error("Несъвместими типове", opToken);
					}
				} else {
					if (type==typeof(Int32) && type1==typeof(Int32)) {
						;
					} else if (type==typeof(Int32) && type1==typeof(Double)) {
						type = typeof(Double);
						Warning("Трябва да използвате явно конвертиране на първия аргумент до double", opToken);
					} else if (type==typeof(Double) && type1==typeof(Int32)) {
						type = typeof(Double);
						Warning("Трябва да използвате явно конвертиране на втория аргумент до double", opToken);
					} else if (type==typeof(Double) && type1==typeof(Double)) {
						;
					} else if (type==typeof(String) || type1==typeof(String)) {
						type = typeof(String);
					} else {
						Error("Несъвместими типове");
					}
				}
				
				//Emit
				if (opToken.value=="+" && type==typeof(String)) {
					emit.AddConcatinationOp();
				} else {
					emit.AddAdditiveOp(opToken.value);
				}
				
				opToken = token as SpecialSymbolToken;
			}
			
			return true;
		}
		
		// [12] MultiplicativeExpr = SimpleExpr {('*' | '/' | '%' | '&' | '&&') SimpleExpr}.
		public bool IsMultiplicativeExpr(LocationInfo location, out Type type)
		{
			if (!IsSimpleExpr(location, out type)) return false;
			
			var opToken = token as SpecialSymbolToken;
			while (CheckSpecialSymbol("*") || CheckSpecialSymbol("/") || CheckSpecialSymbol("%") || CheckSpecialSymbol("&") || CheckSpecialSymbol("&&")) {
				Type type1;
				if (!IsSimpleExpr(null, out type1)) Error("Очаквам прост израз");
				
				// Types check
				if (opToken.value == "&&") {
					if (type==typeof(Boolean) && type1==typeof(Boolean)) {
						;
					} else {
						Error("Несъвместими типове");
					}
				} else {
					if (type==typeof(Int32) && type1==typeof(Int32)) {
						;
					} else if (type==typeof(Int32) && type1==typeof(Double)) {
						type = typeof(Double);
						Warning("Трябва да използвате явно конвертиране на първия аргумент до double", opToken);
					} else if (type==typeof(Double) && type1==typeof(Int32)) {
						type = typeof(Double);
						Warning("Трябва да използвате явно конвертиране на втория аргумент до double", opToken);
					} else if (type==typeof(Double) && type1==typeof(Double)) {
						;
					} else {
						Error("Несъвместими типове");
					}
				}
				
				//Emit
				emit.AddMultiplicativeOp(opToken.value);
				
				opToken = token as SpecialSymbolToken;
			}
			
			return true;
		}
		
		// [13] SimpleExpr = Location |
		// 	   MethodCall |
		// 	   Literal |
		// 	   '++' Location | 
		// 	   '--' Location | 
		// 	   Location '++' | 
		// 	   Location '--' | 
		// 	   '~' SimpleExpr | 
		// 	   '-' SimpleExpr |
		// 	   '!' SimpleExpr |
		// 	   '(' Expression ')' |
		// 	   '(' Type ')' SimpleExpr .
		// [14] MethodCall = Ident '(' [Expression {',' Expression}] ')'.
		public enum IncDecOps {None, PreInc, PreDec, PostInc, PostDec}
		public bool IsSimpleExpr(LocationInfo location, out Type type)
		{
			Type type1;
			SpecialSymbolToken opToken;
			
			var incDecOp = IncDecOps.None;
			
			if (location!=null) {
				opToken = null;
			} else {
				opToken = token as SpecialSymbolToken;
				if (CheckSpecialSymbol("++"))
					incDecOp = IncDecOps.PreInc;
				else if (CheckSpecialSymbol("--")) incDecOp = IncDecOps.PreDec;
				
				if (!IsLocation(out location) && incDecOp!=IncDecOps.None) Error("Очаквам променлива, аргумент или поле");
			}
			
			if (incDecOp==IncDecOps.None) {
				opToken = token as SpecialSymbolToken;
				if (CheckSpecialSymbol("++")) incDecOp = IncDecOps.PostInc;
				else if (CheckSpecialSymbol("--")) incDecOp = IncDecOps.PostDec;
			}
			
			if (location!=null) {
				var fs = location.id as FieldSymbol;
				if (fs != null) {
					if (location.isArray) {
						type = fs.FieldInfo.FieldType.GetElementType();
					} else {
						type = fs.FieldInfo.FieldType;
					}
					
					// Emit
					if (location.isArray) {
						if (incDecOp==IncDecOps.None) emit.AddGetArray(fs.FieldInfo);
						else emit.AddIncArray(fs.FieldInfo, incDecOp);
					} else {
						emit.AddGetField(fs.FieldInfo);
						emit.AddIncField(fs.FieldInfo, incDecOp);
					}
					
					return true;
				}
				
				var lvs = location.id as LocalVarSymbol;
				if (lvs != null) {
					type = lvs.LocalVariableInfo.LocalType;
					
					// Emit
					emit.AddGetLocalVar(lvs.LocalVariableInfo);
					emit.AddIncLocalVar(lvs.LocalVariableInfo, incDecOp);
					
					return true;
				}
				
				var fps = location.id as FormalParamSymbol;
				if (fps != null) {
					type = fps.ParamType;
					
					// Emit
					emit.AddGetParameter(fps.ParameterInfo);
					emit.AddIncParameter(fps.ParameterInfo, incDecOp);
					
					return true;
				}
				
				var ms = location.id as MethodSymbol;
				if (ms != null) {
					// '(' [Expression {',' Expression}] ')'.
					
					var actualParamTypes = new List<Type>();
					var i = 0;
					
					if (!CheckSpecialSymbol("(")) Error("Очаквам специален символ '('");
					while (IsExpression(null, out type1)) {
						actualParamTypes.Add(type1);
						if (!AssignableTypes(ms.formalParams[i].ParamType, type1)) Error("Типа на фомалния параметър {0} и типа на актуалния параметър {1} не са съвместими", location.id, i, i);
						emit.AddAssignCast(ms.formalParams[i].ParamType, type1);
						
						if (!CheckSpecialSymbol(",")) break;
						
						i++;
					}
					if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')' expected");
					
					if (ms.formalParams.Length != actualParamTypes.Count) Error("Броя на актуалните параметри не е равен на броя на формалните параметри");
					
					// Emit
					emit.AddMethodCall(ms.methodInfo);
					
					type = ms.returnType;
					
					return true;
				}
				
				
				var ems = location.id as ExternalMethodSymbol;
				if (ems != null) {
					// '(' [Expression {',' Expression}] ')'.
					
					var actualParamTypes = new List<Type>();
					
					if (!CheckSpecialSymbol("(")) Error("Очаквам специален символ '('");
					while (IsExpression(null, out type1)) {
						actualParamTypes.Add(type1);
						if (!CheckSpecialSymbol(",")) break;
					}
					if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')'");
					
					// Emit
					var lastIx = location.id.value.LastIndexOf(Type.Delimiter);
					string memberName;
					string typeName;
					if (lastIx > 0) {
						memberName = location.id.value.Substring(lastIx + 1);
				 		typeName = location.id.value.Substring(0, lastIx);
					} else {
						memberName = location.id.value;
						typeName = "";
					}
					
					var bestMethodInfo = ems.MethodInfo[0].DeclaringType.GetMethod(memberName, BindingFlags.Public | BindingFlags.Static, null,
						CallingConventions.Standard, actualParamTypes.ToArray(), new ParameterModifier[actualParamTypes.Count]);
					if (bestMethodInfo==null) Error("Няма подходяща комбинация от типове на параметрите за метода {0}", location.id, ems.value);
					emit.AddMethodCall(bestMethodInfo);
					
					type = bestMethodInfo.ReturnType;
					
					return true;
				}
				
				type = null;
				Error("Неочакван тип на символ в таблицата на символите (вътрешна грешка)");
	
			} else if (CheckSpecialSymbol("(")) {
				if (IsType(out type)) {
					if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')'");
					if (!IsSimpleExpr(null, out type1)) Error("Очаквам израз");
					//Emit
					emit.AddCast(type, type1);
				} else {
					if (!IsExpression(null, out type)) Error("Очаквам израз");
					if (!CheckSpecialSymbol(")")) Error("Очаквам специален символ ')'");
				}
			} else if (CheckSpecialSymbol("-") || CheckSpecialSymbol("~") || CheckSpecialSymbol("!")) {
				if (!IsSimpleExpr(null, out type)) Error("Очаквам прост израз");
				
				// Emit
				emit.AddUnaryOp(opToken.value);
			} else if (IsLiteral(out type)) {
				//
			} else {
				type = null;
				return false;
			}
			
			return true;
		}
		
		// [15] Literal = Number | Boolean | Double | Char | String | 'null'.
		public bool IsLiteral(out Type type)
		{
			var literal = token;
			
			if (CheckNumber()) {
				type = typeof(Int32);
				emit.AddGetNumber(((NumberToken)literal).value);
			} else if (CheckDouble()) {
				type = typeof(Double);
				emit.AddGetDouble(((DoubleToken)literal).value);
			} else if (CheckBoolean()) {
				type = typeof(Boolean);
				emit.AddGetBoolean(((BooleanToken)literal).value);
			} else if (CheckChar()) {
				type = typeof(Char);
				emit.AddGetChar(((CharToken)literal).value);
			} else if (CheckString()) {
				type = typeof(String);
				emit.AddGetString(((StringToken)literal).value);
			} else if (CheckKeyword("null")) {
				type = null;
				emit.AddGetNull();
			} else {
				type = null;
				return false;
			}
			
			return true;
		}
		
		public bool AssignableTypes(Type typeAssignTo, Type typeAssignFrom)
		{
			//return typeAssignTo==typeAssignFrom;
			return typeAssignTo.IsAssignableFrom(typeAssignFrom);
		}
		
		public class LocationInfo
		{
			public TableSymbol id;
			public bool isArray;
		}
	}
}
