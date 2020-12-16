namespace sharplox
{
    enum TokenType
    {
        // Single character tokens
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE,
        COMMA, DOT, SEMICOLON, QMARK, COLON,

        // 1-2 character tokens
        BANG, BANG_EQUAL,
        EQUAL, EQUAL_EQUAL,
        GREATER, GREATER_EQUAL,
        LESS, LESS_EQUAL,
        PLUS, PLUS_EQUAL,
        MINUS, MINUS_EQUAL, 
        STAR, STAR_EQUAL,
        SLASH, SLASH_EQUAL, 

        // Literals
        IDENTIFIER, STRING, NUMBER,

        // Keywords
        AND, BREAK, CLASS, ELSE, FALSE, FUN, FOR, IF, NIL, OR,
        PRINT, RETURN, SUPER, THIS, TRUE, VAR, WHILE,

        EOF
    }

    class Token
    {
        public readonly TokenType type;
        public readonly object data;
        public readonly int line;
        public readonly int column;

        public Token(TokenType type, object data, int line, int column)
        {
            this.type = type; this.data = data; this.line = line; this.column = column;
        }

        public override string ToString() 
        {
            return type.ToString() + " " + data?.ToString();
        }
    }
}
