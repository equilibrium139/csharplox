using System;
using System.Collections.Generic;

namespace sharplox
{
    class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int start = 0;
        private int current = 0;
        private int line = 1;
        private int column = 1;

        private static Dictionary<string, TokenType> keywords;

        static Scanner()
        {
            keywords = new Dictionary<string, TokenType>
            {
                {"and", TokenType.AND },
                {"break", TokenType.BREAK },
                {"class", TokenType.CLASS },
                {"else", TokenType.ELSE },
                {"false", TokenType.FALSE },
                {"fun", TokenType.FUN},
                {"for", TokenType.FOR},
                {"if", TokenType.IF },
                {"nil", TokenType.NIL},
                {"or", TokenType.OR },
                {"print", TokenType.PRINT },
                {"return", TokenType.RETURN },
                {"super", TokenType.SUPER },
                {"this", TokenType.THIS },
                {"true", TokenType.TRUE },
                {"var", TokenType.VAR },
                {"while", TokenType.WHILE }
            };
        }

        public Scanner(string source)
        {
            this.source = source;
        }

        public List<Token> scanTokens()
        {
            while(!isAtEnd())
            {
                start = current;
                char currChar = advance();
                ProcessTokenStartingWith(currChar);
            }

            AddToken(TokenType.EOF);

            return tokens;
        }

        private void ProcessTokenStartingWith(char currChar)
        {
            switch (currChar)
            {
                // Single-character tokens
                case '(':
                    AddToken(TokenType.LEFT_PAREN);
                    break;
                case ')':
                    AddToken(TokenType.RIGHT_PAREN);
                    break;
                case '{':
                    AddToken(TokenType.LEFT_BRACE);
                    break;
                case '}':
                    AddToken(TokenType.RIGHT_BRACE);
                    break;
                case ',':
                    AddToken(TokenType.COMMA);
                    break;
                case '.':
                    AddToken(TokenType.DOT);
                    break;
                case ';':
                    AddToken(TokenType.SEMICOLON);
                    break;
                case '?':
                    AddToken(TokenType.QMARK);
                    break;
                case ':':
                    AddToken(TokenType.COLON);
                    break;
                // 1-2 character tokens
                case '!':
                    AddToken(match('=') ? TokenType.BANG_EQUAL : TokenType.BANG);
                    break;
                case '=':
                    AddToken(match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL);
                    break;
                case '>':
                    AddToken(match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER);
                    break;
                case '<':
                    AddToken(match('=') ? TokenType.LESS_EQUAL : TokenType.LESS);
                    break;
                case '+':
                    AddToken(match('=') ? TokenType.PLUS_EQUAL : TokenType.PLUS);
                    break;
                case '-':
                    AddToken(match('=') ? TokenType.MINUS_EQUAL : TokenType.MINUS);
                    break;
                case '*':
                    AddToken(match('=') ? TokenType.STAR_EQUAL : TokenType.STAR);
                    break;
                case '/':
                    if (match('='))
                    {
                        AddToken(TokenType.SLASH_EQUAL);
                    }
                    else if (match('/'))
                    {
                        while (!isAtEnd() && peek() != '\n')
                        {
                            advance();
                        }
                    }
                    else
                    {
                        AddToken(TokenType.SLASH);
                    }
                    break;
                default:
                    if (isDigit(currChar))
                    {
                        LexNumber();
                    }
                    else if (currChar == '"')
                    {
                        LexString();
                    }
                    else if (Char.IsWhiteSpace(currChar))
                    {
                        if (currChar == '\n')
                        {
                            line++;
                            column = 1;
                        }
                    }
                    else if (isAlpha(currChar) || currChar == '_')
                    {
                        LexKeywordOrIdentifier();
                    }
                    else
                    {
                        Lox.ReportError(line, column, "There are no tokens that start with " + currChar + ".");
                    }
                    break;
            }
        }

        private void LexKeywordOrIdentifier()
        {
            char currChar = peek();
            while(isAlphaNumeric(currChar) || currChar == '_')
            {
                advance();
                currChar = peek();
            }
            string kwOrIdentifier = source.Substring(start, current - start);
            if(keywords.TryGetValue(kwOrIdentifier, out TokenType tokenType))
            {
                // No need to store the string containing the keyword because it seems redundant.
                // We already know that "and" corresponds to TokenType.AND.
                AddToken(tokenType);
            }
            else
            {
                AddToken(TokenType.IDENTIFIER, kwOrIdentifier);
            }
        }

        private void LexNumber()
        {
            while(isDigit(peek()))
            {
                advance();
            }
            
            // Method calls are allowed on numbers: 123.abs(). This means that we can't always assume that 
            // a dot following a number indicates a fractional part. 
            if(peek() == '.' && isDigit(peekNext()))
            {
                // Consume '.'
                advance();
                while(isDigit(peek()))
                {
                    advance();
                }
            }

            double value = Double.Parse(source.Substring(start, current - start));
            AddToken(TokenType.NUMBER, value);
        }

        private void LexString()
        {
            while (peek() != '"' && !isAtEnd())
            {
                advance();
            }

            if(isAtEnd())
            {
                Lox.ReportError(line, column, "String literal must end with double quotes.");
                return;
            }

            int literalStart = start + 1;
            string literal = this.source.Substring(literalStart, current - literalStart);
            AddToken(TokenType.STRING, literal);

            // closing quote
            advance();
        }

        private bool isDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool isAlphaNumeric(char c)
        {
            return isAlpha(c) || isDigit(c);
        }

        private bool isAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z');
        }

        private void AddToken(TokenType type)
        {
            AddToken(type, null);
        }

        private void AddToken(TokenType type, object data)
        {
            tokens.Add(new Token(type, data, line, column));
        }

        private bool isAtEnd()
        {
            return current >= source.Length;
        }

        private char advance()
        {
            column++;
            current++;
            return source[current - 1];
        }

        // If peek() == c then advance.
        private bool match(char c)
        {
            if(peek() == c)
            {
                current++;
                return true;
            }
            return false;
        }

        private char peek()
        {
            return !isAtEnd() ? source[current] : '\0';
        }

        private char peekNext()
        {
            return current + 1 < source.Length ? source[current + 1] : '\0';
        }
    }
}
