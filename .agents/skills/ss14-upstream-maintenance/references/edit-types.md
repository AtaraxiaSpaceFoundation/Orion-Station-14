<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

# Edit Types

## Preferred Edit Order

1. configuration or prototype-only edit
2. extend an existing public system API
3. narrow patch in an upstream content file
4. fork-only sidecar file under `_Orion`
5. engine edit as explicit last-resort escalation

## Rule

Choose the earliest option that fully solves the task without hiding fork behavior in unrelated files, duplicating logic, or hardcoding a one-off case that should stay reusable.

When option 3 requires Orion-specific code in a file outside `_Orion`, keep the patch narrow and mark it:

- Single added or changed line: append `// Orion-Edit` as an inline comment.
- Multiple lines: wrap with block markers:

```csharp
// Orion-Edit-Start
...code here...
// Orion-Edit-End
```

Use the file's native comment syntax for non-C# files while preserving the marker text.
