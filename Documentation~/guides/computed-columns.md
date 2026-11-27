# Computed columns

A computed column shows the value returned by a method, instead of a field: a total, a ratio, a rank among the other assets, a tier. Its values are read-only, and follow the edits of the assets.

```cs
using SideXP.Database;

public static class EquipmentColumns
{
    // The sum of the bonuses of each equipment
    [ComputedColumn(typeof(EquipmentSO), "Total Bonus", Width = 88, Tooltip = "STR + AGI + INT.")]
    private static int GetTotalBonus(EquipmentSO item)
    {
        return item.Strength + item.Agility + item.Intelligence;
    }
}
```

The column is added to the table of `EquipmentSO`, and to the tables of its derived types, after the columns of the fields.

## `[ComputedColumn]`

Marks a static method as a column of the tables of a type. The attribute is in the `SideXP.Database` namespace, and is stripped from player builds.

| Option | Default | Description |
|---|---|---|
| `TargetType` (the first argument) | Required | The type of the assets the column applies to. The column is added to the tables whose type is this type or derives from it. It can be an interface, to add a column to unrelated tables |
| `Name` (or the second argument) | The method name, nicified (`GetTotalBonus` becomes *Get Total Bonus*) | The text of the column header |
| `Tooltip` | None | The text shown when hovering the column header. The header also says that the column is computed and read-only |
| `Width` | `0` | The default width of the column, in pixels. With `0` or less, the column is flexible |

### The method

The method can be declared in any class, and can be private:

- In an **editor** class (in an `Editor` folder, or an editor assembly): the most common choice, since the columns only exist in the editor. It also works for types you can't change, such as the types of a package.
- In a **runtime** class, inside `#if UNITY_EDITOR`, to keep the column next to its type.

Its signature must be one of:

```cs
static TValue Method(TAsset asset)
static TValue Method(TAsset asset, ITableContext context)
```

- `TAsset` is the target type or one of its base types: the method receives the asset of the row.
- `TValue` is any type but `void`. It chooses how the cells display, sort and convert the values to text, like the type of a field: a `bool` is a checkbox, an enum a dropdown, both read-only.
- The `context` gives access to the other assets of the table (see [The table context](#the-table-context)).

A method with another signature (not static, generic, without return value, with other parameters) isn't a column: it's reported in the warning that lists the issues of the declarations of the tables, logged once after each compilation, when the window first needs the tables. A method that applies to no table is never reported. Overloads of a method share its identifier, so only the first one is a column.

## What a computed column can do

- **Sort**: like a field of its type.
- **Filter**: the [filters](views-and-filters.md#filters) can have conditions on computed columns (see [Filters and visible assets](#filters-and-visible-assets)).
- **Search**: the search finds their values, as the cells display them.
- **Copy and export**: their values are copied, and exported to CSV and TSV.
- **Layout**: they can be hidden, moved and resized, like the other columns.

They can't be edited: double-click and the keyboard do nothing on their cells, their checkboxes and dropdowns ignore the clicks, and the edits of several cells, the paste and the import skip them, and report them.

> A computed column is identified by its method (`#Namespace.Type.Method`). Changing its `Name` keeps its settings in the views, but renaming the method, or its class, loses them.

## When the values are computed

The values are cached, and computed again when the data of the table changes: an edit, an Undo, a reload, an asset created, deleted or moved. They are also computed again when the filters or the folder scope change. They are never computed on a repaint, a sort, a page change or a search.

- A value can depend on the other assets (a rank), so a change of one asset computes the values of all the rows again.
- The values are computed for the rows that pass the filters only. Exporting the whole table computes them for all the assets.

## The table context

The second parameter, `ITableContext`, gives the assets of the table, to compute values from the other assets:

| Member | Description |
|---|---|
| `AllAssets` | All the assets of the table (in its folder scope), in the order of the rows before any sort |
| `VisibleAssets` | The assets that pass the filters, in the same order. A value based on them changes when the filters change |
| `GetShared<T>(build)` | Data shared by all the rows of the computation, built the first time it's asked for |

The lists hold the assets as `Object`: cast them to the type of the table (`OfType<EquipmentSO>()`).

### Shared data

A value that compares an asset with the others (a rank, an average) would read all the assets for each row. `GetShared()` builds that data once per computation, and gives it to every row:

```cs
using System.Collections.Generic;
using System.Linq;
using SideXP.Database;
using UnityEngine;

public static class EquipmentColumns
{
    // The rank of the price of each equipment among the visible ones, the most expensive first
    [ComputedColumn(typeof(EquipmentSO), "Price Rank", Width = 80)]
    private static int GetPriceRank(EquipmentSO item, ITableContext context)
    {
        return context.GetShared(BuildPriceRanks).TryGetValue(item, out int rank) ? rank : 0;
    }

    // Is the price above the average price of all the equipment, filtered or not?
    [ComputedColumn(typeof(EquipmentSO), "Above Average?", Width = 96)]
    private static bool IsAboveAveragePrice(EquipmentSO item, ITableContext context)
    {
        return item.BasePrice > context.GetShared(GetAveragePrice);
    }

    // Built once per computation, for all the rows
    private static Dictionary<Object, int> BuildPriceRanks(ITableContext context)
    {
        List<EquipmentSO> items = context.VisibleAssets
            .OfType<EquipmentSO>()
            .OrderByDescending(i => i.BasePrice)
            .ToList();

        Dictionary<Object, int> ranks = new Dictionary<Object, int>(items.Count);
        for (int i = 0; i < items.Count; i++)
            ranks[items[i]] = i + 1;
        return ranks;
    }

    private static double GetAveragePrice(ITableContext context)
    {
        List<EquipmentSO> items = context.AllAssets.OfType<EquipmentSO>().ToList();
        return items.Count > 0 ? items.Average(i => i.BasePrice) : 0;
    }
}
```

- The data is identified by its build method: pass a method (a method group, like `BuildPriceRanks` above), not a lambda that captures variables, which would be a new delegate, and so new data, for each row.
- The data is built again on the next computation. Don't keep the context nor its data: they are only valid during the computation.

### Filters and visible assets

The filters can have conditions on computed columns, but the values depend on the filters. So the table filters the rows in two passes:

1. The conditions on the other columns (a condition on a computed column counts as met). The rows that pass are the `VisibleAssets` of the context, and their values are computed.
2. The whole filter, with the computed values, then the search.

With a condition *Price Rank at most 3*, the table shows the 3 most expensive items among the items filtered by the other conditions. Typing a search never changes the values, since it's applied after them.

## Errors

If the method throws an exception, the column stops for this computation: the rows not computed yet get no value. The first exception of the column is logged in the console, with the asset of the row. The other columns are computed as usual.
