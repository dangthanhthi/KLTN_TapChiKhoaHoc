@echo off
echo Dang khoi dong Web Server Tap Chi Khoa Hoc tai cong 8088...
start "" "http://localhost:8088/UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html"
python -m http.server 8088 --directory Web
