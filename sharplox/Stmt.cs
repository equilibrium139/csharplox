using System.Collections.Generic;
namespace sharplox
{
abstract class Stmt
{
public interface IVisitor<T>
{
T visitExpressionStmt(Expression stmt);
T visitPrintStmt(Print stmt);
T visitVarStmt(Var stmt);
T visitBlockStmt(Block stmt);
T visitIfStmt(If stmt);
T visitWhileStmt(While stmt);
T visitBreakStmt(Break stmt);
T visitFunctionStmt(Function stmt);
T visitReturnStmt(Return stmt);
T visitClassStmt(Class stmt);
}
public abstract T accept<T>(IVisitor<T> visitor);

public class Expression : Stmt
{
public Expr expression;
public Expression( Expr expression)
{
this.expression = expression;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitExpressionStmt(this);
}
}

public class Print : Stmt
{
public Expr expression;
public Print( Expr expression)
{
this.expression = expression;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitPrintStmt(this);
}
}

public class Var : Stmt
{
public Token name;
public Expr intializer;
public Var( Token name, Expr intializer)
{
this.name = name;
this.intializer = intializer;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitVarStmt(this);
}
}

public class Block : Stmt
{
public List<Stmt> statements;
public Block( List<Stmt> statements)
{
this.statements = statements;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitBlockStmt(this);
}
}

public class If : Stmt
{
public Expr condition;
public Stmt ifBody;
public Stmt elseBody;
public If( Expr condition, Stmt ifBody, Stmt elseBody)
{
this.condition = condition;
this.ifBody = ifBody;
this.elseBody = elseBody;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitIfStmt(this);
}
}

public class While : Stmt
{
public Expr condition;
public Stmt body;
public While( Expr condition, Stmt body)
{
this.condition = condition;
this.body = body;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitWhileStmt(this);
}
}

public class Break : Stmt
{
public Break()
{
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitBreakStmt(this);
}
}

public class Function : Stmt
{
public Token name;
public List<Token> parameters;
public List<Stmt> body;
public Function( Token name, List<Token> parameters, List<Stmt> body)
{
this.name = name;
this.parameters = parameters;
this.body = body;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitFunctionStmt(this);
}
}

public class Return : Stmt
{
public Token keyword;
public Expr value;
public Return( Token keyword, Expr value)
{
this.keyword = keyword;
this.value = value;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitReturnStmt(this);
}
}

public class Class : Stmt
{
public Token name;
public Expr.Variable superclass;
public List<Stmt.Function> staticMethods;
public List<Stmt.Function> methods;
public Class( Token name, Expr.Variable superclass, List<Stmt.Function> staticMethods, List<Stmt.Function> methods)
{
this.name = name;
this.superclass = superclass;
this.staticMethods = staticMethods;
this.methods = methods;
}
public override T accept<T>(IVisitor<T> visitor)
{
return visitor.visitClassStmt(this);
}
}

}
}
