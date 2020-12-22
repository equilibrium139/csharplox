using System;
using System.Collections.Generic;
using System.Text;

// Operates between the parser and the interpreter. For now, takes the syntax tree produced
// by the parser and resolves variable accesses. This helps solve the issue with closures in
// chapter 11 and also speeds up the interpreter by resolving variable accesses once statically 
// rather than each time the interpreter sees the variable, which was really expensive for a variable that
// was accessed many times in a loop. Since variable resolution touches each node once, its performance is 
// O(n) where n = # of AST nodes.

// If Lox had static types, a type checker could be included in this resolver, in addition to any work that
// "doesn't rely on state that's only available at runtime".

namespace sharplox
{
    class Resolver : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        private readonly Interpreter interpreter;
        // Used only for local block scopes. If a variable we are resolving can't be found in this stack,
        // we assume it is global. Bool value represents whether or not we have finished resolving that var's
        // initializer.

        private readonly List<Dictionary<string, bool>> scopes = new List<Dictionary<string, bool>>();

        private readonly List<Dictionary<string, Token>> unusedVars = new List<Dictionary<string, Token>>();
        private readonly Dictionary<string, Token> unusedGlobals = new Dictionary<string, Token>();

        private readonly List<Dictionary<string, int>> varIndices = new List<Dictionary<string, int>>();
        private readonly List<int> scopeIndices = new List<int>();

        private readonly Dictionary<string, int> globalVarIndices = new Dictionary<string, int>();
        private int globalIndex = 0;

        private enum ClassType
        {
            NONE, CLASS, SUBCLASS
        }

        enum FunctionType
        {
            NONE, FUNCTION, LAMBDA, METHOD, STATIC, INITIALIZER
        }


        ClassType currentClass = ClassType.NONE; // used to ensure "this" keyword only used in methods.
        FunctionType currentFunction = FunctionType.NONE;
        
        public Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
            foreach(string nativeFunc in Interpreter.GetNativeFuncs())
            {
                globalVarIndices.Add(nativeFunc, globalIndex++);
            }
        }

        // This is only called from outside the class. That way we know that when the for loop is done,
        // the entire program has been resolved and only then can we report any unused globals. 
        public void Resolve(List<Stmt> stmts)
        {
            foreach(Stmt stmt in stmts)
            {
                Resolve(stmt);
            }
            foreach(var v in unusedGlobals)
            {
                Lox.ReportError(v.Value, "Variable '" + v.Key + "' not used.");
            }
        }

        private void Resolve(Stmt stmt)
        {
            stmt.accept(this);
        }

        private void Resolve(Expr expr)
        {
            expr.accept(this);
        }

        private void BeginScope()
        {
            scopes.Add(new Dictionary<string, bool>());
            unusedVars.Add(new Dictionary<string, Token>());
            varIndices.Add(new Dictionary<string, int>());
            scopeIndices.Add(0);
        }

        private static T Peek<T>(List<T> list)
        {
            return list[list.Count - 1];
        }

        private static T Pop<T>(List<T> list)
        {
            T removed = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return removed;
        }

        private int GetNextIndex()
        {
            return scopeIndices[scopeIndices.Count - 1]++;
        }

        private void EndScope()
        {
            Pop(scopes);
            Pop(varIndices);
            Pop(scopeIndices);

            var unusedVarsInScope = Pop(unusedVars);
            foreach(var v in unusedVarsInScope)
            {
                Lox.ReportError(v.Value, "Variable '" + v.Key + "' not used.");
            }
        }

        private void Declare(Token name)
        {
            if (scopes.Count > 0)
            {
                var scope = scopes[scopes.Count - 1];
                if (scope.ContainsKey(name.lexeme))
                {
                    Lox.ReportError(name, "Variable with this name was already declared in the same scope.");
                }
                else
                {
                    scope.Add(name.lexeme, false);
                    int varIndex = GetNextIndex();
                    Peek(varIndices).Add(name.lexeme, varIndex);
                }
            }
            else
            {
                if(globalVarIndices.ContainsKey(name.lexeme))
                {
                    Lox.ReportError(name, "Variable with this name was already declared in global scope.");
                }
                else
                {
                    globalVarIndices.Add(name.lexeme, globalIndex++);
                }
            }
        }

        private void MarkUnused(Token name)
        {
            if (scopes.Count > 0)
            {
                Peek(unusedVars)[name.lexeme] = name;
            }
            else unusedGlobals[name.lexeme] = name;
        }

        private void Define(Token name)
        {
            if(scopes.Count > 0)
            {
                scopes[scopes.Count - 1][name.lexeme] = true;
            }
        }

        enum AccessType
        {
            LHS, RHS
        }

        private void ResolveLocal(Expr expr, string name, AccessType accessType)
        {
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                if(scopes[i].ContainsKey(name))
                {
                    interpreter.Resolve(expr, scopes.Count - 1 - i, varIndices[i][name]);
                    if(accessType == AccessType.RHS)
                    {
                        unusedVars[i].Remove(name);
                    }
                    return;
                }
            }

            // It's a reference to a global
            if (accessType == AccessType.RHS)
            {
                unusedGlobals.Remove(name);
            }
            interpreter.ResolveGlobal(expr, globalVarIndices[name]);
        }

        private void ResolveFunction(List<Token> parameters, List<Stmt> body, FunctionType type)
        {
            FunctionType enclosingType = currentFunction;
            currentFunction = type;
            BeginScope();
            foreach(Token parameter in parameters)
            {
                Declare(parameter);
                Define(parameter);
                MarkUnused(parameter);
            }
            body.ForEach(Resolve);
            EndScope();
            currentFunction = enclosingType;
        }

        public object visitAssignmentExpr(Expr.Assignment expr)
        {
            Resolve(expr.value);
            ResolveLocal(expr, expr.name.lexeme, AccessType.LHS);
            return null;
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            Resolve(expr.left);
            Resolve(expr.right);
            return null;
        }

        public object visitCallExpr(Expr.Call expr)
        {
            Resolve(expr.callee);
            expr.args.ForEach(Resolve);
            return null;
        }

        public object visitExprListExpr(Expr.ExprList expr)
        {
            expr.exprs.ForEach(Resolve);
            return null;
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            Resolve(expr.expr);
            return null;
        }

        public object visitLambdaExpr(Expr.Lambda expr)
        {
            ResolveFunction(expr.parameters, expr.body, FunctionType.LAMBDA);
            return null;
        }

        public object visitLiteralExpr(Expr.Literal expr)
        {
            return null;
        }

        public object visitTernaryExpr(Expr.Ternary expr)
        {
            Resolve(expr.condition);
            Resolve(expr.conditionTrueValue);
            Resolve(expr.conditionFalseValue);
            return null;
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            Resolve(expr.expr);
            return null;
        }

        public object visitVariableExpr(Expr.Variable expr)
        {
            string name = expr.name.lexeme;
            if (scopes.Count > 0 && scopes[scopes.Count - 1].TryGetValue(name, out bool resolved) && !resolved)
            {
                Lox.ReportError(expr.name, "Can't read local variable in its own initializer.");
            }

            ResolveLocal(expr, name, AccessType.RHS);
            return null;
        }

        public object visitGetExpr(Expr.Get expr)
        {
            Resolve(expr.instance);
            return null;
        }

        public object visitSetExpr(Expr.Set expr)
        {
            Resolve(expr.value);
            Resolve(expr.instance);
            return null;
        }

        public object visitThisExpr(Expr.This expr)
        {
            if(currentClass == ClassType.NONE)
            {
                Lox.ReportError(expr.keyword, "Can't use 'this' outside of a class.");
            }
            else if(currentFunction == FunctionType.STATIC)
            {
                Lox.ReportError(expr.keyword, "Can't use 'this' in a static method.");
            }
            else ResolveLocal(expr, expr.keyword.lexeme, AccessType.RHS);
            return null;
        }

        public object visitSuperExpr(Expr.Super expr)
        {
            if (currentClass != ClassType.SUBCLASS)
            {
                Lox.ReportError(expr.keyword, "Can't use 'super' outside of a sub class.");
            }
            else ResolveLocal(expr, expr.keyword.lexeme, AccessType.RHS);
            return null;
        }

        object Stmt.IVisitor<object>.visitBlockStmt(Stmt.Block stmt)
        {
            BeginScope();
            stmt.statements.ForEach(Resolve);
            EndScope();
            return null;
        }

        object Stmt.IVisitor<object>.visitBreakStmt(Stmt.Break stmt)
        {
            return null;
        }

        object Stmt.IVisitor<object>.visitExpressionStmt(Stmt.Expression stmt)
        {
            Resolve(stmt.expression);
            return null;
        }

        object Stmt.IVisitor<object>.visitFunctionStmt(Stmt.Function stmt)
        {
            Declare(stmt.name);
            Define(stmt.name);
            MarkUnused(stmt.name);
            ResolveFunction(stmt.parameters, stmt.body, FunctionType.FUNCTION);
            return null;
        }

        object Stmt.IVisitor<object>.visitIfStmt(Stmt.If stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.ifBody);
            if(stmt.elseBody != null) Resolve(stmt.elseBody);
            return null;
        }

        object Stmt.IVisitor<object>.visitPrintStmt(Stmt.Print stmt)
        {
            Resolve(stmt.expression);
            return null;
        }

        object Stmt.IVisitor<object>.visitReturnStmt(Stmt.Return stmt)
        {
            if(currentFunction == FunctionType.NONE)
            {
                Lox.ReportError(stmt.keyword, "Can only return from functions or methods.");
            }
            else if (stmt.value != null)
            {
                if (currentFunction == FunctionType.INITIALIZER)
                {
                    Lox.ReportError(stmt.keyword, "Cannot return value from an initializer.");
                }
                else Resolve(stmt.value);
            }
            return null;
        }

        object Stmt.IVisitor<object>.visitVarStmt(Stmt.Var stmt)
        {
            Declare(stmt.name);
            if (stmt.intializer != null)
            {
                Resolve(stmt.intializer);
            }
            Define(stmt.name);
            MarkUnused(stmt.name);
            return null;
        }

        object Stmt.IVisitor<object>.visitWhileStmt(Stmt.While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
            return null;
        }

        public object visitClassStmt(Stmt.Class stmt)
        {
            ClassType enclosingClass = currentClass;
            currentClass = ClassType.CLASS;

            Declare(stmt.name);
            Define(stmt.name);
            MarkUnused(stmt.name);

            if (stmt.superclass != null)
            {
                if (stmt.superclass.name.lexeme == stmt.name.lexeme)
                {
                    Lox.ReportError(stmt.superclass.name, "Class cannot inherit from itself.");
                }
                else
                {
                    currentClass = ClassType.SUBCLASS;
                    Resolve(stmt.superclass);
                    BeginScope();
                    Peek(scopes)["super"] = true;
                    Peek(varIndices)["super"] = GetNextIndex();
                }
            }

            BeginScope();
            // "this" is the only variable we declare at class scope. That means that when ResolveLocal()
            // is called on "this" in the methods, the resolver will compute depth = 1, index = 0. 
            // In LoxFunction.Bind(), we create a new function with a closure whose only variable is "this" (at index 0)
            // and since it's a closure, it has a depth of 1 from the function body.
            Peek(scopes)["this"] = true;
            Peek(varIndices)["this"] = GetNextIndex(); // TODO maybe change to 0

            foreach (Stmt.Function method in stmt.staticMethods)
            {
                Declare(method.name);
                Define(method.name);
                ResolveFunction(method.parameters, method.body, FunctionType.STATIC);
            }

            foreach (Stmt.Function method in stmt.methods)
            {
                Declare(method.name);
                Define(method.name);
                FunctionType declaration = FunctionType.METHOD;
                if(method.name.lexeme == "init")
                {
                    declaration = FunctionType.INITIALIZER;
                }
                ResolveFunction(method.parameters, method.body, declaration);
            }

            EndScope();

            if (stmt.superclass != null) EndScope();

            currentClass = enclosingClass;
            return null;
        }
    }
}
