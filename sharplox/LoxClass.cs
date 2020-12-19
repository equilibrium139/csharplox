using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class LoxClass : LoxCallable 
    {
        public readonly string name;
        readonly Dictionary<string, LoxFunction> methods;
        readonly Dictionary<string, LoxFunction> staticMethods;
        LoxFunction initializer;

        public LoxClass(string name, Dictionary<string, LoxFunction> staticMethods, Dictionary<string, LoxFunction> methods)
        {
            this.name = name;
            this.staticMethods = staticMethods;
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

        public LoxFunction FindStaticMethod(string name)
        {
            if(staticMethods.TryGetValue(name, out var method))
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
