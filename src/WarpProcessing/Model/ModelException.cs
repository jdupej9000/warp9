using System;
using System.Collections.Generic;
using System.Text;

namespace Warp9.Model
{
    public class ModelException : Exception
    {
        public ModelException()
        {
        }

        public ModelException(string? message) : base(message)
        {
        }
    }
}
