import requests
import re
import yaml
import os

def get_speaker_data(url):
    """
    Получает JSON-данные по указанному URL и возвращает их.
    """
    try:
        response = requests.get(url)
        response.raise_for_status()  # Вызывает исключение для ошибок HTTP
        return response.json()
    except requests.exceptions.RequestException as e:
        print(f"Ошибка при получении данных: {e}")
        return None

def sanitize_for_id(text):
    """
    Санитарная функция для создания идентификаторов.
    Удаляет все, кроме букв, цифр и дефисов, заменяет пробелы на дефисы.
    """
    if not text:
        return "unidentified"
    # Убираем все, что не буква, цифра или дефис, и заменяем пробелы на дефисы
    sanitized = re.sub(r'[^\w\-]+', '', text).strip().replace(' ', '-')
    # Если после очистки ничего не осталось (были только спецсимволы)
    return sanitized if sanitized else "SOSI_HUY"

def generate_localization_and_prototypes(data, dir="result", ftl_filename="tts-voices.ftl", proto_filename="tts-voices.yml"):
    """
    Генерирует .ftl файл для локализации голосов и .yaml файл для прототипов голосов.
    """
    if not data or not isinstance(data, dict) or "voices" not in data or not isinstance(data["voices"], list):
        print("Полученные данные некорректны или пусты.")
        return

    # Создаем выходную директорию, если она не существует
    if not os.path.exists(dir):
        os.makedirs(dir)
        print(f"Создана директория: {dir}")

    ftl_filename = os.path.join(dir, "voices_localization.ftl")
    proto_filename = os.path.join(dir, "voice_prototypes.yaml")

    voices = data["voices"]
    ftl_lines = []
    prototypes = []

    # --- Генерация FTL записей ---
    for voice in voices:
        name_in_json = voice.get("name", "SOSI_HUY")
        speakers_list = voice.get("speakers", [])
        gender = voice.get("gender", "Unsexed") # Берем пол

        if not speakers_list:
            print(f"Предупреждение: Голос '{name_in_json}' не имеет идентификатора спикера. Пропускается.")
            continue

        speaker_id_raw = speakers_list[0] # Raw идентификатор спикера
        sanitized_speaker_id = sanitize_for_id(speaker_id_raw) # Санитарный ID для ключа и прототипа

        # Генерируем ключ FTL
        ftl_key = f"tts-voice-name-{sanitized_speaker_id}"

        # Определяем категорию
        category_value = voice.get("source", "SOSI_HUY")

        ftl_value = f"{name_in_json} ({category_value})"
        ftl_lines.append(f"{ftl_key} = {ftl_value}")

        # --- Добавление прототипа ---
        prototype_entry = {
            "type": "ttsVoice",
            "id": sanitized_speaker_id.capitalize(), # Id с первой заглавной буквы
            "name": ftl_key, # Ключ локализации
            "sex": gender.capitalize(), # Пол с первой заглавной буквы
            "speaker": speaker_id_raw.lower() # Берем первый идентификатор спикера в нижнем регистре
        }
        prototypes.append(prototype_entry)

    # --- Запись в FTL файл ---
    try:
        with open(ftl_filename, "w", encoding="utf-8") as f:
            for line in ftl_lines:
                f.write(line + "\n")
        print(f"Файл локализации '{ftl_filename}' успешно сгенерирован.")
    except IOError as e:
        print(f"Ошибка при записи в файл локализации '{ftl_filename}': {e}")

    # --- Запись в YAML файл прототипов ---
    try:
        with open(proto_filename, "w", encoding="utf-8") as f:
            yaml.dump(prototypes, f, allow_unicode=True, sort_keys=False, default_flow_style=False)
        print(f"Файл прототипов '{proto_filename}' успешно сгенерирован.")
    except IOError as e:
        print(f"Ошибка при записи в файл прототипов '{proto_filename}': {e}")

if __name__ == "__main__":
    api_url = "https://ntts.fdev.team/api/v1/tts/speakers" # Можно было бы и вводимым сделать
    speaker_json = get_speaker_data(api_url)

    if speaker_json:
        generate_localization_and_prototypes(speaker_json)