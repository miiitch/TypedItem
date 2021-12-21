using System;

namespace TypedItem.Lib
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ItemTypeAttribute : Attribute
    {
        public string Name { get; }
        public ItemTypeAttribute(string name)
        {
            Name = name;
        }
    }
}