using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class LoxClass : LoxInstance, LoxCallable 
    {
        public readonly string name;
        readonly Dictionary<string, LoxFunction> methods;
        LoxFunction initializer;

        public LoxClass(string name, Dictionary<string, LoxFunction> methods)
            :base(null)
        {
            this.name = name;
            this.methods = methods;
            initializer = FindMethod("init");
        }

        public int Arity()
        {
            return initializer == null ? 0 : initializer.Arity();
        }

        public object Call(Interpreter interpreter, List<object> args)
        {
            LoxInstance instance = new LoxInstance(this);
            if(initializer != null)
            {
                initializer.Bind(instance).Call(interpreter, args);
            }
            return instance;
        }

        public LoxFunction FindMethod(string name)
        {
            if(methods.TryGetValue(name, out var method))
            {
                return method;
            }

            return null;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
