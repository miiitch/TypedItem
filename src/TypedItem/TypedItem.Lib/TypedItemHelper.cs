using System;

namespace TypedItem.Lib
{
    public static class TypedItemHelper<T> where T : TypedItemBase, new()
    {
        public static string DocumentType { get; } = new T().DocumentType;
        
        public static bool HasSameType<U>(U u) where U : TypedItemBase => string.CompareOrdinal(DocumentType, u.DocumentType) == 0;

        public static string GenerateId() => $"{DocumentType}-{Guid.NewGuid():N}";
    }
}