@echo off
set "GODOT_BIN=E:\workspace\xml-xsd2\application\client\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe"
cd /d E:\workspace\xml-xsd2\application\client\solution
dotnet test --no-build --filter "FullyQualifiedName~Stage_0.Typescript" > "E:\workspace\xml-xsd2\ts_full.log" 2>&1
