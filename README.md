# TypedItem

This library is a set of method extensions other CosmosDb SQL Api to handle typed elements and soft delete in a container

## Why giving a type for giving a type in a CosmosDb Container.

This is wellknown best practice for Cosmos DB (see: https://youtu.be/bQBeTeYUrR8?t=1074). This library helps you to hide the complexity of the type management

## What are the constraints on my container?

TypedItem add specifics fields and methods in your documents:
* ```_pk``` the partition key ( for 1.X version of the SDK)
* ```_type``` the type of the item 
* ```_deleted``` the deletion status (SoftDelete)

Type and deletion status are handled by the extention methods

Starting with version 2.0, the library is compatible with the 3.X version of the SDK. The partition key is now managed by the fonction ```abstract PartitionKey GetPartitionKey()``` and the library doesn't need the ```_pk``` field anymore. This model let you manage the partition key as you want. Even for hierarchical partition key

### Before 2.0: How to add a type to a class ? 

You have to:
* inherits from ```TypedItemBase```
* add the attribute ```ItemType```
* seal the class

For example:


```csharp
 
    [ItemType("person")]
    public sealed class PersonItem: TypedItemBase
    {

        [JsonProperty("firstName")]
        public string FirstName { get; set; }
        
        [JsonProperty("lastName")]
        public string LastName { get; set; }
        
        [JsonProperty("birthdate")]
        public DateTime BirthDate { get; set; }
    }
```
In the container, the type will be ```person```


If you what to create type hierarchy you can do this:

```csharp
 
    [ItemType("event")]
    public class EventItem: TypedItemBase
    {

        [JsonProperty("date")]
        public DateTime Date { get; set; }              
    }

    [ItemType("phonecall")]
    public sealed class PhonecallItem: EventItem
    {

        [JsonProperty("date")]
        public int Duration { get; set; }
    }

    [ItemType("teamsmeeting")]
    public sealed class TeamsMeeting: EventItem
    {

        [JsonProperty("peoples")]
        public string[] Peoples { get; set; }
    }

```

You can create 2 differents items in the container:
* one with the type ```event.phonecall``` 
* one with the type ```event.teamsmeeting```

You can query all the events at once too with the method ```QueryTypedItemAsync```


### After 2.0: How to add a type to a class ?

You have to:
* create a root class from ```TypedItemBase``` to manage the partition key
* add the attribute ```ItemType```
* seal the class

For example:

```csharp
 
    public class ContainerItem : TypedItemBase
    {
        [JsonProperty("part")]
        public string Part { get; set; }
            
        public override PartitionKey GetPartitionKey()
        {
            return CreatePartitionKey(Part);
        }
    }
```

```csharp
 
    [ItemType("person")]
    public sealed class PersonItem: ContainerItem
    {

        [JsonProperty("firstName")]
        public string FirstName { get; set; }
        
        [JsonProperty("lastName")]
        public string LastName { get; set; }
        
        [JsonProperty("birthdate")]
        public DateTime BirthDate { get; set; }
    }
```
In the container, the type will be ```person```


If you what to create type hierarchy you can do this:

```csharp
 
    [ItemType("event")]
    public class EventItem: ContainerItem
    {

        [JsonProperty("date")]
        public DateTime Date { get; set; }              
    }

    [ItemType("phonecall")]
    public sealed class PhonecallItem: EventItem
    {

        [JsonProperty("date")]
        public int Duration { get; set; }
    }

    [ItemType("teamsmeeting")]
    public sealed class TeamsMeeting: EventItem
    {

        [JsonProperty("peoples")]
        public string[] Peoples { get; set; }
    }

```

You can create 2 differents items in the container:
* one with the type ```event.phonecall```
* one with the type ```event.teamsmeeting```

You can query all the events at once too with the method ```QueryTypedItemAsync```