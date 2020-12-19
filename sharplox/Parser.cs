using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class Parser
    {
        readonly List<Token> tokens;
        int current = 0;
        // This is used to ensure break statements only appear in loops. It's simpler to keep a 
        // count of outer and nested loops rather than having a single bool because the logic for
        // that is a bit more complex.
        int loopsCurrentlyParsing = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private class ParseError : Exception { }

        private ParseError Error(Token token, string message)
        {
            Lox.ReportError(token, message);
            return new ParseError();
        }

        private Token consume(TokenType type, string message)
        {
            if(peek().type == type)
            {
                return advance();
            }

            throw Error(peek(), message);
        }

        private void synchronize()
        {
            advance();
            while(!isAtEnd())
            {
                if (previous().type == TokenType.SEMICOLON) return;

                switch(peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.VAR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.RETURN:
                        return;
                }

                advance();
            }
        }

        bool isAtEnd()
        {
            return tokens[current].type == TokenType.EOF;
        }

        Token peek()
        {
            return tokens[current];
        }

        void putBack(int n = 1)
        {
            if(current > n)
            {
                current -= n;
            }
        }

        Token peekNext()
        {
            if(current + 1 < tokens.Count)
            {
                return tokens[current + 1];
            }
            return tokens[current];
        }

        Token previous()
        {
            return tokens[current - 1];
        }

        Token advance()
        {
            if(!isAtEnd()) current++;
            return previous();
        }

        bool match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (peek().type == type)
                {
                    advance();
                    return true;
                }
            }
            return false;
        }

        public List<Stmt> Parse()
        {
            // Parses program until at EOF
            List<Stmt> statements = new List<Stmt>();

            while (!isAtEnd())
            {
                statements.Add(ParseDeclaration());
            }
            
            return statements;
        }

        Stmt ParseDeclaration()
        {
            try
            {
                if (match(TokenType.FUN)) {
                    if(match(TokenType.LEFT_PAREN)) // lambda occuring in expression statement
                    {
                        putBack(2);
                        return ParseExpressionStatement();
                    }
                    Stmt func = ParseFuncDeclaration("function");
                    return func;
                }
                if (match(TokenType.VAR)) return ParseVarDeclaration();
                if (match(TokenType.CLASS)) return ParseClassDeclaration();
                return ParseStatement();
            } catch(ParseError error)
            {
                synchronize();
                return null;
            }
        }

        List<Token> ParseParameters()
        {
            List<Token> parameters = new List<Token>();
            if (peek().type != TokenType.RIGHT_PAREN)
            {
                do
                {
                    parameters.Add(consume(TokenType.IDENTIFIER, "Expect parameter name."));
                    if (parameters.Count >= 255)
                    {
                        Error(peek(), "Can't have more than 255 parameters.");
                    }
                } while (match(TokenType.COMMA));
            }
            return parameters;
        }

        Stmt.Function ParseFuncDeclaration(string kind)
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");
            consume(TokenType.LEFT_PAREN, "Expect '(' before " + kind + " parameter list.");

            List<Token> parameters = ParseParameters();

            consume(TokenType.RIGHT_PAREN, "Expect ')' after " + name + " parameter list.");

            // parse body
            consume(TokenType.LEFT_BRACE, "Expect '{' before " + name + " body.");
            List<Stmt> body = ParseBlock();

            return new Stmt.Function(name, parameters, body);
        }

        Stmt ParseVarDeclaration()
        {
            Token name = consume(TokenType.IDENTIFIER, "Expected variable name.");

            Expr value = null;
            if (match(TokenType.EQUAL))
            {
                value = ParseExpression();
            }
            consume(TokenType.SEMICOLON, "Expected semicolon after variable declaration.");
            return new Stmt.Var(name, value);
        }

        Stmt ParseClassDeclaration()
        {
            Token name = consume(TokenType.IDENTIFIER, "Expect class name.");
            consume(TokenType.LEFT_BRACE, "Expect '{' before class body.");
            List<Stmt.Function> methods = new List<Stmt.Function>();
            List<Stmt.Function> staticMethods = new List<Stmt.Function>();
            while(peek().type != TokenType.RIGHT_BRACE && !isAtEnd())
            {
                if(match(TokenType.CLASS)) staticMethods.Add(ParseFuncDeclaration("static method"));
                else methods.Add(ParseFuncDeclaration("method"));
            }
            consume(TokenType.RIGHT_BRACE, "Expect '}' after class body.");
            return new Stmt.Class(name, staticMethods, methods);
        }

        Stmt ParseStatement()
        {
            if(match(TokenType.PRINT))
            {
                return ParsePrintStatement();
            }
            if(match(TokenType.BREAK))
            {
                if(loopsCurrentlyParsing == 0)
                {
                    Error(previous(), "Break statement can only appear in loop.");
                }
                consume(TokenType.SEMICOLON, "Expect ';' after break.");
                return new Stmt.Break();
            }
            if(match(TokenType.RETURN))
            {
                return ParseReturnStatement();
            }
            if (match(TokenType.IF))
            {
                return ParseIfStatement();
            }
            if(match(TokenType.WHILE))
            {
                loopsCurrentlyParsing++;
                Stmt stmt = ParseWhileStatement();
                loopsCurrentlyParsing--;
                return stmt;
            }
            if(match(TokenType.FOR))
            {
                loopsCurrentlyParsing++;
                Stmt stmt = ParseForStatement();
                loopsCurrentlyParsing--;
                return stmt;
            }
            if (match(TokenType.LEFT_BRACE))
            {
                return new Stmt.Block(ParseBlock());
            }

            return ParseExpressionStatement();
        }

        Stmt ParsePrintStatement()
        {
            Expr value = ParseExpression();
            consume(TokenType.SEMICOLON, "Expected semicolon at the end of print statement.");
            return new Stmt.Print(value);
        }

        Stmt ParseReturnStatement()
        {
            Token keyword = previous();
            Expr value = null;
            if(peek().type != TokenType.SEMICOLON)
            {
                value = ParseExpression();
            }
            consume(TokenType.SEMICOLON, "Expect semicolon at the end of return statement.");
            return new Stmt.Return(keyword, value);
        }

        Stmt ParseExpressionStatement()
        {
            Expr expr = ParseExpression();
            consume(TokenType.SEMICOLON, "Expected semicolon at the end of expression statement.");
            return new Stmt.Expression(expr);
        }

        // This isn't called ParseBlockStatement() because it will later be used for parsing method bodies
        // which aren't Stmt.Block
        List<Stmt> ParseBlock()
        {
            List<Stmt> statements = new List<Stmt>();
            while (peek().type != TokenType.RIGHT_BRACE && !isAtEnd())
            {
                statements.Add(ParseDeclaration());
            }
            consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        Stmt ParseIfStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' before if statement condition.");
            Expr condition = ParseExpression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after if statement condition.");
            Stmt ifBody = ParseStatement();
            Stmt elseBody = null;
            if(match(TokenType.ELSE))
            {
                elseBody = ParseStatement();
            }
            return new Stmt.If(condition, ifBody, elseBody);
        }

        Stmt ParseWhileStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' before while statement condition.");
            Expr condition = ParseExpression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after while statement condition.");
            Stmt body = ParseStatement();
            return new Stmt.While(condition, body);
        }

        // This returns a block with an initializer statement and a while statement, a
        // dedicated Stmt.For class is not needed.
        Stmt ParseForStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' before for statement condition.");
            Stmt initializer = null;
            if (match(TokenType.VAR))
            {
                initializer = ParseVarDeclaration();
            }
            else if (!match(TokenType.SEMICOLON))
            {
                initializer = ParseExpressionStatement();
            }
            Expr condition = null;
            if(!match(TokenType.SEMICOLON))
            {
                condition = ParseExpression();
                consume(TokenType.SEMICOLON, "Expect ';' after for statement condition.");
            }
            Expr increment = null;
            if(!match(TokenType.RIGHT_PAREN))
            {
                increment = ParseExpression();
                consume(TokenType.RIGHT_PAREN, "Expect ')' after for statement condition.");
            }

            Stmt body = ParseStatement();
            if(increment != null)
            {
                body = new Stmt.Block(new List<Stmt>{ body, new Stmt.Expression(increment) });
            }
            if(condition == null) { condition = new Expr.Literal(true); }
            body = new Stmt.While(condition, body);

            if(initializer != null)
            {
                body = new Stmt.Block(new List<Stmt> { initializer, body });
            }

            return body;
        }

        // this made public so the REPL can evaluate expressions without requiring statements.
        // Ex: Entering (10 + 10) automatically prints 20 rather than having to type (print 10 + 10);
        public Expr ParseExpression()
        {
            return ParseCommaExpression();
        }

        // See comma operator in C/C++ (expr1, expr2, ..., exprN) exprN is the value of the whole expression
        Expr ParseCommaExpression()
        {
            return new Expr.ExprList(ParseExprList());
        }

        List<Expr> ParseExprList()
        {
            List<Expr> exprs = new List<Expr>();
            exprs.Add(ParseAssignmentExpression());
            while (match(TokenType.COMMA))
            {
                exprs.Add(ParseAssignmentExpression());
            }
            return exprs;
        }

        Expr ParseAssignmentExpression()
        {
            Expr expr = ParseTernaryExpression();

            if(match(TokenType.EQUAL))
            {
                Token equals = previous();
                Expr value = ParseAssignmentExpression();
                if(expr is Expr.Variable exprAsVariable)
                {
                    Token name = exprAsVariable.name;
                    return new Expr.Assignment(name, value);
                }
                if(expr is Expr.Get exprAsGet)
                {
                    return new Expr.Set(exprAsGet.instance, exprAsGet.name, value);
                }

                Error(equals, "Invalid assignment target.");
            }

            return expr;

            // The above code gives much better error messages than this code and works for future
            // types of assignments which don't involve a single identifier. Ex: point.x = 10;
            // Or: newPoint(x + 2, 0).y = 3; This is probably a bad example because it's modifying a temp object.
            // We need to parse the left hand side of that assignment, so the initial call to ParseTernaryExpr is necessary
            // This above method also avoids backtracking, which the below code does via putBack()

            /*if(match(TokenType.IDENTIFIER))
            {
                Token name = previous();
                if(match(TokenType.EQUAL))
                {
                    Expr value = ParseAssignmentExpression();
                    return new Expr.Assignment(name, value);
                }
                putBack();  // We don't have an assignment, put identifier "back"
            }
            return ParseCommaExpression();*/

        }

        Expr ParseTernaryExpression()
        {
            Expr condition = ParseOrExpression();

            if (match(TokenType.QMARK))
            {
                Expr conditionTrueValue = ParseTernaryExpression();
                consume(TokenType.COLON, "Expect ':' followed by else expression for ternary operator.");
                Expr conditionFalseValue = ParseTernaryExpression();
                return new Expr.Ternary(condition, conditionTrueValue, conditionFalseValue);
            }

            return condition;
        }

        Expr ParseOrExpression()
        {
            Expr lhs = ParseAndExpression();
            while(match(TokenType.OR))
            {
                Token orToken = previous();
                Expr rhs = ParseAndExpression();
                lhs = new Expr.Binary(lhs, orToken, rhs);
            }
            return lhs;
        }

        Expr ParseAndExpression()
        {
            Expr lhs = ParseEquality();
            while(match(TokenType.AND))
            {
                Token andToken = previous();
                Expr rhs = ParseEquality();
                lhs = new Expr.Binary(lhs, andToken, rhs);
            }
            return lhs;
        }

        Expr ParseEquality()
        {
            Expr lhs = ParseComparison();
            while(match(TokenType.EQUAL_EQUAL, TokenType.BANG_EQUAL))
            {
                Token op = previous();
                Expr rhs = ParseComparison();
                lhs = new Expr.Binary(lhs, op, rhs);
            }
            return lhs;
        }

        Expr ParseComparison()
        {
            Expr lhs = ParseTerm();
            while(match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = previous();
                Expr rhs = ParseTerm();
                lhs = new Expr.Binary(lhs, op, rhs);
            }
            return lhs;
        }

        Expr ParseTerm()
        {
            Expr lhs = ParseFactor();
            while(match(TokenType.PLUS, TokenType.MINUS))
            {
                Token op = previous();
                Expr rhs = ParseFactor();
                lhs = new Expr.Binary(lhs, op, rhs);
            }
            return lhs;
        }

        Expr ParseFactor()
        {
            Expr lhs = ParseUnary();
            while(match(TokenType.STAR, TokenType.SLASH))
            {
                Token op = previous();
                Expr rhs = ParseUnary();
                lhs = new Expr.Binary(lhs, op, rhs);
            }
            return lhs;
        }

        Expr ParseUnary()
        {
            if(match(TokenType.BANG, TokenType.MINUS))
            {
                Token op = previous();
                Expr right = ParseUnary();
                return new Expr.Unary(op, right);
            }
            return ParseCall();
        }

        Expr ParseCall()
        {
            Expr expr = ParsePrimary();

            while(true)
            {
                if(match(TokenType.LEFT_PAREN))
                {
                    expr = FinishCall(expr);
                }
                else if(match(TokenType.DOT))
                {
                    Token name = consume(TokenType.IDENTIFIER, "Expect property name after '.'.");
                    expr = new Expr.Get(expr, name);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        Expr FinishCall(Expr callee)
        {
            List<Expr> args = new List<Expr>();
            if(peek().type != TokenType.RIGHT_PAREN)
            {
                args.Add(ParseAssignmentExpression());
                while(match(TokenType.COMMA))
                {
                    args.Add(ParseAssignmentExpression());
                    if(args.Count >= 255)
                    {
                        Error(peek(), "Can't have more than 255 arguments.");
                    }
                }
            }

            Token paren = consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");
            return new Expr.Call(callee, paren, args);
        }

        Expr ParsePrimary()
        {
            if (match(TokenType.NUMBER, TokenType.STRING)) return new Expr.Literal(previous().data);

            if (match(TokenType.TRUE)) return new Expr.Literal(true);
            if (match(TokenType.FALSE)) return new Expr.Literal(false);
            if (match(TokenType.NIL)) return new Expr.Literal(null);
            if (match(TokenType.IDENTIFIER)) return new Expr.Variable(previous());
            if (match(TokenType.FUN)) return ParseLambda();
            if (match(TokenType.THIS)) return new Expr.This(previous());

            if (match(TokenType.LEFT_PAREN))
            {
                Expr expr = ParseExpression();
                consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            // Challenge 3: Add error productions to handle each binary operator appearing without a left-hand operand
            if(match(TokenType.PLUS, TokenType.STAR, TokenType.SLASH))
            {
                Lox.ReportError(previous().line, previous().column, "Expect left-hand operand for binary operator.");
                synchronize();
            }

            throw Error(peek(), "Expect expression.");
        }

        public Expr ParseLambda()
        {
            Token funKeyword = previous();
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'fun' in lambda.");

            List<Token> parameters = ParseParameters();

            consume(TokenType.RIGHT_PAREN, "Expect ')' after lambda parameter list.");

            // parse body
            consume(TokenType.LEFT_BRACE, "Expect '{' before lambda body.");
            List<Stmt> body = ParseBlock();

            return new Expr.Lambda(funKeyword, parameters, body);
        }
    }
}
