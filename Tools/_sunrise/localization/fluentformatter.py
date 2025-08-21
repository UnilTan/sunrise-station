#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) в соответствие стайлгайду
# path - путь к папке, содержащий форматируемые файлы. Для форматирования всего проекта, необходимо заменить значение на root_dir_path
import typing
import re
import os

from file import FluentFile
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


######################################### Class defifitions ############################################################

class FluentFormatter:
    @classmethod
    def format(cls, fluent_files: typing.List[FluentFile]):
        files_to_remove = []
        for file in fluent_files:
            file_data = file.read_data()
            parsed_file_data = file.parse_data(file_data)
            serialized_file_data = cls.format_serialized_file_data(file_data)
            
            # Check if file is effectively empty after formatting
            if cls.is_file_empty(serialized_file_data):
                files_to_remove.append(file.full_path)
            else:
                file.save_data(serialized_file_data)
        
        # Remove empty files
        for file_path in files_to_remove:
            cls.remove_empty_file(file_path)
        
        # Clean up empty directories
        cls.cleanup_empty_directories()

    @classmethod
    def format_serialized_file_data(cls, file_data: typing.AnyStr):
        parsed_data = FluentParser().parse(file_data)
        serialized_data = FluentSerializer(with_junk=True).serialize(parsed_data)
        
        # Process lines while preserving meaningful whitespace
        lines = serialized_data.split('\n')
        formatted_lines = []
        
        for line in lines:
            # Handle tag concatenation on same line (existing functionality)
            if (line.strip().startswith('[color=') or 
                line.strip().startswith('[bold]') or 
                line.strip().startswith('[font') or
                line.strip().startswith('**')):
                if formatted_lines:
                    formatted_lines[-1] += ' ' + line.strip()
                else:
                    formatted_lines.append(line)
            else:
                formatted_lines.append(line)
        
        # Process the result to handle proper formatting
        result = cls.process_file_formatting(formatted_lines)
        return result

    @classmethod
    def process_file_formatting(cls, lines: typing.List[str]) -> str:
        """
        Process file formatting:
        - Preserve meaningful indentation between locales (don't remove all whitespace)
        - Only trim newlines at the beginning and end of file
        - Ensure exactly one newline at the end
        - Auto-escape multiline locales starting with tags using zero-width space
        """
        # Remove leading empty lines
        while lines and not lines[0].strip():
            lines.pop(0)
        
        # Remove trailing empty lines
        while lines and not lines[-1].strip():
            lines.pop()
        
        # Process lines for tag escaping
        processed_lines = []
        for line in lines:
            processed_line = cls.escape_multiline_tags(line)
            processed_lines.append(processed_line)
        
        # Join lines and ensure exactly one newline at the end
        if processed_lines:
            result = '\n'.join(processed_lines) + '\n'
        else:
            result = ''
            
        return result

    @classmethod
    def escape_multiline_tags(cls, line: str) -> str:
        """
        Automatically escape multiline locales starting with tags using zero-width space.
        Tags include [bold], [color=], [font], etc.
        """
        stripped = line.strip()
        
        # Check if line starts with a tag and is part of a multiline locale
        tag_patterns = [
            r'^\[bold\]',
            r'^\[/bold\]', 
            r'^\[color=',
            r'^\[/color\]',
            r'^\[font',
            r'^\[/font\]',
            r'^\[italic\]',
            r'^\[/italic\]',
            r'^\[underline\]',
            r'^\[/underline\]'
        ]
        
        for pattern in tag_patterns:
            if re.match(pattern, stripped):
                # Add zero-width space (U+200B) at the beginning to escape the tag
                indentation = line[:len(line) - len(line.lstrip())]
                return indentation + '\u200B' + stripped
        
        return line

    @classmethod
    def is_file_empty(cls, file_content: str) -> bool:
        """Check if file content is effectively empty (only whitespace/comments)"""
        if not file_content.strip():
            return True
            
        # Parse to check if there are any actual locale entries
        try:
            parsed = FluentParser().parse(file_content)
            has_messages = any(isinstance(entry, ast.Message) for entry in parsed.body)
            return not has_messages
        except:
            # If parsing fails, consider file empty
            return True

    @classmethod 
    def remove_empty_file(cls, file_path: str):
        """Remove an empty file"""
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
                print(f"Удален пустой файл: {file_path}")
        except Exception as e:
            print(f"Ошибка при удалении файла {file_path}: {e}")

    @classmethod
    def cleanup_empty_directories(cls):
        """Remove empty directories in locale folders"""
        project = Project()
        locale_dirs = [project.en_locale_dir_path, project.ru_locale_dir_path]
        
        for locale_dir in locale_dirs:
            if os.path.exists(locale_dir):
                cls._remove_empty_dirs_recursive(locale_dir, locale_dir)

    @classmethod 
    def _remove_empty_dirs_recursive(cls, directory: str, base_dir: str):
        """Recursively remove empty directories, but don't remove the base directory itself"""
        if not os.path.exists(directory):
            return
            
        # First, recursively process subdirectories
        for item in os.listdir(directory):
            item_path = os.path.join(directory, item)
            if os.path.isdir(item_path):
                cls._remove_empty_dirs_recursive(item_path, base_dir)
        
        # Then check if current directory is empty and can be removed
        # Don't remove the base locale directory itself (en-US, ru-RU)
        if directory != base_dir:
            try:
                if not os.listdir(directory):  # Directory is empty
                    os.rmdir(directory)
                    print(f"Удалена пустая папка: {directory}")
            except Exception as e:
                print(f"Ошибка при удалении папки {directory}: {e}")



######################################## Var definitions ###############################################################
project = Project()
fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)

########################################################################################################################

FluentFormatter.format(fluent_files)
