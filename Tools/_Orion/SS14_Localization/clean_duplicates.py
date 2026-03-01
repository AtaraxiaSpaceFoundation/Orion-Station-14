import os
import re
import chardet
from datetime import datetime
from typing import Dict, List, Tuple
from project import Project


def find_ftl_files(root_dir: str) -> List[str]:
    ftl_files: List[str] = []
    for root, _, files in os.walk(root_dir):
        for file_name in sorted(files):
            if file_name.endswith('.ftl'):
                ftl_files.append(os.path.join(root, file_name))
    return sorted(ftl_files)

def detect_encoding(file_path: str) -> str:
    with open(file_path, 'rb') as file:
        raw_data = file.read()
    return chardet.detect(raw_data).get('encoding') or 'utf-8'

def parse_ent_blocks(file_path: str) -> Dict[str, str]:
    try:
        encoding = detect_encoding(file_path)
        with open(file_path, 'r', encoding=encoding) as file:
            content = file.read()
    except UnicodeDecodeError:
        print(f'Ошибка при чтении файла {file_path}. Попытка чтения в UTF-8.')
        try:
            with open(file_path, 'r', encoding='utf-8') as file:
                content = file.read()
        except UnicodeDecodeError:
            print(f'Не удалось прочитать файл {file_path}. Пропускаем.')
            return {}

    ent_blocks: Dict[str, str] = {}
    current_ent: str | None = None
    current_block: List[str] = []

    for line in content.split('\n'):
        if line.startswith('ent-'):
            if current_ent:
                ent_blocks[current_ent] = '\n'.join(current_block)
            current_ent = line.split('=')[0].strip()
            current_block = [line]
            continue

        if current_ent and (line.strip().startswith('.desc') or line.strip().startswith('.suffix')):
            continue

        if line.strip() == '' and current_ent:
            ent_blocks[current_ent] = '\n'.join(current_block)
            current_ent = None
            current_block = []
            continue

    if current_ent:
        ent_blocks[current_ent] = '\n'.join(current_block)

    return ent_blocks

def remove_duplicates(root_dir: str) -> Tuple[int, str]:
    ftl_files = find_ftl_files(root_dir)
    all_entity: Dict[str, Tuple[str, str]] = {}
    removed_duplicates: List[Tuple[str, str, str]] = []

    for file_path in ftl_files:
        ent_blocks = parse_ent_blocks(file_path)
        for ent, block in ent_blocks.items():
            if ent not in all_entity:
                all_entity[ent] = (file_path, block)

    for file_path in ftl_files:
        try:
            encoding = detect_encoding(file_path)
            with open(file_path, 'r', encoding=encoding) as file:
                content = file.read()

            ent_blocks = parse_ent_blocks(file_path)
            for ent, block in ent_blocks.items():
                if all_entity[ent][0] != file_path:
                    content = content.replace(block, '')
                    removed_duplicates.append((ent, file_path, block))

            content = re.sub(r'\n{3,}', '\n\n', content)

            with open(file_path, 'w', encoding=encoding) as file:
                file.write(content)
        except (OSError, UnicodeError) as error:
            print(f'Ошибка при обработке файла {file_path}: {error}')

    # Сохранение лога удаленных дубликатов
    log_filename = f"removed_duplicates_{datetime.now().strftime('%Y%m%d_%H%M%S')}.log"
    with open(log_filename, 'w', encoding='utf-8') as log_file:
        for ent, file_path, block in removed_duplicates:
            log_file.write(f'Удален дубликат: {ent}\n')
            log_file.write(f'Файл: {file_path}\n')
            log_file.write('Содержимое:\n')
            log_file.write(block)
            log_file.write('\n\n')

    print(f'Обработка завершена. Проверено файлов: {len(ftl_files)}')
    print(f'Лог удаленных дубликатов сохранен в файл: {log_filename}')
    return len(ftl_files), log_filename

if __name__ == '__main__':
    project = Project()
    remove_duplicates(project.ru_locale_dir_path)
