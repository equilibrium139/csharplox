﻿using System;
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

        public Resolver(Interpreter interpreter)
        {
            this.interpreter = interpreter;
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

        private void Declare(Token nameToken, string name)
        {
            if (scopes.Count > 0)
            {
                var scope = scopes[scopes.Count - 1];
                if (scope.ContainsKey(name))
                {
                    Lox.ReportError(nameToken, "Variable with this name was already declared in the same scope.");
                }
                else
                {
                    scope.Add(name, false);
                    int varIndex = GetNextIndex();
                    Peek(varIndices).Add(name, varIndex);
                    Peek(unusedVars).Add(name, nameToken);
                }
            }
            else
            {
                globalVarIndices.Add(name, globalIndex++);
                unusedGlobals.Add(name, nameToken);
            }
        }

        private void Define(string name)
        {
            if(scopes.Count > 0)
            {
                scopes[scopes.Count - 1][name] = true;
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

        private void ResolveFunction(Stmt.Function function)
        {
            BeginScope();
            foreach(Token parameter in function.parameters)
            {
                string name = (string)parameter.data;
                Declare(parameter, name);
                Define(name);
            }
            function.body.ForEach(Resolve);
            EndScope();
        }

        public object visitAssignmentExpr(Expr.Assignment expr)
        {
            Resolve(expr.value);
            ResolveLocal(expr, (string)(expr.name.data), AccessType.LHS);
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
            BeginScope();

            foreach(Token parameter in expr.parameters)
            {
                string name = (string)parameter.data;
                Declare(parameter, name);
                Define(name);
            }

            expr.body.ForEach(Resolve);

            EndScope();

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
            string name = (string)expr.name.data;
            if (scopes.Count > 0 && scopes[scopes.Count - 1].TryGetValue(name, out bool resolved) && !resolved)
            {
                Lox.ReportError(expr.name, "Can't read local variable in its own initializer.");
            }

            ResolveLocal(expr, name, AccessType.RHS);
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
            string name = (string)stmt.name.data;
            Declare(stmt.name, name);
            Define(name);
            ResolveFunction(stmt);
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
            if(stmt.value != null) Resolve(stmt.value);
            return null;
        }

        object Stmt.IVisitor<object>.visitVarStmt(Stmt.Var stmt)
        {
            string name = (string)stmt.name.data;
            Declare(stmt.name, name);
            if (stmt.intializer != null)
            {
                Resolve(stmt.intializer);
            }
            Define(name);
            return null;
        }

        object Stmt.IVisitor<object>.visitWhileStmt(Stmt.While stmt)
        {
            Resolve(stmt.condition);
            Resolve(stmt.body);
            return null;
        }
    }
}
