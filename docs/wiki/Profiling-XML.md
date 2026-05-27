# profiling.xml

Instrument methods in any mod -- including vanilla RimWorld and third-party mods you cannot recompile -- by declaring targets in a plain XML file.

## Purpose

The C# [Profile API](Profile-API) requires you to modify source code. When you want to time a method in someone else's assembly, that is not an option. `profiling.xml` lets you declare targets declaratively: RimObs reads every active mod's file at game startup, resolves the methods by reflection, and installs Harmony transpiler patches automatically.

This covers PRD §11 "declarative profiling.xml" instrumentation style.

## File location

Place the file at:

```
<YourMod>/About/profiling.xml
```

The `About/` folder is the same directory that contains `About.xml` and `Preview.png`. RimObs scans `About/profiling.xml` for every mod in the active mod list, in load order, once at startup.

## Schema

```xml
<!-- Root element must be exactly <Profiling> -->
<Profiling>

  <!--
    <Section> groups one or more methods under a single named section.

    name      (required) - bare section name. RimObs automatically prefixes
              this with your mod's packageId at registration, so a name of
              "Pathfinding" from mod "author.mymod" becomes
              "author.mymod.Pathfinding" in the dashboard.

    subsystem (optional) - free-form label used to group sections in the
              dashboard (e.g. "AI", "Rendering", "World"). Omit to leave
              the section ungrouped.
  -->
  <Section name="Pathfinding" subsystem="AI">

    <!--
      <Methods> is a required container. Sections without it are skipped
      with a warning in Player.log.
    -->
    <Methods>

      <!--
        Each <Method> names one target method using the format:
            Namespace.TypeName:MethodName

        The colon separates the fully-qualified type name from the method
        name. Use the last colon when a type name contains colons (generic
        type parameters use angle brackets, not colons, so this is rare).

        If the type has multiple overloads with the same name, RimObs picks
        the overload with the most parameters. See "Method targeting" below
        for overload disambiguation.
      -->
      <Method>Verse.PathFinder:FindPathNow</Method>
      <Method>Verse.AI.Pawn_PathFollower:PatherTick</Method>

    </Methods>
  </Section>

  <!-- Multiple <Section> elements are allowed in one file. -->
  <Section name="TickBudget" subsystem="Simulation">
    <Methods>
      <Method>Verse.TickManager:DoSingleTick</Method>
    </Methods>
  </Section>

</Profiling>
```

The loader recognizes exactly four element names: `Profiling`, `Section`, `Methods`, and `Method`. All other elements are silently ignored.

## Method targeting

The method spec inside `<Method>` must follow the format `Namespace.TypeName:MethodName`. The type name is resolved by scanning all assemblies loaded into the current `AppDomain` -- this includes RimWorld itself (`Assembly-CSharp.dll`), all loaded mod assemblies, and .NET framework assemblies.

**Overload disambiguation.** When a type has multiple methods with the same name, RimObs selects the overload with the highest parameter count. This matches RimWorld's convention where the canonical heavy implementation takes the most arguments and lighter convenience overloads delegate to it.

If you need to target a specific overload, there is currently no attribute syntax to pin parameter types from XML. Use the [Profile API](Profile-API) instead, which gives you full `MethodInfo` control.

**Example targeting a vanilla method:**

```xml
<Method>Verse.AI.Pawn_JobTracker:DetermineNextJob</Method>
```

`Verse.AI.Pawn_JobTracker` is the fully-qualified type; `DetermineNextJob` is the method name.

## Lifecycle

1. **Load** - `profiling.xml` files are read once during `RimObsMod` constructor, before any Harmony patches are installed. The loader iterates every mod in `LoadedModManager.RunningModsListForReading`.
2. **Resolution** - After all files are parsed, `SectionCatalog.ResolveAll()` resolves each `TypeName:MethodName` pair by reflection against all currently loaded assemblies.
3. **Patch installation** - `PatchInstaller.InstallAll()` installs a Harmony IL transpiler on each resolved method. The transpiler wraps the entire method body in a `Profiler.Start` / `Profiler.Stop` pair.
4. **Missing targets** - If a type or method cannot be found, RimObs logs a warning to `Player.log` in the form `[RimObs] Section 'name' unresolved: ...` and continues. No exception is thrown and no other sections are affected.
5. **Parse errors** - If the file fails to parse (malformed XML, wrong root element, missing `name` attribute), RimObs logs a warning per-file and skips it. Other mods' files are still processed.

The scan happens once per game session. Adding or editing `profiling.xml` requires a game restart to take effect.

## Limits

`profiling.xml` wraps the entire method body of each target. It cannot:

- Measure a sub-range inside a method
- Apply conditional measurement based on arguments or game state
- Attach custom label values or metric attributes
- Target constructors, property accessors, or generic method instantiations

For any of these cases, use the [Profile API](Profile-API) directly from C#.

Note that XML-declared patches go through the same IL transpiler as code-declared patches, so all [hot-path discipline](Hot-Path-Discipline) rules still apply: no allocation on the instrumented path, exception-safe start/stop pairing, and zero cost when the profiler is disabled.

## Related

- [Profile API](Profile-API) - C# instrumentation API for cases XML cannot express
- [Hot-Path Discipline](Hot-Path-Discipline) - allocation and performance rules that apply to all patches
- [Troubleshooting](Troubleshooting) - diagnosing "my target method was not patched" and other startup warnings
