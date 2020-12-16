using System;
using System.IO;
using System.Collections.Generic;

namespace sharplox
{
    class Lox
    {
        static bool hadError = false;
        static bool hadRuntimeError = false;
        static bool REPLMode = false;
        static readonly Interpreter interpreter = new Interpreter();

        public static void RuntimeError(RuntimeError error)
        {
            Console.Error.WriteLine(error.Message + $"\n[line {error.token.line}, character {error.token.column}]");
            hadRuntimeError = true;
        }

        public static void ReportError(Token token, string message)
        {
            if(token.type == TokenType.EOF)
            {
                ReportError(token.line, token.column, message + " at end");
            }
            else ReportError(token.line, token.column, message);
        }

        public static void ReportError(int line, int charNo, string message)
        {
            Console.Error.WriteLine($"Error: {message} on line {line}, character {charNo}.");
            hadError = true;
        }

        private static void Run(string source)
        {
            Scanner scanner = new Scanner(source);
            List<Token> tokens = scanner.scanTokens();
            Parser parser = new Parser(tokens);
            //if (REPLMode)
            //{
            //    bool enteredExpression = tokens.TrueForAll(x => x.type != TokenType.SEMICOLON && x.type != TokenType.LEFT_BRACE &&
            //                                                x.type != TokenType.RIGHT_BRACE);
            //    if(enteredExpression)
            //    {
            //        Expr expr = parser.ParseExpression();
            //        Console.WriteLine(interpreter.Evaluate(expr));
            //        return;
            //    }
            //}
            List<Stmt> statements = parser.Parse();
            if (hadError)
            {
                return;
            }
            interpreter.Interpret(statements);
        }

        private static void RunFile(string path)
        {
            string source = File.ReadAllText(path);
            Run(source);
            if(hadError) { System.Environment.Exit(65); }
            if(hadRuntimeError) { System.Environment.Exit(70); }
        }

        private static void RunPrompt()
        {
            while(true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == null) { break; }
                Run(line);
                hadError = false;
            }
        }

        static void Main(string[] args)
        {
            /*if (args.Length > 1)
            {
                Console.WriteLine("Usage: sharplox [script]");
                System.Environment.Exit(64);
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                REPLMode = true;
                RunPrompt();
            }*/

            Console.Write("Enter (1) name of file, (2) 'default', or (3) 'REPL':");
            string mode = Console.ReadLine();
            if(mode == "default")
            {
                string defaultPath = "C:\\Documents\\csharplox\\sharplox\\TestScripts\\Test.lox";
                RunFile(defaultPath);
            }
            else if(mode == "REPL")
            {
                REPLMode = true;
                RunPrompt();
            }
            else
            {
                RunFile(mode);
            }
        }
    }
}
