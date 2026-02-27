@echo off
setlocal

python -c "import sys; raise SystemExit(0 if sys.version_info >= (3, 13, 0) else 1)"
if errorlevel 1 (
  echo Python 3.13.0 or newer is required.
  exit /b 1
)

call python -m pip install -r requirements.txt --no-warn-script-location

call python ./yamlextractor.py
call python ./keyfinder.py
call python ./clean_duplicates.py
call python ./clean_empty.py

echo Done.
exit /b 0

:error
echo Script failed with error %errorlevel%
exit /b %errorlevel%
