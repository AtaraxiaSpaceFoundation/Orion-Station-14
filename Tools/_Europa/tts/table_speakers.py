import requests
from tabulate import tabulate

def get_speaker_data(url):
    """
    Получает JSON-данные по указанному URL и возвращает их.
    """
    try:
        response = requests.get(url)
        response.raise_for_status()  # Вызывает исключение для ошибок HTTP (4xx или 5xx)
        return response.json()
    except requests.exceptions.RequestException as e:
        print(f"Ошибка при получении данных: {e}")
        return None

def display_as_table(data):
    """
    Выводит данные в виде таблицы в консоль.
    """
    if not data or not isinstance(data, dict) or "voices" not in data or not isinstance(data["voices"], list):
        print("Полученные данные некорректны или пусты.")
        return

    voices = data["voices"]

    # Определяем заголовки таблицы.
    # Мы будем использовать ключи из первого элемента списка "voices",
    # но можно и явно указать нужные нам ключи, если они могут отличаться.
    headers = ["Name", "Description", "Source", "Gender", "Speakers"]

    # Готовим данные для tabulate.
    # Каждая строка таблицы - это список значений для каждой колонки.
    table_data = []
    for voice in voices:
        # Преобразуем список speakers в строку, чтобы он поместился в одну ячейку
        speakers_str = ", ".join(voice.get("speakers", [])) if voice.get("speakers") else "N/A"
        table_data.append([
            voice.get("name", "N/A"),
            voice.get("description", "N/A"),
            voice.get("source", "N/A"),
            voice.get("gender", "N/A"),
            speakers_str
        ])

    # Выводим таблицу
    print(tabulate(table_data, headers=headers, tablefmt="grid")) # tablefmt="grid" для красивой рамки

if __name__ == "__main__":
    api_url = "https://ntts.fdev.team/api/v1/tts/speakers"
    speaker_json = get_speaker_data(api_url)

    if speaker_json:
        display_as_table(speaker_json)