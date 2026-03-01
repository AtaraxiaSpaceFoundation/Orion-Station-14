import os
import logging
import typing
from fluent.syntax import FluentParser, FluentSerializer, ast
from pydash import py_
from file import FluentFile
from fluentast import FluentSerializedMessage
from localise_fluent_ast_comparer_manager import LocaliseFluentAstComparerManager
from localise_project import LocaliseProject
from localisemodels import LocaliseKey

######################################### Class definitions ############################################################

# TODO непереведенные элементы приходят как { "" }. Необходимо сохранять английский перевод
class TranslationsAssembler:
    def __init__(self, items: typing.List[LocaliseKey]):
        self.group = py_.group_by(items, 'key_base_name')
        keys = list(self.group.keys())
        self.sorted_keys = py_.sort_by(keys, lambda key: self.sort_by_translations_timestamp(self.group[key]),
                                       reverse=True)

    def _find_message_by_id(self, parsed: ast.Resource, message_id: str):
        for element in parsed.body:
            if isinstance(element, ast.Message) and element.id.name == message_id:
                return element

        return None

    def _is_reference_pattern(self, pattern: ast.Pattern | None) -> bool:
        if not pattern or not pattern.elements or len(pattern.elements) != 1:
            return False

        return isinstance(pattern.elements[0], ast.Placeable)

    def _is_empty_pattern(self, pattern: ast.Pattern | None) -> bool:
        if not pattern:
            return True

        if not pattern.elements:
            return True

        if len(pattern.elements) != 1:
            return False

        first_element = pattern.elements[0]
        if isinstance(first_element, ast.TextElement):
            return first_element.value.strip() == ''

        if isinstance(first_element, ast.Placeable) and isinstance(first_element.expression, ast.StringLiteral):
            return first_element.expression.value.strip() == ''

        return False

    def _apply_en_reference_fallbacks(self, target_parsed: ast.Resource, en_parsed: ast.Resource):
        for element in target_parsed.body:
            if not isinstance(element, ast.Message):
                continue

            en_message = self._find_message_by_id(en_parsed, element.id.name)
            if not en_message:
                continue

            if self._is_reference_pattern(en_message.value) or self._is_empty_pattern(element.value):
                element.value = en_message.value

            en_attrs = {attr.id.name: attr for attr in en_message.attributes}
            for target_attr in element.attributes:
                en_attr = en_attrs.get(target_attr.id.name)
                if not en_attr:
                    continue

                if self._is_reference_pattern(en_attr.value) or self._is_empty_pattern(target_attr.value):
                    target_attr.value = en_attr.value


    def execute(self):
        for key in self.sorted_keys:
            full_message = FluentSerializedMessage.from_localise_keys(self.group[key])
            parsed_message = FluentParser().parse(full_message)
            file_paths = self.group[key][0].get_file_path()
            ru_file = FluentFile(file_paths.ru)

            try:
                ru_file_parsed = ru_file.read_parsed_data()
            except FileNotFoundError:
                logging.exception(f'Файла {ru_file.full_path} не существует')
                continue

            if os.path.isfile(file_paths.en):
                en_file = FluentFile(file_paths.en)
                en_file_parsed = en_file.read_parsed_data()
                self._apply_en_reference_fallbacks(parsed_message, en_file_parsed)

            manager = LocaliseFluentAstComparerManager(source_parsed=ru_file_parsed, target_parsed=parsed_message)

            for_update = manager.for_update()
            manager.for_create()
            manager.for_delete()

            if len(for_update):
                updated_ru_file_parsed = manager.update(for_update)
                updated_ru_file_serialized = FluentSerializer(with_junk=True).serialize(updated_ru_file_parsed)
                ru_file.save_data(updated_ru_file_serialized)

                updated_keys = list(map(lambda el: el.get_id_name(), for_update))
                logging.info(f'Обновлены ключи: {updated_keys} в файле {ru_file.full_path}')

    def sort_by_translations_timestamp(self, list):
        sorted_list = py_.sort_by(list, 'data.translations_modified_at_timestamp', reverse=True)

        return sorted_list[0].data.translations_modified_at_timestamp

######################################## Var definitions ###############################################################

logging.basicConfig(level=logging.INFO)
localise_project_id = os.getenv('localise_project_id')
localise_personal_token = os.getenv('localise_personal_token')
localise_project = LocaliseProject(project_id=localise_project_id,
                                   personal_token=localise_personal_token)
all_keys: typing.List[LocaliseKey] = localise_project.get_all_keys()
translations_assembler = TranslationsAssembler(all_keys)

########################################################################################################################

translations_assembler.execute()
