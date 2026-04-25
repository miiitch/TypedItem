using System;
using Microsoft.Azure.Cosmos;
using NFluent;
using TypedItem.Lib;
using TypedItem.Tests.ItemModels;
using Xunit;

namespace TypedItem.Tests;

public class TypedItemHelperUnitTests
{
    [Fact]
    public void generate_id_starts_with_item_type_followed_by_dash()
    {
        var id = TypedItemHelper<PersonItem>.GenerateId();

        Check.That(id).StartsWith("person-");
    }

    [Fact]
    public void generate_id_is_unique_across_calls()
    {
        var id1 = TypedItemHelper<PersonItem>.GenerateId();
        var id2 = TypedItemHelper<PersonItem>.GenerateId();

        Check.That(id1).IsNotEqualTo(id2);
    }

    [Fact]
    public void generate_id_uses_full_dotted_type_as_prefix_for_hierarchy()
    {
        var id = TypedItemHelper<TypeAEventItem>.GenerateId();

        Check.That(id).StartsWith("event.typeA-");
    }

    [Fact]
    public void has_same_type_returns_true_when_item_has_matching_type()
    {
        var item = new PersonItem { ItemType = TypedItemHelper<PersonItem>.ItemType };

        Check.That(TypedItemHelper<PersonItem>.HasSameType(item)).IsTrue();
    }

    [Fact]
    public void has_same_type_returns_false_when_item_has_different_type()
    {
        var item = new PersonItem { ItemType = "other.type" };

        Check.That(TypedItemHelper<PersonItem>.HasSameType(item)).IsFalse();
    }

    [Fact]
    public void has_same_type_is_case_sensitive()
    {
        var item = new PersonItem { ItemType = "Person" };

        Check.That(TypedItemHelper<PersonItem>.HasSameType(item)).IsFalse();
    }

    [Fact]
    public void has_same_type_returns_false_when_item_type_is_null()
    {
        var item = new PersonItem { ItemType = null };

        Check.That(TypedItemHelper<PersonItem>.HasSameType(item)).IsFalse();
    }

    [Fact]
    public void partition_key_null_is_null_or_none()
    {
        Check.That(PartitionKey.Null.IsNullOrNone()).IsTrue();
    }

    [Fact]
    public void partition_key_none_is_null_or_none()
    {
        Check.That(PartitionKey.None.IsNullOrNone()).IsTrue();
    }

    [Fact]
    public void valid_string_partition_key_is_not_null_or_none()
    {
        Check.That(new PartitionKey("value").IsNullOrNone()).IsFalse();
    }

    [Fact]
    public void soft_delete_throws_argument_null_exception_when_item_id_is_null()
    {
        Container container = null!;
        var item = new PersonItem { Id = null, Part = "p1" };

        Check.ThatCode(() => _ = container.SoftDeleteTypedItemAsync(item))
            .Throws<ArgumentNullException>();
    }

    [Fact]
    public void soft_delete_throws_argument_exception_when_partition_key_is_null_or_none()
    {
        Container container = null!;
        var item = new PersonItem { Id = "some-id" };

        Check.ThatCode(() => _ = container.SoftDeleteTypedItemAsync(item))
            .Throws<ArgumentException>();
    }

    [Fact]
    public void soft_delete_throws_argument_exception_when_item_is_already_deleted()
    {
        Container container = null!;
        var item = new PersonItem { Id = "some-id", Part = "p1", Deleted = true };

        Check.ThatCode(() => _ = container.SoftDeleteTypedItemAsync(item))
            .Throws<ArgumentException>();
    }
}
