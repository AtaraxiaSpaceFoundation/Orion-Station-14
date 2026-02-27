import typing
import os
from pydash import py_
from project import Project

class LocalePath:
    def __init__(self, relative_file_path):
        self.ru = os.path.join(Project().ru_locale_dir_path, relative_file_path)
        self.en = os.path.join(Project().en_locale_dir_path, relative_file_path)


class LocaliseTranslation:
    def __init__(self, data, key_name: typing.AnyStr):
        self.key_name = key_name
        self.data = data

class LocaliseKey:
    def __init__(self, data):
        self.data = data
        self.key_name = self.data.key_name['web']
        self.key_base_name = self.get_key_base_name(self.key_name)
        self.is_attr = self.check_is_attr()

    def get_file_path(self):
        path_key_name = self._get_best_key_name_for_path()
        key_base_path = path_key_name.split('.', maxsplit=1)[0]
        path_parts = list(filter(None, key_base_path.replace('\\', '::').replace('/', '::').split('::')))
        relative_dir_path = '{relative_file_path}.ftl'.format(
            relative_file_path='/'.join(path_parts)
        )

        return LocalePath(relative_dir_path)

    def _get_best_key_name_for_path(self) -> str:
        key_name_data = self.data.key_name

        if isinstance(key_name_data, str):
            return key_name_data

        if not isinstance(key_name_data, dict):
            return self.key_name

        candidates = []

        for platform in ('desktop', 'web'):
            value = key_name_data.get(platform)
            if isinstance(value, str) and value:
                candidates.append((platform, value))

        for platform, value in key_name_data.items():
            if platform in ('desktop', 'web'):
                continue

            if isinstance(value, str) and value:
                candidates.append((platform, value))

        if not candidates:
            return self.key_name

        def score_key_name(item):
            _, value = item
            path_part = value.split('.', maxsplit=1)[0]
            parts = list(filter(None, path_part.replace('\\', '::').replace('/', '::').split('::')))

            score = len(parts)
            if any(part.startswith('_') for part in parts):
                score += 100

            if any(part.lower() == 'prototypes' for part in parts):
                score += 20

            if any(char.isupper() for char in value):
                score += 5

            return score

        candidates.sort(key=score_key_name, reverse=True)

        return candidates[0][1]

    def get_key_base_name(self, key_name):
        split_name = key_name.split('.')
        return split_name[0]

    def get_key_last_name(self, key_name):
        split_name = key_name.split('.')
        return py_.last(split_name)

    def get_parent_key(self):
        if self.is_attr:
            split_name = self.key_name.split('.')[0:-1]
            return '.'.join(split_name)
        else:
            return None

    def check_is_attr(self):
        # Localize key usually stores attributes as '<message-id>.<attr-name>'
        return len(self.key_name.split('.')) > 1

    def serialize(self):
        if self.is_attr:
            return self.serialize_attr()
        else:
            return self.serialize_message()



    def serialize_attr(self):
        return '.{name} = {value}'.format(name=self.get_key_last_name(self.key_name), value=self.get_translation('ru').data['translation'])

    def serialize_message(self):
        return '{name} = {value}'.format(name=self.get_key_last_name(self.key_name), value=self.get_translation('ru').data['translation'])

    def get_translation(self, language_iso='ru'):
        return list(map(lambda data: LocaliseTranslation(key_name=self.data.key_name['web'], data=data), py_.filter(self.data.translations, {'language_iso': language_iso})))[0]
