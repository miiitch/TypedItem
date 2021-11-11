using System;

namespace TypedItem.Lib
{
    public static class TypedItemHelper<TItemType> where TItemType : TypedItemBase, new()
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public static string DocumentType { get; } = new TItemType().DocumentType;
        
        public static bool HasSameType<TItemTypeToCompareU>(TItemTypeToCompareU u) where TItemTypeToCompareU : TypedItemBase => string.CompareOrdinal(DocumentType, u.DocumentType) == 0;

        public static string GenerateId() => $"{DocumentType}-{Guid.NewGuid():N}";
    }
}