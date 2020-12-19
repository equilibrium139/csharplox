using System;
using System.Collections.Generic;
using System.IO;

namespace generate_ast
{
    class Program
    {
        static List<string> exprDerivedTypes;
        static List<string> stmtDerivedTypes;
        static Program()
        {
            exprDerivedTypes = new List<string>
            {
                "Binary : Expr left, Token op, Expr right",
                "Unary : Token op, Expr expr",
                "Literal : object value",
                "Grouping : Expr expr",
                "Ternary : Expr condition, Expr conditionTrueValue, Expr conditionFalseValue",
                "Variable : Token name",
                "Assignment : Token name, Expr value",
                "ExprList   : List<Expr> exprs",
                "Call       : Expr callee, Token paren, List<Expr> args",
                "Lambda     : Token funKeyword, List<Token> parameters, List<Stmt> body",
                "Get        : Expr instance, Token name",   // var x = person.height;
                "Set        : Expr instance, Token name, Expr value", // person.height = 6.0;
                "This       : Token keyword",
            };

            stmtDerivedTypes = new List<string>
            {
                "Expression : Expr expression",
                "Print      : Expr expression",     
                "Var        : Token name, Expr intializer",
                "Block      : List<Stmt> statements",
                "If         : Expr condition, Stmt ifBody, Stmt elseBody",
                "While      : Expr condition, Stmt body", 
                "Break      :",
                "Function   : Token name, List<Token> parameters, List<Stmt> body",
                "Return     : Token keyword, Expr value",
                "Class      : Token name, List<Stmt.Function> staticMethods, List<Stmt.Function> methods"
            };
        }

        static void DefineVisitorInterface(StreamWriter writer, string baseName, List<string> derivedTypes)
        {
            writer.WriteLine("public interface IVisitor<T>");
            writer.WriteLine("{");
            foreach(string type in derivedTypes)
            {
                string typeName = type.Split(':')[0].Trim();
                writer.WriteLine("T visit" + typeName + baseName + "(" + typeName + " " + baseName.ToLower() + ");");
            }
            writer.WriteLine("}");
        }

        static void DefineType(StreamWriter writer, string name, string members, string baseName)
        {
            writer.WriteLine("public class " + name + " : " + baseName);
            writer.WriteLine("{");

            // Member variables
            var membersSplit = members.Split(',', StringSplitOptions.RemoveEmptyEntries);
            // Remove leading and trailing whitespace from all member variables
            for(int i = 0; i < membersSplit.Length; i++)
            {
                membersSplit[i] = membersSplit[i].Trim();
            }
            foreach(string member in membersSplit)
            {
                writer.WriteLine("public " + member.Trim() + ";");
            }

            // Constructor
            writer.WriteLine("public " + name + "(" + members + ")");
            writer.WriteLine("{");
            foreach(string member in membersSplit)
            {
                string memberName = member.Split(' ')[1];
                writer.WriteLine("this." + memberName + " = " + memberName + ";");
            }
            writer.WriteLine("}");  // Ctor }
            writer.WriteLine("public override T accept<T>(IVisitor<T> visitor)");
            writer.WriteLine("{");
            writer.WriteLine("return visitor.visit" + name + baseName + "(this);");
            writer.WriteLine("}");  // accept<T> }
            writer.WriteLine("}");  // Class }
        }

        static void DefineAST(string path, string baseName, List<string> derivedTypes)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("using System.Collections.Generic;");
                writer.WriteLine("namespace sharplox");
                writer.WriteLine("{");
                writer.WriteLine("abstract class " + baseName);
                writer.WriteLine("{");
                DefineVisitorInterface(writer, baseName, derivedTypes);
                writer.WriteLine("public abstract T accept<T>(IVisitor<T> visitor);");
                writer.WriteLine();
                foreach (string type in derivedTypes)
                {
                    var typeSplit = type.Split(':');
                    string name = typeSplit[0].Trim();
                    string members = typeSplit[1];
                    DefineType(writer, name, members, baseName);
                    writer.WriteLine();
                }
                writer.WriteLine("}");
                writer.WriteLine("}");
            }
        }

        static void Main(string[] args)
        {
            //if(args.Length != 1)
            //{
            //    Console.WriteLine("Usage: generate_ast <outputdir>");
            //    Environment.Exit(65);
            //}

            string dir = "C:\\Documents\\csharplox\\sharplox";

            string exprBaseName = "Expr";
            string exprPath = $"{dir}\\{exprBaseName}.cs";
            DefineAST(exprPath, exprBaseName, exprDerivedTypes);

            string stmtBaseName = "Stmt";
            string stmtPath = $"{dir}\\{stmtBaseName}.cs";
            DefineAST(stmtPath, stmtBaseName, stmtDerivedTypes);
        }
    }
}
