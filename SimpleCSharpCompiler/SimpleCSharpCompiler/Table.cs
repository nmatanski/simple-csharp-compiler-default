using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using SimpleCSharpCompiler.TableSymbols;
using SimpleCSharpCompiler.Tokens;

namespace SimpleCSharpCompiler
{
    public class Table
    {
        private Stack<Dictionary<string, TableSymbol>> symbolTable;
        private Dictionary<string, TableSymbol> fieldScope;
        private Dictionary<string, TableSymbol> universeScope;
        private List<string> usingNamespaces = new List<string>();
        private List<string> references;
        
        public Table(List<string> references)
        {
            symbolTable = new Stack<Dictionary<string, TableSymbol>>();
            universeScope = BeginScope();
            fieldScope = BeginScope();
            this.references = references;
            foreach (var assemblyRef in references) {
                //Assembly.LoadWithPartialName(assemblyRef); // deprecated
                Assembly.Load(assemblyRef);
            }
        }
        
        public override string ToString()
        {
            var s = new StringBuilder();
            var i = symbolTable.Count;
            s.AppendFormat("=========\n");
            foreach (var table in symbolTable) {
                s.AppendFormat("---[{0}]---\n", i--);
                foreach (var row in table) {
                    s.AppendFormat("[{0}] {1}\n", row.Key, row.Value);
                }
            }
            s.AppendFormat("=========\n");
            return s.ToString();
        }
        
        public void AddUsingNamespace(string usingNamespace)
        {
            usingNamespaces.Add(usingNamespace);
        }
        
        public TableSymbol Add(TableSymbol symbol) {
            symbolTable.Peek().Add(symbol.value, symbol);
            return symbol;
        }
        
        public TableSymbol AddToUniverse(TableSymbol symbol) {
            universeScope.Add(symbol.value, symbol);
            return symbol;
        }
        
        public FieldSymbol AddField(IdentToken token, FieldInfo field) {
            var result = new FieldSymbol(token, field);
            fieldScope.Add(token.value, result);
            return result;
        }
        
        public LocalVarSymbol AddLocalVar(IdentToken token, LocalBuilder localBuilder) {
            var result = new LocalVarSymbol(token, localBuilder);
            symbolTable.Peek().Add(token.value, result);
            return result;
        }
        
        public FormalParamSymbol AddFormalParam(IdentToken token, Type type, ParameterBuilder parameterInfo) {
            var result = new FormalParamSymbol(token, type, parameterInfo);
            symbolTable.Peek().Add(token.value, result);
            return result;
        }
        
        public MethodSymbol AddMethod(IdentToken token, Type type, FormalParamSymbol[] formalParams, MethodInfo methodInfo) {
            var result = new MethodSymbol(token, type, formalParams, methodInfo);
            symbolTable.Peek().Add(token.value, result);
            return result;
        }
        
        public Dictionary<string, TableSymbol> BeginScope()
        {
            symbolTable.Push(new Dictionary<string, TableSymbol>());
            return symbolTable.Peek();
        }
        
        public void EndScope()
        {
            Debug.WriteLine(ToString());
            
            symbolTable.Pop();
        }
        
        public TableSymbol GetSymbol(string ident)
        {
            TableSymbol result;
            foreach (var table in symbolTable) {
                if (table.TryGetValue(ident, out result)) {
                    return result;
                }
            }
            return ResolveExternalMember(ident);
        }
        
        public bool ExistCurrentScopeSymbol(string ident)
        {
            return symbolTable.Peek().ContainsKey(ident);
        }
        
        public Type ResolveExternalType(string ident)
        {
            // Type
            // Namespace.Type
            
            var type = Type.GetType(ident, false, false);
            if (type != null) return type;
            foreach (var ns in usingNamespaces) {
                var nsTypeName = ns + Type.Delimiter + ident;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                    type = assembly.GetType(ident);
                    if (type != null) return type;
                    type = assembly.GetType(nsTypeName);
                    if (type != null) return type;
                }
            }
            return null;
        }
            
        public TableSymbol ResolveExternalMember(string ident)
        {
            // Type.Member
            // Namespace.Type.Member
            
            var lastIx = ident.LastIndexOf(Type.Delimiter);
            if (lastIx > 0) {
                var memberName = ident.Substring(lastIx + 1);
                var typeName = ident.Substring(0, lastIx);
                Debug.WriteLine(string.Format("{0} -- {1}", typeName, memberName));
                
                var type = ResolveExternalType(typeName);
                if (type == null) {
                    foreach (var usingNamespace in usingNamespaces) {
                        type = ResolveExternalType(usingNamespace + "." + typeName);
                        if (type != null) break;
                    }
                }
                
                if (type != null) {
                    var fi = type.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
                    if (fi != null) return new FieldSymbol(new IdentToken(0,0,memberName), fi);
                    var mi = type.GetMember(memberName, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static);
                    if (mi != null) return new ExternalMethodSymbol(new IdentToken(0,0,memberName), (MethodInfo[])mi);
                }
            }
            
            return null;
        }
        
    }
}
