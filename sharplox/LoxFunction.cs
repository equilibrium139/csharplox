using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class LoxFunction : LoxCallable
    {
        private readonly Stmt.Function function;
        private readonly Environment closure;
        readonly bool isInitializer;

        public LoxFunction(Stmt.Function function, Environment closure, bool isInitializer)
        {
            this.function = function;
            this.closure = closure;
            this.isInitializer = isInitializer;
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
                environment.Define(args[i]);
            }

            try
            {
                interpreter.ExecuteBlock(function.body, environment);
            } catch(ReturnException returnValue)
            {
                if (isInitializer) return closure.Get(0); // return "this"
                return returnValue.value;
            }

            if (isInitializer) return closure.Get(0); // return "this"

            return null;
        }

        public LoxFunction Bind(LoxInstance instance)
        {
            Environment closureWithThis = new Environment(enclosing: closure);
            closureWithThis.Define(instance);
            return new LoxFunction(function, closure: closureWithThis, isInitializer);
        }

        public override string ToString()
        {
            return "<fn " + function.name.data + ">";
        }
    }
}
