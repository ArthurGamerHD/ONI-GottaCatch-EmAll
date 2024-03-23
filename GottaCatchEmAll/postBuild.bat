for /f "delims=" %%a in ('powershell.exe -command "& {write-host $([Environment]::GetFolderPath('MyDocuments'))}"') do Set "MY_DOCS_ROOT=%%a"

Echo %MY_DOCS_ROOT%\Klei\OxygenNotIncluded\mods\dev\GottaCatch'EmAll\GottaCatch'EmAll.dll

md "%MY_DOCS_ROOT%\Klei\OxygenNotIncluded\mods\dev\GottaCatch'EmAll" 2> nul
copy %1 "%MY_DOCS_ROOT%\Klei\OxygenNotIncluded\mods\dev\GottaCatch'EmAll\GottaCatch'EmAll.dll"
copy mod.yaml "%MY_DOCS_ROOT%\Klei\OxygenNotIncluded\mods\dev\GottaCatch'EmAll\mod.yaml"
copy mod_info.yaml "%MY_DOCS_ROOT%\Klei\OxygenNotIncluded\mods\dev\GottaCatch'EmAll\mod_info.yaml"
