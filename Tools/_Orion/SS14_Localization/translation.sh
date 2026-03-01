#!/usr/bin/env sh
set -eu

# make sure to start from script dir
if [ "$(dirname "$0")" != "." ]; then
    cd "$(dirname "$0")"
fi

python3 - <<'PY'
import sys
required = (3, 13, 0)
if sys.version_info < required:
    raise SystemExit(f"Python {required[0]}.{required[1]}.{required[2]}+ is required")
PY

python3 -m pip install -r requirements.txt --no-warn-script-location
python3 ./yamlextractor.py
python3 ./keyfinder.py
python3 ./clean_duplicates.py
python3 ./clean_empty.py
