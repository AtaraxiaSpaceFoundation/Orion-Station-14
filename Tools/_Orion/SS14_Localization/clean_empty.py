import logging
import os
from datetime import datetime
from typing import Tuple
from project import Project


def setup_logging() -> str:
    log_filename = f"cleanup_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"
    logging.basicConfig(
        filename=log_filename,
        level=logging.INFO,
        format='%(asctime)s - %(levelname)s - %(message)s',
    )
    console = logging.StreamHandler()
    console.setLevel(logging.INFO)
    logging.getLogger('').addHandler(console)
    return log_filename

def remove_empty_files_and_folders(path: str) -> Tuple[int, int]:
    removed_files = 0
    removed_folders = 0

    for root, _, files in os.walk(path, topdown=False):
        for file_name in files:
            file_path = os.path.join(root, file_name)
            if os.path.getsize(file_path) == 0:
                try:
                    os.remove(file_path)
                    logging.info(f"Удален пустой файл: {file_path}")
                    removed_files += 1
                except Exception as error:
                    logging.error(f"Ошибка при удалении файла {file_path}: {error}")

        # Удаление пустых папок
        if not os.listdir(root):
            try:
                os.rmdir(root)
                logging.info(f"Удалена пустая папка: {root}")
                removed_folders += 1
            except Exception as error:
                logging.error(f"Ошибка при удалении папки {root}: {error}")

    return removed_files, removed_folders

if __name__ == "__main__":
    project = Project()
    root_dir = project.locales_dir_path
    log_file = setup_logging()

    logging.info(f"Начало очистки в директории: {root_dir}")
    files_removed, folders_removed = remove_empty_files_and_folders(root_dir)
    logging.info(f"Очистка завершена. Удалено файлов: {files_removed}, удалено папок: {folders_removed}")
    print(f"Лог операций сохранен в файл: {log_file}")
