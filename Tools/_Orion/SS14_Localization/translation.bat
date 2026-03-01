@echo off
setlocal

python -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 13, 0) else 1)"
if errorlevel 1 (
  echo Python 3.13.0 or newer is required.
  goto error
)

call python -m pip install -r requirements.txt --no-warn-script-location
if errorlevel 1 goto error

call python ./yamlextractor.py
if errorlevel 1 goto error

call python ./keyfinder.py
if errorlevel 1 goto error

call python ./clean_duplicates.py
if errorlevel 1 goto error

call python ./clean_empty.py
if errorlevel 1 goto error

echo.
echo Done. Press any key to close this window.
pause >nul
exit /b 0

:error
echo.
echo Script failed with error %errorlevel%. Press any key to close this window.
pause >nul
exit /b %errorlevel%
