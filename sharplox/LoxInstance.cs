using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class LoxInstance
    {
        LoxClass loxClass;
        // TODO change to list
        readonly Dictionary<string, object> fields = new Dictionary<string, object>();

        public LoxInstance(LoxClass loxClass)
        {
            this.loxClass = loxClass;
        }

        public object Get(Token name)
        {
            // Fields shadow properties
            if(fields.TryGetValue(name.lexeme, out object value))
            {
                return value;
            }

            // Property
            LoxFunction method = loxClass.FindMethod(name.lexeme);
            if(method != null)
            {
                return method.Bind(this);
            }

            throw new RuntimeError(name, "Undefined property '" + name.data + "'.");
        }

        public void Set(Token name, object value)
        {
            fields[name.lexeme] = value;
        } 

        public override string ToString()
        {
            return loxClass.name + " instance";
        }
    }
}
