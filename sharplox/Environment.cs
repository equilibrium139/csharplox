using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace sharplox
{
    class Environment
    {
        public readonly Environment enclosing; // resort to this scope if a variable cannot be found in this
        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public Environment()
        {
            this.enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        // This function only used to define globals
        public void Define(string name, object value)
        {
            bool success = values.TryAdd(name, value);
            Debug.Assert(success, "Global variable " + " name was already defined.");
        }

        public void Define(Token name, object value)
        {
            string nameStr = (string)name.data;
            if (!values.TryAdd(nameStr, value))
            {
                throw new RuntimeError(name, "Variable \"" + nameStr + "\" was already declared.");
            }
        }

        public object Get(Token name)
        {
            string varName = (string)(name.data);
            if(values.TryGetValue(varName, out object value))
            {
                return value;
            }
            if(enclosing != null)
            {
                return enclosing.Get(name);
            }
            throw new RuntimeError(name, "Undefined variable \"" + varName + "\".");
        }

        public object GetAt(string name, int distance)
        {
            Environment ancestor = Ancestor(distance);
            Debug.Assert(ancestor.values.ContainsKey(name));
            return ancestor.values[name];
        }

        private Environment Ancestor(int depth)
        {
            Environment environment = this;
            for(int i = 0; i < depth; i++)
            {
                environment = this.enclosing;
            }
            return environment;
        }

        public void Assign(Token name, object value)
        {
            string varName = (string)name.data;
            if(values.ContainsKey(varName))
            {
                values[varName] = value;
            }
            else if(enclosing != null)
            {
                enclosing.Assign(name, value);
            }
            else
            {
                throw new RuntimeError(name, "Undefined variable \"" + varName + "\".");
            }
        }

        public void AssignAt(Token name, object value, int depth)
        {
            string varName = (string)name.data;
            Environment ancestor = Ancestor(depth);
            Debug.Assert(ancestor.values.ContainsKey(varName));
            ancestor.values[varName] = value;
        }
    }
}
