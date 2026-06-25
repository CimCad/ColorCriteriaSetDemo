# ColorCriteriaSetDemo

A minimal Cimatron 2026 API plugin that answers the question: **"How do I get the filter back out
of an existing set?"**

`ISet` exposes only `Name`, `Show`, and `IsCriteria` — there is no `GetFilter()` on `ISet`, and
`ISetsFactory` has no method to hand you a set's filter. So a set you didn't *just* create looks
opaque. The trick is that the set's underlying COM object **also implements `IEntityQuery`**, which
*does* have `GetFilter()`. Cast to it, pull the filter, then cast the filter to its concrete type
(here `FilterColor`) and read the values.

This plugin creates a color criteria set and then immediately recovers the color from it, proving
the round-trip.

## The snippet

```csharp
// model is your interop.CimMdlrAPI.IModel for the active document.

// ---- FORWARD: build a COLOR criteria set ----------------------------------
var query  = (interop.CimMdlrAPI.IEntityQuery)model;
var filter = query.CreateFilter(interop.CimMdlrAPI.EFilterEnumType.cmFilterColor);
var colorFilter = (interop.CimBaseAPI.FilterColor)filter;
colorFilter.Add(0x0000FF);                       // red — packed Win32 COLORREF 0x00BBGGRR

var factory = model.GetSetsFactory();            // interop.CimMdlrAPI.ISetsFactory
try { factory.DeleteSet("DemoColorCriteriaSet"); } catch { }   // idempotent
var set = factory.CreateSet(
    "DemoColorCriteriaSet", (interop.CimMdlrAPI.IEntityFilter)colorFilter);

// ---- REVERSE: read the filter back out of the existing set ----------------
// ISet has no filter getter, but the same COM object implements IEntityQuery.
var setAsQuery = (interop.CimMdlrAPI.IEntityQuery)set;   // ISet -> IEntityQuery
var recovered  = setAsQuery.GetFilter();                // -> IEntityFilter
var recoveredColor = (interop.CimBaseAPI.FilterColor)recovered;  // -> FilterColor
int[] colors = recoveredColor.GetFilter();              // recovered color ints
// colors == { 0x0000FF }   (red, in 0x00BBGGRR order)
```

The full, runnable version (with the active-document plumbing and a result dialog) is in
[`ColorCriteriaSetDemoCommand.cs`](ColorCriteriaSetDemoCommand.cs).

## Notes & caveats

- **Color is a packed Win32 COLORREF** — `0x00BBGGRR`, i.e. `R | (G << 8) | (B << 16)` — **not**
  `0xRRGGBB`. So **red is `0x0000FF`**, green is `0x00FF00`, and **blue is `0xFF0000`** (confirmed at
  runtime: `0xFF0000` renders blue, because blue is the high byte). `FilterColor.GetFilter()` returns
  an `int[]` of these packed values; decode with `R = v & 0xFF; G = (v >> 8) & 0xFF; B = (v >> 16) & 0xFF`.
- **Fully-qualified interop names on purpose.** `interop.CimBaseAPI` and `interop.CimMdlrAPI` both
  declare `IEntityQuery`, `IEntityFilter`, `EFilterEnumType`, etc. If you `using` both namespaces in
  one file you'll hit `CS0104` (ambiguous reference) on every unqualified use. Either qualify fully
  (as above) or add per-file `using` aliases pinning each shared type to one namespace.
- **The reverse cast (`ISet` → `IEntityQuery`) is not in the SDK docs.** It's relied upon here and
  verified working for **color** criteria sets. For a different criteria type, the chain is the same
  up to `GetFilter()`; you then cast the returned `IEntityFilter` to *that* filter's concrete type
  (e.g. a layer/level filter, a type filter) instead of `FilterColor`. You can branch on
  `IEntityFilter.Type` (an `EFilterEnumType`) to pick the right concrete cast.
- **Criteria vs. static sets.** Passing a color filter makes a *criteria* set (a live rule:
  "everything colored X"). A `cmFilterEntityList` filter makes a *static* set of explicit entities.
  This demo uses the color (criteria) flavor.

## Build / register / run

This is a `net48`, `x64`, C# 7.3 plugin. Its build output drops **straight into the Cimatron
Program folder**, so the build *is* the deploy.

1. **Build** with VS Code (or `dotnet build -p:Platform=x64`) running **as Administrator**, with
   **Cimatron closed** (otherwise the previous DLL is locked / Program Files is read-only).
2. **Register** the plugin by adding this line to
   `C:\ProgramData\Cimatron\Cimatron\2026.0\Data\ExternalCommands.ini`:
   ```ini
   [Plugin Ext Commands]
   ColorCriteriaSetDemo.ColorCriteriaSetDemoPlugin=ColorCriteriaSetDemo.ColorCriteriaSetDemoPlugin@1
   ```
   The key must be the `ICimApiCommandPlugin` class (`...Plugin`), **not** the command class.
3. **Run** — launch Cimatron, open a Part, and click **Color Criteria Set Demo** on the `APIs`
   toolbar. A dialog reports the recovered color(s); a `DemoColorCriteriaSet` appears under **Sets**.

## Layout

| File | Role |
|---|---|
| `ColorCriteriaSetDemoPlugin.cs` | `ICimApiCommandPlugin` entry point — registers the toolbar command. |
| `ColorCriteriaSetDemoCommand.cs` | The command — the create + recover round-trip. |
| `ColorCriteriaSetDemo.csproj`, `Directory.Build.props` | Build config + Cimatron interop references. |
| `.vscode/` | F5 = build (admin-checked) → launch Cimatron → attach debugger. |
