@echo off
cd /d "%~dp0"
echo Starte lokalen Webserver auf http://localhost:8080
echo Oeffne http://localhost:8080/launcher.html im Browser
echo.
echo Zum Beenden: STRG+C
echo.
python -m http.server 8080
pause
