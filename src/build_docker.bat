@echo off
echo.
echo === DEBUG: CONTENT OF Web Dockerfile ===
type LinguaAI.Web\Dockerfile
echo ========================================
echo.

echo Building LinguaAI.Api...
docker build --no-cache -f LinguaAI.Api/Dockerfile .

echo.
echo Building LinguaAI.Web...
docker build --no-cache -f LinguaAI.Web/Dockerfile .

echo.
echo Build Complete!
pause
