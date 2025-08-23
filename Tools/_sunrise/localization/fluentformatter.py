#!/usr/bin/env python3

# Форматтер, приводящий fluent-файлы (.ftl) в соответствие стайлгайду
# path - путь к папке, содержащий форматируемые файлы. Для форматирования всего проекта, необходимо заменить значение на root_dir_path
import typing
import os
import re

from file import FluentFile
from project import Project
from fluent.syntax import ast, FluentParser, FluentSerializer


######################################### Class defifitions ############################################################

class FluentFormatter:
    ZERO_WIDTH_SPACE = '\u200B'  # Zero-width space character
    TAG_PATTERNS = [
        r'^\s*\[bold\]',
        r'^\s*\[/bold\]', 
        r'^\s*\[italic\]',
        r'^\s*\[/italic\]',
        r'^\s*\[color=',
        r'^\s*\[/color\]',
        r'^\s*\[font',
        r'^\s*\[/font\]'
    ]
    
    @classmethod
    def format(cls, fluent_files: typing.List[FluentFile], delete_empty_files: bool = True):
        files_to_delete = []
        
        for file in fluent_files:
            file_data = file.read_data()
            formatted_data = cls.format_serialized_file_data(file_data)
            
            # Check if file becomes empty after formatting
            if delete_empty_files and cls._is_effectively_empty(formatted_data):
                files_to_delete.append(file.full_path)
            else:
                file.save_data(formatted_data)
        
        # Delete empty files
        for file_path in files_to_delete:
            try:
                os.remove(file_path)
                print(f"Deleted empty file: {file_path}")
            except OSError as e:
                print(f"Failed to delete {file_path}: {e}")
        
        # Clean up empty directories
        if delete_empty_files:
            cls._cleanup_empty_directories(fluent_files)

    @classmethod
    def _is_effectively_empty(cls, content: str) -> bool:
        """Check if content is effectively empty (only whitespace and newlines)"""
        return not content.strip()

    @classmethod
    def _cleanup_empty_directories(cls, fluent_files: typing.List[FluentFile]):
        """Remove empty directories after file cleanup"""
        directories_to_check = set()
        
        # Collect all directories that might need cleanup
        for file in fluent_files:
            parent_dir = os.path.dirname(file.full_path)
            directories_to_check.add(parent_dir)
            
            # Also add parent directories up the tree
            while parent_dir:
                parent_dir = os.path.dirname(parent_dir)
                if parent_dir:
                    directories_to_check.add(parent_dir)
        
        # Sort by depth (deepest first) to clean up bottom-up
        sorted_dirs = sorted(directories_to_check, key=lambda x: x.count(os.sep), reverse=True)
        
        for dir_path in sorted_dirs:
            try:
                if os.path.exists(dir_path) and not os.listdir(dir_path):
                    os.rmdir(dir_path)
                    print(f"Deleted empty directory: {dir_path}")
            except OSError:
                # Directory not empty or other error - that's fine
                pass

    @classmethod
    def format_serialized_file_data(cls, file_data: typing.AnyStr) -> str:
        parsed_data = FluentParser().parse(file_data)
        serialized_data = FluentSerializer(with_junk=True).serialize(parsed_data)
        
        # Process the content with our improvements
        formatted_content = cls._apply_formatting_improvements(serialized_data)
        
        return formatted_content

    @classmethod
    def _apply_formatting_improvements(cls, content: str) -> str:
        """Apply all formatting improvements according to requirements"""
        lines = content.split('\n')
        
        # Remove leading and trailing empty lines only (preserve internal spacing)
        while lines and not lines[0].strip():
            lines.pop(0)
        while lines and not lines[-1].strip():
            lines.pop()
        
        # Process lines for tag escaping and multiline handling
        processed_lines = []
        i = 0
        while i < len(lines):
            line = lines[i]
            
            # Check if this is a multiline locale entry starting with tags
            if '=' in line and not line.strip().startswith('#'):
                # Find the value part after '='
                parts = line.split('=', 1)
                if len(parts) == 2:
                    key_part = parts[0]
                    value_part = parts[1].strip()
                    
                    # Check if it's a multiline entry (value is empty or continues on next line)
                    if not value_part or i + 1 < len(lines):
                        multiline_content = []
                        j = i + 1
                        
                        # Collect multiline content
                        while j < len(lines) and (lines[j].startswith('\t') or lines[j].startswith('    ')):
                            multiline_content.append(lines[j])
                            j += 1
                        
                        # Process multiline content for tag escaping
                        if multiline_content:
                            escaped_content = cls._escape_multiline_tags(multiline_content)
                            processed_lines.append(line)
                            processed_lines.extend(escaped_content)
                            i = j
                            continue
            
            processed_lines.append(line)
            i += 1
        
        # Ensure exactly one newline at the end
        result = '\n'.join(processed_lines)
        if result and not result.endswith('\n'):
            result += '\n'
        elif result.endswith('\n\n'):
            # Remove extra newlines, keep only one
            result = result.rstrip('\n') + '\n'
            
        return result

    @classmethod 
    def _escape_multiline_tags(cls, multiline_content: typing.List[str]) -> typing.List[str]:
        """Add zero-width space before lines starting with tags in multiline content"""
        escaped_lines = []
        
        for line in multiline_content:
            original_line = line
            stripped_content = line.lstrip('\t ') # Remove leading whitespace to check content
            
            # Check if line starts with a tag
            starts_with_tag = any(re.match(pattern, stripped_content) for pattern in cls.TAG_PATTERNS)
            
            if starts_with_tag:
                # Find the indentation
                indent = line[:len(line) - len(line.lstrip())]
                content = line[len(indent):]
                # Add zero-width space at the beginning of the actual content
                escaped_line = indent + cls.ZERO_WIDTH_SPACE + content
                escaped_lines.append(escaped_line)
            else:
                escaped_lines.append(original_line)
                
        return escaped_lines


    @classmethod
    def sync_cross_locale_keys(cls, project: Project):
        """Synchronize locale keys between Russian and English locales"""
        print("Starting cross-locale synchronization...")
        
        # Get all Russian and English fluent files
        ru_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)
        en_files = project.get_fluent_files_by_dir(project.en_locale_dir_path)
        
        # Create mappings of relative paths to files
        ru_file_map = {}
        en_file_map = {}
        
        for file in ru_files:
            rel_path = file.get_relative_path_without_extension(project.ru_locale_dir_path)
            ru_file_map[rel_path] = file
            
        for file in en_files:
            rel_path = file.get_relative_path_without_extension(project.en_locale_dir_path)
            en_file_map[rel_path] = file
        
        # Find files that exist in one locale but not the other
        all_paths = set(ru_file_map.keys()) | set(en_file_map.keys())
        
        for rel_path in all_paths:
            ru_file = ru_file_map.get(rel_path)
            en_file = en_file_map.get(rel_path)
            
            if ru_file and en_file:
                # Both files exist, sync missing keys
                cls._sync_keys_between_files(ru_file, en_file)
            elif ru_file and not en_file:
                # Russian file exists, create English file
                en_path = os.path.join(project.en_locale_dir_path, rel_path + '.ftl')
                cls._copy_file_structure(ru_file, en_path)
                print(f"Created missing English file: {en_path}")
            elif en_file and not ru_file:
                # English file exists, create Russian file
                ru_path = os.path.join(project.ru_locale_dir_path, rel_path + '.ftl')
                cls._copy_file_structure(en_file, ru_path)
                print(f"Created missing Russian file: {ru_path}")

    @classmethod
    def _sync_keys_between_files(cls, file1: FluentFile, file2: FluentFile):
        """Sync missing keys between two locale files"""
        try:
            data1 = file1.read_data()
            data2 = file2.read_data()
            
            parsed1 = file1.parse_data(data1)
            parsed2 = file2.parse_data(data2)
            
            keys1 = cls._extract_keys_from_parsed_data(parsed1)
            keys2 = cls._extract_keys_from_parsed_data(parsed2)
            
            # Find missing keys
            missing_in_2 = keys1 - keys2
            missing_in_1 = keys2 - keys1
            
            if missing_in_2 or missing_in_1:
                print(f"Syncing keys between {file1.full_path} and {file2.full_path}")
                
            # Add missing keys to file2
            if missing_in_2:
                updated_data2 = cls._add_missing_keys_to_content(data2, data1, missing_in_2)
                file2.save_data(updated_data2)
                
            # Add missing keys to file1  
            if missing_in_1:
                updated_data1 = cls._add_missing_keys_to_content(data1, data2, missing_in_1)
                file1.save_data(updated_data1)
                
        except Exception as e:
            print(f"Error syncing {file1.full_path} and {file2.full_path}: {e}")

    @classmethod
    def _extract_keys_from_parsed_data(cls, parsed_data) -> set:
        """Extract all message keys from parsed fluent data"""
        keys = set()
        for entry in parsed_data.body:
            if isinstance(entry, ast.Message):
                keys.add(entry.id.name)
        return keys

    @classmethod
    def _add_missing_keys_to_content(cls, target_content: str, source_content: str, missing_keys: set) -> str:
        """Add missing keys from source content to target content"""
        if not missing_keys:
            return target_content
            
        # Parse source to get the missing entries
        parser = FluentParser()
        source_parsed = parser.parse(source_content)
        
        # Find the actual entries for missing keys
        missing_entries = []
        for entry in source_parsed.body:
            if isinstance(entry, ast.Message) and entry.id.name in missing_keys:
                missing_entries.append(entry)
        
        if not missing_entries:
            return target_content
            
        # Serialize the missing entries
        serializer = FluentSerializer(with_junk=True)
        missing_content_lines = []
        
        for entry in missing_entries:
            # Create a temporary resource with just this entry
            temp_resource = ast.Resource([entry])
            entry_content = serializer.serialize(temp_resource).strip()
            missing_content_lines.append(entry_content)
        
        # Add missing entries to target content
        target_lines = target_content.split('\n')
        
        # Remove trailing empty lines
        while target_lines and not target_lines[-1].strip():
            target_lines.pop()
            
        # Add missing entries
        if target_lines:  # If file has content, add a separator
            target_lines.append('')
            
        target_lines.extend(missing_content_lines)
        
        return '\n'.join(target_lines) + '\n'

    @classmethod
    def _copy_file_structure(cls, source_file: FluentFile, target_path: str):
        """Copy a file structure to create a missing locale file"""
        try:
            source_data = source_file.read_data() 
            
            # Create target directory if it doesn't exist
            os.makedirs(os.path.dirname(target_path), exist_ok=True)
            
            # Save the content to the new file
            with open(target_path, 'w', encoding='utf8') as f:
                f.write(source_data)
                
        except Exception as e:
            print(f"Error copying file structure from {source_file.full_path} to {target_path}: {e}")


if __name__ == "__main__":
    ######################################## Var definitions ###############################################################
    project = Project()

    # Process both Russian and English locales
    print("Formatting Russian locale files...")
    ru_fluent_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)
    FluentFormatter.format(ru_fluent_files)

    print("Formatting English locale files...")
    en_fluent_files = project.get_fluent_files_by_dir(project.en_locale_dir_path)
    FluentFormatter.format(en_fluent_files)

    # Synchronize cross-locale keys
    FluentFormatter.sync_cross_locale_keys(project)

    print("Localization formatting and synchronization complete!")

########################################################################################################################
