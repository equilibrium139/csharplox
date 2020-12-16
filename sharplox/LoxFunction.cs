using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class LoxFunction : LoxCallable
    {
        private readonly Stmt.Function function;
        private readonly Environment closure;

        public LoxFunction(Stmt.Function function, Environment closure)
        {
            this.function = function;
            this.closure = closure;
        }

        public int Arity()
        {
            return function.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            Environment environment = new Environment(enclosing: closure);
            for(int i = 0; i < args.Count; i++)
            {
                environment.Define(function.parameters[i], args[i]);
            }

            try
            {
                interpreter.ExecuteBlock(function.body, environment);
            } catch(ReturnException returnValue)
            {
                return returnValue.value;
            }

            return null;
        }

        public override string ToString()
        {
            return "<fn " + function.name.data + ">";
        }
    }
}
