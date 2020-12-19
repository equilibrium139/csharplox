using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class ASTPrinter : Expr.IVisitor<string>
    {
        public string print(Expr expr)
        {
            if(expr != null) return expr.accept(this);
            return "(null expr)";
        }

        public string visitBinaryExpr(Expr.Binary expr)
        {
            return parenthesize(expr.op.ToString(), expr.left, expr.right);
        }

        public string visitGroupingExpr(Expr.Grouping expr)
        {
            return parenthesize("group", expr.expr);
        }

        public string visitLiteralExpr(Expr.Literal expr)
        {
            return expr.value == null ? "nil" : expr.value.ToString();
        }

        public string visitUnaryExpr(Expr.Unary expr)
        {
            return parenthesize(expr.op.ToString(), expr.expr);
        }

        public string visitTernaryExpr(Expr.Ternary expr)
        {
            return parenthesize("IF" + expr.condition.accept(this)) + " THEN " + expr.conditionTrueValue.accept(this)
                                                       + " ELSE " + expr.conditionFalseValue.accept(this);
        }

        public string visitVariableExpr(Expr.Variable expr)
        {
            return parenthesize(expr.name.lexeme);
        }

        public string visitAssignmentExpr(Expr.Assignment expr)
        {
            return parenthesize(expr.name.lexeme + "=", expr.value);
        }

        public string visitExprListExpr(Expr.ExprList expr)
        {
            return parenthesize("EXPR_LIST", expr.exprs.ToArray());
        }

        public string visitCallExpr(Expr.Call expr)
        {
            return parenthesize("CALL " + expr.callee.ToString(), expr.args.ToArray());
        }

        public string visitLambdaExpr(Expr.Lambda expr)
        {
            return parenthesize("LAMBDA");
        }

        public string visitGetExpr(Expr.Get expr)
        {
            return parenthesize("GET");
        }

        private string parenthesize(string name, params Expr[] exprs)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("(").Append(name);
            foreach(Expr expr in exprs)
            {
                builder.Append(" ");
                builder.Append(expr.accept(this));
            }
            builder.Append(")");
            return builder.ToString();
        }

        public string visitSetExpr(Expr.Set expr)
        {
            throw new NotImplementedException();
        }

        public string visitThisExpr(Expr.This expr)
        {
            throw new NotImplementedException();
        }
    }
}
