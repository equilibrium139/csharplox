using System.Collections.Generic;
namespace sharplox
{
abstract class Expr
{
public interface IVisitor<T>
{
T visitBinaryExpr(Binary expr);
T visitUnaryExpr(Unary expr);
T visitLiteralExpr(Literal expr);
T visitGroupingExpr(Grouping expr);
T visitTernaryExpr(Ternary expr);
T visitVariableExpr(Variable expr);
T visitAssignmentExpr(Assignment expr);
T visitExprListExpr(ExprList expr);
T visitCallExpr(Call expr);
T visitLambdaExpr(Lambda expr);
T visitGetExpr(Get expr);
T visitSetExpr(Set expr);
T visitThisExpr(This expr);
}
public abstract T accept<T>(IVisitor<T> visitor);

public class Binary : Expr
{
public Expr left;
public Token op;
public Expr right;
public Binary( Expr left, Token op, Expr right)
{
this.left = left;
this.op = op;
this.right = right;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitBinaryExpr(this);
}
}

public class Unary : Expr
{
public Token op;
public Expr expr;
public Unary( Token op, Expr expr)
{
this.op = op;
this.expr = expr;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitUnaryExpr(this);
}
}

public class Literal : Expr
{
public object value;
public Literal( object value)
{
this.value = value;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitLiteralExpr(this);
}
}

public class Grouping : Expr
{
public Expr expr;
public Grouping( Expr expr)
{
this.expr = expr;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitGroupingExpr(this);
}
}

public class Ternary : Expr
{
public Expr condition;
public Expr conditionTrueValue;
public Expr conditionFalseValue;
public Ternary( Expr condition, Expr conditionTrueValue, Expr conditionFalseValue)
{
this.condition = condition;
this.conditionTrueValue = conditionTrueValue;
this.conditionFalseValue = conditionFalseValue;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitTernaryExpr(this);
}
}

public class Variable : Expr
{
public Token name;
public Variable( Token name)
{
this.name = name;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitVariableExpr(this);
}
}

public class Assignment : Expr
{
public Token name;
public Expr value;
public Assignment( Token name, Expr value)
{
this.name = name;
this.value = value;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitAssignmentExpr(this);
}
}

public class ExprList : Expr
{
public List<Expr> exprs;
public ExprList( List<Expr> exprs)
{
this.exprs = exprs;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitExprListExpr(this);
}
}

public class Call : Expr
{
public Expr callee;
public Token paren;
public List<Expr> args;
public Call( Expr callee, Token paren, List<Expr> args)
{
this.callee = callee;
this.paren = paren;
this.args = args;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitCallExpr(this);
}
}

public class Lambda : Expr
{
public Token funKeyword;
public List<Token> parameters;
public List<Stmt> body;
public Lambda( Token funKeyword, List<Token> parameters, List<Stmt> body)
{
this.funKeyword = funKeyword;
this.parameters = parameters;
this.body = body;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitLambdaExpr(this);
}
}

public class Get : Expr
{
public Expr instance;
public Token name;
public Get( Expr instance, Token name)
{
this.instance = instance;
this.name = name;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitGetExpr(this);
}
}

public class Set : Expr
{
public Expr instance;
public Token name;
public Expr value;
public Set( Expr instance, Token name, Expr value)
{
this.instance = instance;
this.name = name;
this.value = value;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitSetExpr(this);
}
}

public class This : Expr
{
public Token keyword;
public This( Token keyword)
{
this.keyword = keyword;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitThisExpr(this);
}
}

}
}
