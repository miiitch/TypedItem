using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace TypedItem.Lib
{
    public static class TypedItemHelper<TItemType> where TItemType : TypedItemBase, new()
    {
        
        // ReSharper disable once MemberCanBePrivate.Global
        public static string ItemType => GetItemType();

        // ReSharper disable once StaticMemberInGenericType
        private static string? _computedItemType = null;
        private static string GetItemType()
        {
            _computedItemType ??= typeof(TItemType).GetItemType();
            if (_computedItemType is null)
            {
                throw new TypedItemException("ItemType attribute is missing");
            }
            
            return _computedItemType;
        }

        public static bool IsFinal => typeof(TItemType).IsSealed;

        public static bool HasSameType<TItemTypeToCompareU>(TItemTypeToCompareU u) where TItemTypeToCompareU : TypedItemBase => string.CompareOrdinal(ItemType, u.ItemType) == 0;

        public static string GenerateId() => $"{ItemType}-{Guid.NewGuid():N}";
        
        public static void PrepareItem(TItemType item, bool generateId = false)
        {
            if (!IsFinal)
            {
                throw new TypedItemException("Cannot fill item data for non sealed class");
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

    internal static class InternalHelpers
    {
        internal static string? GetItemType(this Type type)
        {
            var attribute = GetItemTypeAttribute(type);
                
            if (attribute is null)
            {
                return null;
            }
                
            var name = attribute.Name;

            var parentInfo = GetParentWithItemType(type.BaseType);

            while (parentInfo is not null)
            {
                var (parentType, parentName) = parentInfo.Value;
                name = parentName + "." + name;
                parentInfo = GetParentWithItemType(parentType?.BaseType);
            }
                
            return name;
            
            ItemTypeAttribute? GetItemTypeAttribute(Type currentType)
            {
                var attributes = currentType.GetCustomAttributes(typeof(ItemTypeAttribute), false) as ItemTypeAttribute[];

                return attributes is null or { Length: 0 } ? null : attributes[0];
            }
            
            (Type, string)? GetParentWithItemType(Type? currentType)
            {
                if (currentType is null)
                {
                    return null;
                }

                if (currentType == typeof(TypedItemBase))
                {
                    return null;
                }

                var attribute = GetItemTypeAttribute(currentType);
                if (attribute is null)
                {
                    return GetParentWithItemType(currentType.BaseType);
                }

                var parentName = attribute.Name;

                return (currentType, parentName);
            }
        }
    }
    
}