dotnet build -c Release
cd bin/Release/net7.0

@REM For Root Installer
copy TowerFall.FortRise.mm.dll "../../../../../../Installer/TowerFall.FortRise.mm.dll"
copy TowerFall.FortRise.mm.pdb "../../../../../../Installer/TowerFall.FortRise.mm.pdb"
copy TowerFall.FortRise.mm.xml "../../../../../../Installer/TowerFall.FortRise.mm.xml"

@REM For production Installer
copy TowerFall.FortRise.mm.dll "../../../../Installer/lib/TowerFall.FortRise.mm.dll"
copy TowerFall.FortRise.mm.pdb "../../../../Installer/lib/TowerFall.FortRise.mm.pdb"
copy TowerFall.FortRise.mm.xml "../../../../Installer/lib/TowerFall.FortRise.mm.xml"

cd ../../../../../../fortOrig
copy "TowerFall.exe" "../TowerFall.exe"

cd ../Installer

Installer.NoAnsi.exe --patch "../"
cd ../
del TowerFall.exe


@REM copy "TowerFall.exe" "../TowerFall.exe"
@REM cd ../
@REM MonoMod.Patcher.exe --dependency-missing-throw=0 TowerFall.exe
@REM copy MONOMODDED_TowerFall.exe TowerFall.exe
@REM cd MonoMod/TowerFallMM

pause