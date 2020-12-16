using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    interface LoxCallable
    {
        public int Arity();
        public object Call(Interpreter interpreter, List<object> args);
    }
}
