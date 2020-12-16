using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class LoxLambda : LoxCallable
    {
        Expr.Lambda lambda;
        Environment closure;

        public LoxLambda(Expr.Lambda lambda, Environment closure)
        {
            this.lambda = lambda;
            this.closure = closure;
        }

        public int Arity()
        {
            return lambda.parameters.Count;
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            Environment environment = new Environment(enclosing: closure);
            for (int i = 0; i < args.Count; i++)
            {
                environment.Define(lambda.parameters[i], args[i]);
            }

            try
            {
                interpreter.ExecuteBlock(lambda.body, environment);
            }
            catch (ReturnException returnValue)
            {
                return returnValue.value;
            }

            return null;
        }
    }
}
