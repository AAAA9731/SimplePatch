@echo off
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
cl /nologo /utf-8 /O2 /LD /MT proxy.c /Fe:version.dll /link /DLL user32.lib
