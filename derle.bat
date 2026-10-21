@echo off
rem RAM Danismani - derleme betigi.
rem Windows'ta yerlesik gelen .NET Framework 4 derleyicisini (csc.exe, C# 5) kullanir.
rem Hicbir ek kurulum gerekmez. Cikti: RamChecker.exe (bu klasorde)
setlocal
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo HATA: .NET Framework 4 derleyicisi bulunamadi.
  exit /b 1
)

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 ^
  /out:RamChecker.exe /win32manifest:src\app.manifest /win32icon:src\ram.ico ^
  /r:System.dll /r:System.Core.dll /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  src\*.cs
if errorlevel 1 (
  echo.
  echo DERLEME BASARISIZ.
  exit /b 1
)
echo Derlendi: %~dp0RamChecker.exe
exit /b 0
