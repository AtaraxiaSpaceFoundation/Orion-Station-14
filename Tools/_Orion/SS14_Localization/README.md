# 🛠️ SS14 Localization

Утилита синхронизирует локализацию и поддерживает актуальность FTL-файлов.

## Требования

1. Python **3.13.0** и выше.
2. Доступ к установленному `pip`.
3. Установленные зависимости из `requirements.txt`.

## Быстрый запуск

### Linux/macOS

```bash
cd Tools/_Orion/SS14_Localization
./translation.sh
```

### Windows

```bat
cd Tools\_Orion\SS14_Localization
translation.bat
```

## Переменные окружения

Если используете `translationsassembler.py`, задайте:

- `localise_project_id`
- `localise_personal_token`

Пример Linux/macOS:

```bash
export localise_project_id="..."
export localise_personal_token="..."
python3 translationsassembler.py
```

<h1 align="right"> <img alt="Orion Station" src="https://raw.githubusercontent.com/AtaraxiaSpaceFoundation/asset-dump/refs/heads/master/OrionStation/Orion-Banner-Small.png" />  </h1>
