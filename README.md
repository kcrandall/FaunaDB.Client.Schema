# FaunaDB.Client.Schema
Extension library to FaunaDB.Client - allows documenting of schema and one line database creation.

## Documenting classes
You can one FaunaCollection attribute and as many FaunaIndexes attributes to your class. Supports all parameters except source which is infered by the class they are attached to.
```csharp
[FaunaIndex(
      name: "collection_by_email",
      terms: new object[]
      {
          nameof(Email)
      },
      values: new object[]
      {
          FaunaValueType.Ref
      })]
[FaunaIndex(
    name: "collection_ordered_by_date",
    terms: new object[]
    {
        FaunaTermType.Ref,
    },
    values: new object[]
    {
        nameof(Date),
        FaunaValueType.Ref
    })]
[FaunaCollection("collection")]
public class Collection : Base
{
  [FaunaField("email")]
  public RefV Email { get; set; }
  
  [FaunaField("date")]
  public DateTime Date { get; set; }
}
```

## Creating a new database
Creating a new database is easy and can be done with one line of code. Either provide the base class and the Database builder will find all subclasses. Or provide a list of types.
```csharp
//From a base class
await DatabaseBuilder.CreateDatabaseSchema(YOUR_FAUNA_CLIENT, typeof(ExampleBase));
//From a list
await DatabaseBuilder.CreateDatabaseSchema(YOUR_FAUNA_CLIENT, new List<Type> { typeof(Collection) });
```
