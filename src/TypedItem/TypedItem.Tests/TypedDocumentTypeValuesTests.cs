﻿using System;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;

namespace TypedItem.Tests
{
    public class TypedDocumentTypeHelperTests
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
            Check.That(TypedItemHelper<AppEvent>.ItemType).IsEqualTo("event");
            Check.That(TypedItemHelper<AppEvent>.IsFinal).IsFalse();
        }

        [Fact]
        public void class_that_inherit_from_another_class_is_valid_if_an_event_type_is_present_at_each_level()
        {
            Check.That(TypedItemHelper<TypeAEvent>.ItemType).IsEqualTo("event.typeA");
            Check.That(TypedItemHelper<TypeAEvent>.IsFinal).IsTrue();
        }

        [Fact]
        public void class_with_parent_class_without_type_attribute_is_invalid()
        {
            Check.ThatCode(() => TypedItemHelper<TypeB1Event>.ItemType).Throws<TypedItemException>();
           
        }

        [Fact]
        public void class_without_type_attribute_is_invalid()
        {
            Check.ThatCode(() => TypedItemHelper<CustomDocument>.ItemType).Throws<TypedItemException>();
        }

        [Fact]
        public void root_class_is_not_valid_for_type_computation()
        {
            Check.ThatCode(() => TypedItemHelper<TypedItemBase>.ItemType).Throws<TypedItemException>();
        }
        
        [Fact]
        public void cannot_prepare_item_for_a_non_final_object()
        {
            var appEvent = new AppEvent();

            Check.ThatCode(() => TypedItemHelper<AppEvent>.PrepareItem(appEvent)).Throws<TypedItemException>();
        }
        
        [Fact]
        public void cannot_prepare_item_for_a_final_object_with_invalid_type_defined()
        {
            var appEvent = new TypeAEvent()
            {
                ItemType = "toto"
            };

            Check.ThatCode(() => TypedItemHelper<AppEvent>.PrepareItem(appEvent)).Throws<TypedItemException>();
        }
        
        [Fact]
        public void prepare_item_for_a_final_object_sets_the_valid_type()
        {
            var appEvent = new TypeAEvent();
            TypedItemHelper<TypeAEvent>.PrepareItem(appEvent);

            Check.That(appEvent.ItemType).IsEqualTo("event.typeA");
            Check.That(appEvent.Id).IsNull();
        }
        
        [Fact]
        public void prepare_item_for_a_final_object_sets_the_valid_type_and_id()
        {
            var appEvent = new TypeAEvent();
            TypedItemHelper<TypeAEvent>.PrepareItem(appEvent,generateId:true);

            Check.That(appEvent.ItemType).IsEqualTo("event.typeA");
            Check.That(appEvent.Id).Not.IsNullOrEmpty();
        }
    }
}