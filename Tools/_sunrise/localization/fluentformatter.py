#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) в соответствие стайлгайду
# path - путь к папке, содержащий форматируемые файлы. Для форматирования всего проекта, необходимо заменить значение на root_dir_path
import typing

from file import FluentFile
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


######################################### Class defifitions ############################################################

class FluentFormatter:
    @classmethod
    def format(cls, fluent_files: typing.List[FluentFile]):
        for file in fluent_files:
            file_data = file.read_data()
            parsed_file_data = file.parse_data(file_data)
            serialized_file_data = cls.format_serialized_file_data(file_data)
            file.save_data(serialized_file_data)

    @classmethod
    def format_serialized_file_data(cls, file_data: typing.AnyStr):
        parsed_data = FluentParser().parse(file_data)

        serialized_data = FluentSerializer(with_junk=True).serialize(parsed_data)
        
        # Preserve indentation and spacing - only process multiline tags
        lines = serialized_data.split('\n')
        formatted_lines = []
        
        for line in lines:
            # Handle multiline locales starting with tags - add zero-width space for escaping
            if (line.strip().startswith('[color=') or 
                line.strip().startswith('[bold]') or 
                line.strip().startswith('[font') or
                line.strip().startswith('**')):
                # Add zero-width space (U+200B) for escaping if this starts a value
                if formatted_lines and not formatted_lines[-1].strip().endswith('='):
                    # This is a continuation of a multiline value starting with tags
                    line = '\u200B' + line.strip()
                formatted_lines.append(line)
            else:
                formatted_lines.append(line)
        
        # Ensure proper line ending (only at the very end)
        result = '\n'.join(formatted_lines)
        result = result.rstrip() + '\n'
        
        return result



######################################## Var definitions ###############################################################
project = Project()
fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)

########################################################################################################################

FluentFormatter.format(fluent_files)
