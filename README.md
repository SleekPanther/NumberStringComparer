# NumberStringComparer
A C# Comparer to sort lists of data that could be either numeric of string replacing the default alphabetical sort which doesn't always work for numbers.

## The Problem
The default alphabetical sort doesn't work when numbers are stored as strings.  
Example 1:
```
var list1 = new List<string>(){
    "1",
    "2",
    "3",
    "10",
    "11",
    "100",
};
list1.Sort();

// Result: string.Join(Environment.NewLine, list1);

"1"
"10"
"100"
"11"
"2"
"3"
```
Example 2: 
```
var list3 = new List<object>(){
    "1",
    "2",
    "3",
    "10",
    "11",
    "100",
    "A",
    "a",
    "b",
    "ab",
};
list3.Sort();

// Result: 
"1"
"10"
"100"
"11"
"2"
"3"
"A"
"a"
"b"
"ab"
```

Example 3: Combined lists containing both numbers and strings because you can't parse every item as numbers and it fails to compare items of different types.
```
var list3 = new List<object>(){
    1,
    2,
    3,
    10,
    11,
    100,
    "A",
    "a",
    "b",
    "ab",
};
list3.Sort();
```
```diff
Result:
- InvalidOperationException: 'Failed to compare two elements in the array.'
```

## Solution
`NumberStringComparer<T>` parses each item into a `NumberString<T>` object composed of a `text` property containing the original text and if the item is numeric also a `number` component. If an item is not completely numeric, `number` will be null and the text is parsed into a list of `parts` containing the text broken up into its numeric and non-numeric parts.  
e.g. "a122bc4" will result in `parts` = `["a", 122, "bc", 4]`  
Items are compared by number if they are completely numeric, otherwise each parts list will be compared for sorting.
If the text contains commas, is broken up into a parts array of numbers, removing the commas.
e.g. "1,2,3" will result in `[1, 2, 3]` and "1,2" will result in `[1, 2]` and when sorting, "1,2" will come before "1,2,3"

## Comparing complex objects by 1 property
You can use any of the following primitive collection types
### Supported data types
- `string`
- `int`
- `double`
- `float`
- `decimal`
- `long`
- `short`

As well as complex objects or Dictionaries using a property.
For Dictionaries, it will use the key property of `KeyValuePair` and ignore the Value property.
See `NumberStringComparerTests.NumberStringComparerObjectComparison_Tests()` for more complex object comparison examples.

### .NET Version
This project uses .NET 7.0 (Core) but could be compiled against .NET Framework with only a few minor changes.
- Replacing `readonly` properties with private fields and properties with private do nothing setters.
- Replace uses of the Range operator with the `SubStringSafe` extension methods.
- Explicitly specify the object type on the right hand of equals replacing `new()`
