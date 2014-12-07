@echo off
cd %~dp0

echo Deleting existing PlgX folder
rmdir /s /q PlgX

echo Creating PlgX folder
mkdir PlgX

echo Copying files
xcopy "AutoTypeSearch" PlgX /s /e /exclude:PlgXExclude.txt

echo Compiling PlgX
"../KeePass/KeePass.exe" /plgx-create "%~dp0PlgX" --plgx-prereq-kp:2.27

echo Releasing PlgX
move /y PlgX.plgx "Releases\Build Outputs\AutoTypeSearch.plgx"

echo Cleaning up
rmdir /s /q PlgX
