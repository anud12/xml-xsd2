# 19 - Test run output: drag-and-drop tests

Steps: build -> DragDropBuyTests (runtime-only) -> DragDropVisualTests (visual). Nothing was fixed; run and report only.

Working directory: E:\workspace\xml-xsd2\application\client\solution
Captured: 16/11/2026 ~01:28 local time (PowerShell 7, all stdout+stderr streams redirected to files)

## Summary

| # | Command | Exit code | Outcome |
|---|---------|-----------|---------|
| 1 | `dotnet build "New Game Project.csproj" -v quiet -nologo` | 0 | Build succeeded - 96 warnings, 0 errors |
| 2 | `dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropBuyTests" -v normal --nologo` | 0 | Passed: 1 (Given_land_for_sale_when_dropped_on_inventory_it_should_buy) |
| 3 | `dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropVisualTests" -v normal --nologo` | 1 | Failed: 1 (it_should_show_ghost_and_drop_feedback_and_complete_the_drop) - Error: "shop should report IsDragging while the drag is in progress" at DragDropVisualTests.cs:line 75 |

Note: an earlier verification run of the exact same three commands produced identical results (build ok / buy pass / visual fail with the same assertion at DragDropVisualTests.cs:line 75, exit codes 0 / 0 / 1). The outputs below are the byte-exact captures of the redirected run.

## Step 1: Build

Command:

```powershell
cd E:\workspace\xml-xsd2\application\client\solution
dotnet build "New Game Project.csproj" -v quiet -nologo
```

Exit code: **0**

Complete output (stdout+stderr combined, verbatim):

```
```

## Step 2: DragDropBuyTests (runtime-only)

Command:

```powershell
cd E:\workspace\xml-xsd2\application\client\solution
dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropBuyTests" -v normal --nologo
```

Exit code: **0**

Complete output (stdout+stderr combined, verbatim):

```
```

## Step 3: DragDropVisualTests (visual)

Command:

```powershell
cd E:\workspace\xml-xsd2\application\client\solution
dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropVisualTests" -v normal --nologo
```

Exit code: **1**

Complete output (stdout+stderr combined, verbatim):

```
```

