using System;

namespace TypedItem.Lib
{
    public class TypedItemException: Exception
    {
        public TypedItemException(string message) : base(message)
        {
        }
    }
}
