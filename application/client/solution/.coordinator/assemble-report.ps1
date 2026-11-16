$f = Split-Path -Parent $MyInvocation.MyCommand.Path
$md = New-Object System.Collections.Generic.List[string]
$md.Add('# 19 - Test run output: drag-and-drop tests')
$md.Add('')
$md.Add('Steps: build -> DragDropBuyTests (runtime-only) -> DragDropVisualTests (visual). Nothing was fixed; run and report only.')
$md.Add('')
$md.Add('Working directory: E:\workspace\xml-xsd2\application\client\solution')
$md.Add('Captured: 16/11/2026 ~01:28 local time (PowerShell 7, all stdout+stderr streams redirected to files)')
$md.Add('')
$md.Add('## Summary')
$md.Add('')
$md.Add('| # | Command | Exit code | Outcome |')
$md.Add('|---|---------|-----------|---------|')
$md.Add('| 1 | `dotnet build "New Game Project.csproj" -v quiet -nologo` | 0 | Build succeeded - 96 warnings, 0 errors |')
$md.Add('| 2 | `dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropBuyTests" -v normal --nologo` | 0 | Passed: 1 (Given_land_for_sale_when_dropped_on_inventory_it_should_buy) |')
$md.Add('| 3 | `dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropVisualTests" -v normal --nologo` | 1 | Failed: 1 (it_should_show_ghost_and_drop_feedback_and_complete_the_drop) - Error: "shop should report IsDragging while the drag is in progress" at DragDropVisualTests.cs:line 75 |')
$md.Add('')
$md.Add('Note: an earlier verification run of the exact same three commands produced identical results (build ok / buy pass / visual fail with the same assertion at DragDropVisualTests.cs:line 75, exit codes 0 / 0 / 1). The outputs below are the byte-exact captures of the redirected run.')
$md.Add('')

function Add-Section {
  param($md, $title, [string[]]$cmd, $exitcode, $file)
  $md.Add("## $title")
  $md.Add('')
  $md.Add('Command:')
  $md.Add('')
  $md.Add('```powershell')
  $md.AddRange($cmd)
  $md.Add('```')
  $md.Add('')
  $md.Add("Exit code: **$exitcode**")
  $md.Add('')
  $md.Add('Complete output (stdout+stderr combined, verbatim):')
  $md.Add('')
  $md.Add('```')
  $md.AddRange((Get-Content -LiteralPath $file))
  $md.Add('```')
  $md.Add('')
}

Add-Section $md 'Step 1: Build' @(
  'cd E:\workspace\xml-xsd2\application\client\solution',
  'dotnet build "New Game Project.csproj" -v quiet -nologo'
) 0 (Join-Path $f 'step1-build.txt')

Add-Section $md 'Step 2: DragDropBuyTests (runtime-only)' @(
  'cd E:\workspace\xml-xsd2\application\client\solution',
  'dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropBuyTests" -v normal --nologo'
) 0 (Join-Path $f 'step2-buy.txt')

Add-Section $md 'Step 3: DragDropVisualTests (visual)' @(
  'cd E:\workspace\xml-xsd2\application\client\solution',
  'dotnet test --settings .runsettings --no-build --filter "FullyQualifiedName~DragDropVisualTests" -v normal --nologo'
) 1 (Join-Path $f 'step3-visual.txt')

$out = Join-Path $f '19-test-run-output.md'
Set-Content -LiteralPath $out -Value ($md -join "`n") -Encoding utf8
"written: $out ($($md.Count) lines)"
