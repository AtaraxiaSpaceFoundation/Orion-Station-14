import glob
import os
from pathlib import Path
from typing import Tuple
from file import FluentFile

class Project:
    def __init__(self):
        self.base_dir_path = self._resolve_repo_root()
        self.resources_dir_path = os.path.join(self.base_dir_path, 'Resources')
        self.locales_dir_path = os.path.join(self.resources_dir_path, 'Locale')
        self.ru_locale_dir_path = os.path.join(self.locales_dir_path, 'ru-RU')
        self.en_locale_dir_path = os.path.join(self.locales_dir_path, 'en-US')
        self.prototypes_dir_path = os.path.join(self.resources_dir_path, 'Prototypes')

    @staticmethod
    def _resolve_repo_root() -> str:
        marker_file = 'SpaceStation14.sln'
        current_path = Path(__file__).resolve().parent

        for directory in [current_path, *current_path.parents]:
            if (directory / marker_file).is_file():
                return str(directory)

        raise FileNotFoundError(f'Не удалось найти {marker_file} для определения корня репозитория')

    def split_prototype_relative_path(self, relative_parent_dir: str) -> Tuple[str | None, str]:
        normalized_path = os.path.normpath(relative_parent_dir)
        path_parts = [part for part in normalized_path.split(os.sep) if part and part != '.']

        if not path_parts:
            return None, ''

        first_segment = path_parts[0]
        if first_segment.startswith('_'):
            module_name = first_segment
            module_relative_path = os.path.join(*path_parts[1:]) if len(path_parts) > 1 else ''
            return module_name, module_relative_path

        return None, os.path.join(*path_parts)

    def get_locale_prototypes_dir(self, locale: str, relative_parent_dir: str) -> str:
        locale_root = os.path.join(self.locales_dir_path, locale)
        module_name, module_relative_path = self.split_prototype_relative_path(relative_parent_dir)

        if module_name:
            base_path = os.path.join(locale_root, module_name, 'prototypes')
        else:
            base_path = os.path.join(locale_root, 'prototypes')

        return os.path.join(base_path, module_relative_path) if module_relative_path else base_path

    def get_files_paths_by_dir(self, dir_path, files_extenstion):
        return glob.glob(f'{dir_path}/**/*.{files_extenstion}', recursive=True)

    def get_fluent_files_by_dir(self, dir_path):
        files = []
        files_paths_list = glob.glob(f'{dir_path}/**/*.ftl', recursive=True)

        for file_path in files_paths_list:
            try:
                files.append(FluentFile(file_path))
            except Exception:
                continue

        return files
