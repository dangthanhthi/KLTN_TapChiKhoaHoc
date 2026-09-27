@echo off
chcp 65001 > nul
echo =====================================================================
echo    CONG CU DAY MA NGUON LEN GITHUB - KHOA LUAN TOT NGHIEP HUIT
echo =====================================================================
echo.

set GIT_EXE=git
where git >nul 2>&1
if %errorlevel% neq 0 (
    if exist "C:\laragon\bin\git\cmd\git.exe" (
        set GIT_EXE="C:\laragon\bin\git\cmd\git.exe"
    ) else (
        echo [LOI] Khong tim thay Git tren may! Vui long cai dat Git.
        pause
        exit /b 1
    )
)

echo [1/4] Kiem tra Git repository...
if not exist ".git" (
    echo Khoi tao git repo...
    %GIT_EXE% init
    %GIT_EXE% config user.name "dangthanhthi"
    %GIT_EXE% config user.email "dangthanhthi@users.noreply.github.com"
    %GIT_EXE% branch -M main
    %GIT_EXE% remote add origin https://github.com/dangthanhthi/KLTN_TapChiKhoaHoc.git
) else (
    %GIT_EXE% remote set-url origin https://github.com/dangthanhthi/KLTN_TapChiKhoaHoc.git
)

echo [2/4] Dong bo tap tin vao Git index...
%GIT_EXE% add .

echo [3/4] Tao Commit...
%GIT_EXE% commit -m "Cap nhat toan dien: DongTacGia, Stored Procedures sp_DongBoDongTacGia_TheoEmail, Author Claiming va Tai lieu KLTN"

echo.
echo [4/4] Day len GitHub (Branch: main)...
echo.
echo Dang day ma nguon len GitHub bang Git Credential Manager...
%GIT_EXE% push -u origin main

echo.
if %errorlevel% equ 0 (
    echo [THANH CONG] Da day toan bo ma nguon len GitHub thanh cong!
) else (
    echo [THONG BAO] Neu gap loi xac thuc, hay dang nhap GitHub bang Git Credential Manager.
)
echo.
pause
