using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<object>
    {
        struct ExprLocationData
        {
            public int depth;
            public int index;
        }

        private readonly Environment native = new Environment();
        public readonly Environment globals;
        Environment environment;    // initialized to globals in ctor below
        private readonly Dictionary<Expr, ExprLocationData> locals = new Dictionary<Expr, ExprLocationData>();
        private readonly Dictionary<Expr, int> globalIndices = new Dictionary<Expr, int>();

        // Example of a native function
        private class Clock : LoxCallable
        {
            public int Arity()
            {
                return 0;
            }

            public object Call(Interpreter interpreter, List<object> args)
            {
                return (double)System.Environment.TickCount;
            }

            public override string ToString()
            {
                return "<native fn>";
            }
        }

        public Interpreter()
        {
            globals = new Environment(enclosing: native);
            environment = globals;
            native.Define(new Clock());
        }

        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach(Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            catch(RuntimeError error)
            {
                Lox.RuntimeError(error);
            }
        }

        public void Resolve(Expr expr, int depth, int index)
        {
            // This is fine because each expr is unique.
            locals[expr] = new ExprLocationData { depth = depth, index = index };
        }

        public void ResolveGlobal(Expr expr, int index)
        {
            globalIndices[expr] = index;
        }

        void Execute(Stmt statement)
        {
            statement.accept(this);
        }

        string Stringify(object obj)
        {
            if (obj == null) return "nil";

            if(obj is double d)
            {
                string dString = d.ToString();
                if(dString.EndsWith(".0"))
                {
                    dString = dString.Substring(0, dString.Length - 2);
                }
                return dString;
            }

            if(obj is bool b)
            {
                // C# prints them out capitalized so we don't use ToString on bool
                return b ? "true" : "false"; 
            }

            return obj.ToString();
        }

        // This made public for the same reason ParseExpression() made public
        public object Evaluate(Expr expr)
        {
            return expr.accept(this);
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            object left = Evaluate(expr.left);
            object right = Evaluate(expr.right);

            if(expr.op.type == TokenType.AND)
            {
                return GetTruthValue(left) && GetTruthValue(right);
            }
            if(expr.op.type == TokenType.OR)
            {
                return GetTruthValue(left) || GetTruthValue(right);
            }

            // same for strings, numbers and bools
            switch(expr.op.type)
            {
                case TokenType.EQUAL_EQUAL:
                    return IsEqual(left, right);
                case TokenType.BANG_EQUAL:
                    return !IsEqual(left, right);
            }
            
            // string specific
            if (left is string || right is string)
            {
                if (expr.op.type != TokenType.PLUS)
                {
                    throw new RuntimeError(expr.op, expr.op.type.ToString() + " not a valid operator on strings.");
                }
                return Stringify(left) + Stringify(right);
            }

            // number specific
            CheckNumberOperands(expr.op, left, right);

            switch (expr.op.type)
            {
                case TokenType.PLUS:
                    return (double)left + (double)right;
                case TokenType.MINUS:
                    return (double)left - (double)right;
                case TokenType.STAR:
                    return (double)left * (double)right;
                case TokenType.SLASH:
                    double r = (double)right;
                    if(r == 0)
                    {
                        throw new RuntimeError(expr.op, "Divide by zero error.");
                    }
                    return (double)left / r;
                case TokenType.GREATER:
                    return (double)left > (double)right;
                case TokenType.GREATER_EQUAL:
                    return (double)left >= (double)right;
                case TokenType.LESS:
                    return (double)left < (double)right;
                case TokenType.LESS_EQUAL:
                    return (double)left <= (double)right;
                default:
                    Lox.ReportError(expr.op, expr.op.type.ToString() + " not a valid operator on doubles.");
                    return right;
            }
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            return Evaluate(expr.expr);
        }

        public object visitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }

        public object visitTernaryExpr(Expr.Ternary expr)
        {
            object conditionExpr = Evaluate(expr.condition);
            bool condition = GetTruthValue(conditionExpr);
            return condition ? Evaluate(expr.conditionTrueValue) : Evaluate(expr.conditionFalseValue);
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            object right = Evaluate(expr.expr);

            if(expr.op.type == TokenType.MINUS)
            {
                CheckNumberOperand(expr.op, right);
                return -(double)right;
            }

            return !(bool)GetTruthValue(right);
        }

        public object visitVariableExpr(Expr.Variable expr)
        {
            // Variables and functions exist in the same environment, so unlike some other languages,
            // Lox does not allow a function to have the same name as a variable in the same environment.
            return LookUpVariable(expr);
        }

        public object visitAssignmentExpr(Expr.Assignment expr)
        {
            object value = Evaluate(expr.value);
            if(locals.TryGetValue(expr, out var exprLocation))
            {
                environment.AssignAt(exprLocation.depth, exprLocation.index, value);
            }
            else
            {
                globals.Assign(globalIndices[expr], value);
            }
            return value;
        }

        public object visitExprListExpr(Expr.ExprList expr)
        {
            for(int i = 0; i < expr.exprs.Count - 1; i++)
            {
                Evaluate(expr.exprs[i]);
            }
            return Evaluate(expr.exprs[expr.exprs.Count - 1]);
        }

        public object visitCallExpr(Expr.Call expr)
        {
            object callee = Evaluate(expr.callee);

            List<object> args = expr.args.ConvertAll(Evaluate);
            
            if (callee is LoxCallable function)
            {
                if(args.Count != function.Arity())
                {
                    throw new RuntimeError(expr.paren, "Expected " + function.Arity() + " arguments but got " + args.Count + ".");
                }
                return function.Call(this, args);
            }
            else
            {
                throw new RuntimeError(expr.paren, "Can only call functions and classes.");
            }
        }

        public object visitLambdaExpr(Expr.Lambda expr)
        {
            return new LoxLambda(expr, environment);
        }

        public object visitGetExpr(Expr.Get expr)
        {
            object instance = Evaluate(expr.instance);
            if(instance is LoxInstance loxInstance)
            {
                return loxInstance.Get(expr.name);
            }
            if(instance is LoxClass loxClass)
            {
                return loxClass.FindStaticMethod(expr.name.lexeme);
            }

            throw new RuntimeError(expr.name, "Only instances have properties.");
        }

        public object visitSetExpr(Expr.Set expr)
        {
            object instance = Evaluate(expr.instance);
            if(instance is LoxInstance loxInstance)
            {
                object value = Evaluate(expr.value);
                loxInstance.Set(expr.name, value);
                return value;
            }
            throw new RuntimeError(expr.name, "Only instances have fields.");
        }

        public object visitThisExpr(Expr.This expr)
        {
            return LookUpVariable(expr);
        }

        public object visitSuperExpr(Expr.Super expr)
        {
            var exprLocationData = locals[expr];
            LoxClass superclass = (LoxClass)environment.GetAt(exprLocationData.depth, exprLocationData.index);
            // We know "this" is one environment removed from "super" because of the way we set the environments up
            // in visitClassStmt. Hacky fix but it works. We also know its index is 0 because "this" is the only variable
            // in its environment.
            LoxInstance instance = (LoxInstance)environment.GetAt(exprLocationData.depth - 1, 0);
            LoxFunction method = superclass.FindMethod(expr.method.lexeme);
            if(method == null)
            {
                throw new RuntimeError(expr.method, "Undefined superclass property '" + expr.method.lexeme + "'.");
            }
            return method.Bind(instance);
        }

        // Everything is "truthy" except for false and nil in Lox
        private bool GetTruthValue(object obj)
        {
            if (obj == null) return false;
            if (obj is bool b) return b;
            return true;
        }

        private bool IsEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;
            return a.Equals(b);
        }

        private void CheckNumberOperand(Token op, object operand)
        {
            if(!(operand is double)) { throw new RuntimeError(op, "Operand must be a number."); } 
        }

        private void CheckNumberOperands(Token op, object lhs, object rhs)
        {
            if(!(lhs is double && rhs is double)) { throw new RuntimeError(op, "Operands must be numbers."); }
        }

        private object LookUpVariable(Expr expr)
        {
            if(locals.TryGetValue(expr, out var exprLocation))
            {
                return environment.GetAt(exprLocation.depth, exprLocation.index);
            }
            else
            {
                return globals.Get(globalIndices[expr]);
            }
        }

        // Hacky fix: since we can't implement IVisitor<void>, we have to implement
        // IVisitor<object> and return null instead. LMAO Java requires you to return null
        // anyways so it doesn't even matter.
        // Statements don't produce a value so the return value of the visitStmt functions 
        // is always null.
        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.expression);
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            object value = Evaluate(stmt.expression);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public object visitVarStmt(Stmt.Var stmt)
        {
            object varValue = null;
            if(stmt.intializer != null)
            {
                varValue = Evaluate(stmt.intializer);
            }
            environment.Define(varValue);
            return null;
        }

        public object visitBlockStmt(Stmt.Block block)
        {
            Environment blockEnvironment = new Environment(enclosing: this.environment);
            ExecuteBlock(block.statements, blockEnvironment);
            return null;
        }

        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            Environment previous = this.environment;
            try
            {
                this.environment = environment;

                foreach(Stmt statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                // Once we exit the block by executing all the statements in it or encountering a runtime error,
                // we reset the environment to the enclosing scope.
                // This is the same environment that this.environment pointed to at the start of this function.
                this.environment = previous;
            }
        }

        public object visitIfStmt(Stmt.If stmt)
        {
            if(GetTruthValue(Evaluate(stmt.condition)))
            {
                Execute(stmt.ifBody);
            }
            else if(stmt.elseBody != null)
            {
                Execute(stmt.elseBody);
            }
            return null;
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            if(stmt.body is Stmt.Break)
            {
                return null;
            }

            while(GetTruthValue(Evaluate(stmt.condition)))
            {
                try
                {
                    Execute(stmt.body);
                } catch(BreakException)
                {
                    break;
                }
            }

            return null;
        }

        private class BreakException : Exception { }

        public object visitBreakStmt(Stmt.Break stmt)
        {
            throw new BreakException();
        }

        public object visitReturnStmt(Stmt.Return stmt)
        {
            object value = stmt.value == null ? null : Evaluate(stmt.value);
            throw new ReturnException { value = value };
        }

        public object visitFunctionStmt(Stmt.Function stmt)
        {
            LoxFunction function = new LoxFunction(function: stmt, closure: environment, false);
            environment.Define(function);
            return null;
        }

        public object visitClassStmt(Stmt.Class stmt)
        {
            object superclass = null;
            if(stmt.superclass != null)
            {
                superclass = Evaluate(stmt.superclass);
                if(!(superclass is LoxClass))
                {
                    throw new RuntimeError(stmt.superclass.name, "Superclass must be a class.");
                }
            }

            int classIndex = environment.Define(null);

            if (stmt.superclass != null)
            {
                environment = new Environment(enclosing: environment);
                environment.Define(superclass);
            }

            Dictionary<string, LoxFunction> methods = new Dictionary<string, LoxFunction>();
            foreach(Stmt.Function method in stmt.methods)
            {
                LoxFunction loxFunc = new LoxFunction(method, closure: environment, method.name.lexeme == "init");
                methods.Add(method.name.lexeme, loxFunc);
            }
            Dictionary<string, LoxFunction> staticMethods = new Dictionary<string, LoxFunction>();
            foreach(Stmt.Function method in stmt.staticMethods)
            {
                LoxFunction loxFunc = new LoxFunction(method, closure: environment, false);
                staticMethods.Add(method.name.lexeme, loxFunc);
            }
            LoxClass loxClass = new LoxClass(stmt.name.lexeme, (LoxClass)superclass, staticMethods, methods);
            
            if(stmt.superclass != null)
            {
                environment = environment.enclosing;
            }

            environment.Assign(classIndex, loxClass);

            return null;
        }


        /*public object visitForStmt(Stmt.For stmt)
        {
            if(stmt.initializer != null)
            {
                Execute(stmt.initializer);
            }
            // If there is no condition (stmt.condition == null) it's equivalent to a while true
            while (stmt.condition == null || GetTruthValue(Evaluate(stmt.condition)))
            {
                Execute(stmt.body);
                if(stmt.increment != null)
                {
                    Evaluate(stmt.increment);
                }
            }
            return null;
        }*/
    }
}
