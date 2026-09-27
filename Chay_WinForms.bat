@echo off
chcp 65001 >nul
echo ======================================================================
echo    KHOI CHAY UNG DUNG DESKTOP WINFORMS - HUIT SCIENTIFIC JOURNAL
echo ======================================================================
set "EXE_PATH=%~dp0Winform\QL_TapChi_WinForms\bin\Debug\net8.0-windows\QL_TapChi_WinForms.exe"
if exist "%EXE_PATH%" (
    echo [OK] Phat hien ban bien dich san sang. Dang khoi dong WinForms...
    start "" "%EXE_PATH%"
) else (
    echo [INFO] Dang bien dich va chay du an WinForms qua .NET CLI...
    dotnet run --project "%~dp0Winform\QL_TapChi_WinForms\QL_TapChi_WinForms.csproj"
)
