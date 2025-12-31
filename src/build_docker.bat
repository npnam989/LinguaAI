@echo off
echo Building LinguaAI.Api...
docker build -f LinguaAI.Api/Dockerfile .

echo.
echo Building LinguaAI.Web...
docker build -f LinguaAI.Web/Dockerfile .

echo.
echo Build Complete!
pause
