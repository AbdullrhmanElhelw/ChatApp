@echo off
title ChatApp Runner
echo ========================================================
echo       Starting ChatApp (.NET MAUI Windows 10/11)
echo ========================================================
cd /d "%~dp0"
start "" "%~dp0ChatApp\Mobile\bin\Debug\net10.0-windows10.0.19041.0\win-x64\ChatApp.Mobile.exe"
exit
