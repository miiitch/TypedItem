# TypedItem — Instructions pour GitHub Copilot

## Description du projet

TypedItem est une librairie NuGet (.NET 10) qui fournit des extensions pour le SDK **Microsoft.Azure.Cosmos** afin de gérer des containers CosmosDB avec des items fortement typés.

Le concept central : chaque type d'item stocké dans CosmosDB est identifié par un champ `_type` persisté dans le document. Cela permet de stocker plusieurs types d'items dans un même container CosmosDB et de les récupérer de manière typée.

## Structure du projet

```
src/TypedItem/
  TypedItem.Lib/    — librairie principale (publiée sur NuGet)
  TypedItem.Tests/  — tests d'intégration avec CosmosDB
```

## Pattern central : `[ItemType]` + `TypedItemBase`

1. **Déclarer un type d'item** : hériter de `TypedItemBase` et décorer avec `[ItemType("nom")]`
2. **La classe doit être `sealed`** pour être utilisée dans les opérations d'écriture (`CreateTypedItemAsync`, `UpsertTypedItemAsync`, `ReplaceTypedItemAsync`)
3. **Implémenter `GetPartitionKey()`** (hérité de `ItemBase`)

```csharp
[ItemType("person")]
public sealed class PersonItem : TypedItemBase
{
    [JsonProperty("firstName")] public string? FirstName { get; set; }
    [JsonProperty("lastName")]  public string? LastName  { get; set; }

    public override PartitionKey GetPartitionKey() => CreatePartitionKey(LastName!);
}
```

### Hiérarchie de types

Il est possible de créer une hiérarchie : les classes parentes (non-sealed) servent à regrouper des types et à les requêter ensemble. Les classes sealed sont les types concrets.

```csharp
[ItemType("event")]
public abstract class EventItem : TypedItemBase { ... }

[ItemType("created")]
public sealed class EventCreatedItem : EventItem { ... }
// → _type stocké = "event.created"
```

## Namespace des extensions

Toutes les méthodes d'extension sont dans le namespace **`Microsoft.Azure.Cosmos`** (pas `TypedItem.Lib`) pour faciliter la découverte via IntelliSense.

## Sérialisation

Le projet utilise **Newtonsoft.Json** (via le SDK Cosmos) pour la sérialisation. Utiliser `[JsonProperty("nom")]` pour les propriétés sérialisées dans CosmosDB.

## Conventions de nommage

- Classes d'items : suffixe `Item` (ex: `PersonItem`, `EventCreatedItem`)
- Tests : suffixe `Tests` (ex: `TypedItemOperationsSinglePKTests`)
- Modèles de test dans le dossier `ItemModels/`

## Lancer les tests

Les tests d'intégration nécessitent **Docker** (utilisé via Testcontainers pour démarrer automatiquement l'émulateur CosmosDB Linux).

```bash
# Depuis la racine du repo
dotnet test src/TypedItem/TypedItem.sln
```

> ⚠️ Le premier lancement peut être lent (pull de l'image Docker CosmosDB emulator ~2 Go).

## Opérations disponibles

| Méthode | Description |
|---|---|
| `CreateTypedItemAsync<T>` | Crée un item (génère un ID) |
| `UpsertTypedItemAsync<T>` | Upsert un item (génère un ID si absent) |
| `ReplaceTypedItemAsync<T>` | Remplace un item existant |
| `ReadTypedItemAsync<T>` | Lit un item par ID (respecte le soft-delete) |
| `SoftDeleteTypedItemAsync<T>` | Suppression logique (pose `_deleted: true`) |
| `QueryTypedItemAsync<TFrom, TTo>` | Requête LINQ typée avec pagination |
| Variantes `TransactionalBatch` | `CreateTypedItem`, `UpsertTypedItem`, etc. |
