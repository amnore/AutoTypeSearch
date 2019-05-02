@echo off
set version=1.0
set output=%~dp0v%version%\
set zipfile="%output%AutoTypeSearch-v%version%.zip"
set buildoutputs="%~dp0Build Outputs"

rd /s /q "%output%"

copy "%~dp0..\Readme.txt" %buildoutputs%
rem copy "%~dp0..\COPYING" %buildoutputs%

rem don't include pdb files
rem del %buildoutputs%\*.pdb

pushd %buildoutputs%
"%ProgramFiles%\7-Zip\7z.exe" a -tzip -mx9 -bd %zipfile% *
popd
copy "%~dp0..\Readme.txt" "%output%"

set version=
set output=
set zipfile=
set buildoutputs=