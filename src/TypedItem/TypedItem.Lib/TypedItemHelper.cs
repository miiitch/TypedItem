using System;

namespace TypedItem.Lib
{
    public static class TypedItemHelper<TItemType> where TItemType : TypedItemBase, new()
    {
        
        // ReSharper disable once MemberCanBePrivate.Global
        public static string ItemType => GetDocumentType();

        private static string? _computedDocumentType = null;
        private static string GetDocumentType()
        {
            _computedDocumentType ??= ComputeDocumentType(typeof(TItemType));
            
            return _computedDocumentType;
            
            string ComputeDocumentType(Type type)
            {
                var attributes = type.GetCustomAttributes(typeof(ItemTypeAttribute), false) as ItemTypeAttribute[];
                if (attributes is null ||
                    attributes.Length == 0)
                {
                    throw new TypedItemException("ItemType attribute is missing");
                }

                var name = attributes[0].Name;
                
                var parent = type.BaseType!;

                if (parent == typeof(TypedItemBase))
                {
                    return name;
                }

                return ComputeDocumentType(parent) + "." + name;
            }
        }

        public static bool IsFinal => typeof(TItemType).IsSealed;

        public static bool HasSameType<TItemTypeToCompareU>(TItemTypeToCompareU u) where TItemTypeToCompareU : TypedItemBase => string.CompareOrdinal(ItemType, u.ItemType) == 0;

        public static string GenerateId() => $"{ItemType}-{Guid.NewGuid():N}";
        
        public static void PrepareItem(TItemType item, bool generateId = false)
        {
            if (!IsFinal)
            {
                throw new TypedItemException("Cannot fill item data for non final object");
            }
            var itemType = ItemType;
            if (item.ItemType is null)
            {
                item.ItemType = itemType;
            }
            else if (string.Compare(itemType, item.ItemType, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new TypedItemException(
                    $"object of type {item.GetType().FullName} as a type value of {item.ItemType} but {itemType} is required");
            }
            if (generateId)
            {
                item.Id ??= GenerateId();
            }
        }
    }
}