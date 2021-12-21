using System;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;

namespace TypedItem.Tests
{
    public class TypedItemTypeHelperTests
    {
        [Fact]
        public void sealed_class_that_inherits_from_TypedItemBase_is_valid()
        {
            Check.That(TypedItemHelper<PersonItem>.ItemType).IsEqualTo("person");
            Check.That(TypedItemHelper<PersonItem>.IsFinal).IsTrue();
        }
        
        [Fact]
        public void non_class_that_inherits_from_TypedItemBase_is_valid_but_marked_not_final()
        {
            Check.That(TypedItemHelper<EventItem>.ItemType).IsEqualTo("event");
            Check.That(TypedItemHelper<EventItem>.IsFinal).IsFalse();
        }

        [Fact]
        public void class_that_inherit_from_another_class_is_valid_if_an_event_type_is_present_at_each_level()
        {
            Check.That(TypedItemHelper<TypeAEventItem>.ItemType).IsEqualTo("event.typeA");
            Check.That(TypedItemHelper<TypeAEventItem>.IsFinal).IsTrue();
        }
        
        [Fact]
        public void class_that_inherit_from_another_class_is_valid_if_an_event_type_is_not_present_at_each_level()
        {
            Check.That(TypedItemHelper<TypeB1EventItem>.ItemType).IsEqualTo("event.typeB1");
            Check.That(TypedItemHelper<TypeB1EventItem>.IsFinal).IsTrue();
        }
        
        
        [Fact]
        public void class_without_type_attribute_is_invalid()
        {
            Check.ThatCode(() => TypedItemHelper<CustomAndInvalidItem>.ItemType).Throws<TypedItemException>();
        }

        [Fact]
        public void root_class_is_not_valid_for_type_computation()
        {
            Check.ThatCode(() => TypedItemHelper<TypedItemBase>.ItemType).Throws<TypedItemException>();
        }
        
        [Fact]
        public void cannot_prepare_item_for_a_non_final_object()
        {
            var appEvent = new EventItem();

            Check.ThatCode(() => TypedItemHelper<EventItem>.PrepareItem(appEvent)).Throws<TypedItemException>();
        }
        
        [Fact]
        public void cannot_prepare_item_for_a_final_object_with_invalid_type_defined()
        {
            var appEvent = new TypeAEventItem()
            {
                ItemType = "toto"
            };

            Check.ThatCode(() => TypedItemHelper<EventItem>.PrepareItem(appEvent)).Throws<TypedItemException>();
        }
        
        [Fact]
        public void prepare_item_for_a_final_object_sets_the_valid_type()
        {
            var appEvent = new TypeAEventItem();
            TypedItemHelper<TypeAEventItem>.PrepareItem(appEvent);

            Check.That(appEvent.ItemType).IsEqualTo("event.typeA");
            Check.That(appEvent.Id).IsNull();
        }
        
        [Fact]
        public void prepare_item_for_a_final_object_sets_the_valid_type_and_id()
        {
            var appEvent = new TypeAEventItem();
            TypedItemHelper<TypeAEventItem>.PrepareItem(appEvent,generateId:true);

            Check.That(appEvent.ItemType).IsEqualTo("event.typeA");
            Check.That(appEvent.Id).Not.IsNullOrEmpty();
        }
    }
}
