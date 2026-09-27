@echo off
chcp 65001 >nul
title KIỂM THỬ TỰ ĐỘNG RESPONSIVE & CHỨC NĂNG WEB (PLAYWRIGHT)
color 0b

echo ===============================================================================
echo   HỆ THỐNG QUẢN LÝ TÒA SOẠN & XUẤT BẢN TẠP CHÍ KHOA HỌC HUIT
echo   KỊCH BẢN KIỂM THỬ TỰ ĐỘNG RESPONSIVE 12 TRANG x 5 KÍCH THƯỚC (PLAYWRIGHT)
echo ===============================================================================
echo.
echo Đang kiểm tra Python và Playwright...
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [LỖI] Không tìm thấy Python trên máy tính. Vui lòng cài đặt Python 3.10+ để chạy kiểm thử.
    pause
    exit /b
)

echo Đang khởi chạy kịch bản kiểm thử: Web/tests/test_responsive_web.py...
echo Quá trình kiểm tra 12 trang web tại 5 kích thước (320px, 375px, 768px, 1024px, 1366px)...
echo.

python "Web\tests\test_responsive_web.py"

echo.
echo ===============================================================================
echo   Quá trình kiểm thử đã hoàn tất!
echo ===============================================================================
pause
