@echo off
echo Dang khoi dong Backend Web API Tap Chi Khoa Hoc (HuitJournal.Api) tai cong 5000...
start "" "http://localhost:5000/UI_Mockup_He_Thong_Tap_Chi_Khoa_Hoc.html"
dotnet run --project "%~dp0Backend\HuitJournal.Api" --launch-profile http
