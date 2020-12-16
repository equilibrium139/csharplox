using System;
using System.Collections.Generic;
using System.Text;

namespace sharplox
{
    class ReturnException : Exception
    {
        public object value;
    }
}
