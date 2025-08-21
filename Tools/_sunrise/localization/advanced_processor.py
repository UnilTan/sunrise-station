#!/usr/bin/env python3

"""
Advanced locale processor for Sunrise Station 14
Implements advanced functionality for locale processing:
- Removes untranslated locales from _prototypes using Locale Validator mechanism
- Syncs missing localizations between en-US and ru-RU folders
"""

import os
import re
import logging
import typing
from tqdm import tqdm
from fluent.syntax import ast, FluentParser, FluentSerializer
from file import FluentFile
from project import Project


class AdvancedLocaleProcessor:
    def __init__(self):
        self.project = Project()
        self.parser = FluentParser()
        self.serializer = FluentSerializer(with_junk=True)
        self.processed_files = []

    def is_english(self, text: str) -> bool:
        """Check if text contains English characters"""
        return bool(re.search(r'[a-zA-Z]', text))

    def has_russian(self, text: str) -> bool:
        """Check if text contains Russian characters"""
        return bool(re.search(r'[а-яА-Я]', text))

    def remove_markup_content(self, text: str) -> str:
        """Remove markup content like {variables}, [tags], <elements>"""
        text = re.sub(r'\{.*?\}', '', text)
        text = re.sub(r'\[.*?\]', '', text)
        text = re.sub(r'\<.*?\>', '', text)
        return text.strip()

    def is_untranslated(self, message: ast.Message, expected_language: str) -> bool:
        """
        Check if a message is untranslated based on language detection
        expected_language: 'ru' or 'en'
        """
        if not message.value:
            return True
            
        # Get the text content
        text_content = self.extract_text_from_pattern(message.value)
        clean_text = self.remove_markup_content(text_content)
        
        if not clean_text:
            return False  # Consider empty/markup-only as translated
            
        if expected_language == 'ru':
            # Russian file should contain Russian text
            return not self.has_russian(clean_text) and self.is_english(clean_text)
        elif expected_language == 'en':
            # English file should contain English text  
            return not self.is_english(clean_text) and self.has_russian(clean_text)
        
        return False

    def extract_text_from_pattern(self, pattern) -> str:
        """Extract text content from a Fluent pattern"""
        if hasattr(pattern, 'elements'):
            text_parts = []
            for element in pattern.elements:
                if hasattr(element, 'value'):  # TextElement
                    text_parts.append(element.value)
            return ' '.join(text_parts)
        return str(pattern) if pattern else ''

    def remove_untranslated_locales(self):
        """Remove untranslated locales from _prototypes directories"""
        logging.info("Удаление непереведенных локалей из _prototypes...")
        
        # Process Russian prototypes
        self._process_prototype_dir(self.project.ru_locale_prototypes_dir_path, 'ru')
        
        # Process English prototypes  
        self._process_prototype_dir(self.project.en_locale_prototypes_dir_path, 'en')

    def _process_prototype_dir(self, proto_dir: str, language: str):
        """Process a prototype directory to remove untranslated entries"""
        if not os.path.exists(proto_dir):
            return
            
        fluent_files = self.project.get_fluent_files_by_dir(proto_dir)
        
        for fluent_file in tqdm(fluent_files, desc=f"Обработка {language} прототипов"):
            self._clean_untranslated_file(fluent_file, language)

    def _clean_untranslated_file(self, fluent_file: FluentFile, language: str):
        """Remove untranslated entries from a single file"""
        try:
            data = fluent_file.read_data()
            parsed = self.parser.parse(data)
            
            original_count = len([e for e in parsed.body if isinstance(e, ast.Message)])
            
            # Filter out untranslated messages
            new_body = []
            removed_count = 0
            
            for entry in parsed.body:
                if isinstance(entry, ast.Message):
                    if self.is_untranslated(entry, language):
                        removed_count += 1
                        continue
                new_body.append(entry)
            
            if removed_count > 0:
                parsed.body = new_body
                
                # Check if file is now empty
                has_messages = any(isinstance(e, ast.Message) for e in new_body)
                
                if not has_messages:
                    # Remove empty file
                    if os.path.exists(fluent_file.full_path):
                        os.remove(fluent_file.full_path)
                        rel_path = os.path.relpath(fluent_file.full_path, self.project.base_dir_path)
                        logging.info(f"Удален пустой файл: {rel_path}")
                else:
                    # Save cleaned file
                    fluent_file.save_data(self.serializer.serialize(parsed))
                    rel_path = os.path.relpath(fluent_file.full_path, self.project.base_dir_path)
                    logging.info(f"Удалено {removed_count} непереведенных записей из {rel_path}")
                    
                self.processed_files.append(fluent_file.full_path)
                    
        except Exception as e:
            rel_path = os.path.relpath(fluent_file.full_path, self.project.base_dir_path)
            logging.error(f"Ошибка при обработке {rel_path}: {e}")

    def sync_missing_localizations(self):
        """
        Sync missing localizations between en-US and ru-RU folders.
        If a localization doesn't exist in Russian folder, take it from English.
        Similarly, for English, take from Russian if it doesn't exist in English.
        """
        logging.info("Синхронизация отсутствующих локализаций между языками...")
        
        # Get all unique relative paths
        en_files = self.project.get_fluent_files_by_dir(self.project.en_locale_dir_path)
        ru_files = self.project.get_fluent_files_by_dir(self.project.ru_locale_dir_path)
        
        # Create mappings of relative paths to files
        en_file_map = {}
        ru_file_map = {}
        
        for f in en_files:
            rel_path = os.path.relpath(f.full_path, self.project.en_locale_dir_path)
            en_file_map[rel_path] = f
            
        for f in ru_files:
            rel_path = os.path.relpath(f.full_path, self.project.ru_locale_dir_path)
            ru_file_map[rel_path] = f
        
        all_paths = set(en_file_map.keys()) | set(ru_file_map.keys())
        
        for rel_path in tqdm(all_paths, desc="Синхронизация файлов"):
            en_file = en_file_map.get(rel_path)
            ru_file = ru_file_map.get(rel_path)
            
            if en_file and not ru_file:
                # Create Russian file from English
                self._create_missing_file(en_file, self.project.ru_locale_dir_path, rel_path, 'ru')
                
            elif ru_file and not en_file:
                # Create English file from Russian
                self._create_missing_file(ru_file, self.project.en_locale_dir_path, rel_path, 'en')
                
            elif en_file and ru_file:
                # Both exist, sync missing keys
                self._sync_missing_keys(en_file, ru_file)

    def _create_missing_file(self, source_file: FluentFile, target_dir: str, rel_path: str, target_lang: str):
        """Create a missing file by copying from the source language"""
        target_path = os.path.join(target_dir, rel_path)
        
        try:
            source_data = source_file.read_data()
            target_file = FluentFile(target_path)
            target_file.save_data(source_data)
            
            rel_source = os.path.relpath(source_file.full_path, self.project.base_dir_path)
            rel_target = os.path.relpath(target_path, self.project.base_dir_path)
            logging.info(f"Создан файл {rel_target} на основе {rel_source}")
            
            self.processed_files.append(target_path)
            
        except Exception as e:
            logging.error(f"Ошибка при создании файла {target_path}: {e}")

    def _sync_missing_keys(self, en_file: FluentFile, ru_file: FluentFile):
        """Sync missing keys between English and Russian files"""
        try:
            en_data = en_file.read_data()
            ru_data = ru_file.read_data()
            
            en_parsed = self.parser.parse(en_data)
            ru_parsed = self.parser.parse(ru_data)
            
            # Get existing keys
            en_keys = {e.id.name: e for e in en_parsed.body if isinstance(e, ast.Message)}
            ru_keys = {e.id.name: e for e in ru_parsed.body if isinstance(e, ast.Message)}
            
            en_modified = False
            ru_modified = False
            
            # Add missing keys to Russian from English
            for key, message in en_keys.items():
                if key not in ru_keys:
                    ru_parsed.body.append(message)
                    ru_modified = True
                    
            # Add missing keys to English from Russian  
            for key, message in ru_keys.items():
                if key not in en_keys:
                    en_parsed.body.append(message)
                    en_modified = True
            
            # Save modified files
            if en_modified:
                en_file.save_data(self.serializer.serialize(en_parsed))
                rel_path = os.path.relpath(en_file.full_path, self.project.base_dir_path)
                logging.debug(f"Добавлены ключи в {rel_path}")
                self.processed_files.append(en_file.full_path)
                
            if ru_modified:
                ru_file.save_data(self.serializer.serialize(ru_parsed))
                rel_path = os.path.relpath(ru_file.full_path, self.project.base_dir_path)
                logging.debug(f"Добавлены ключи в {rel_path}")
                self.processed_files.append(ru_file.full_path)
                
        except Exception as e:
            rel_en = os.path.relpath(en_file.full_path, self.project.base_dir_path)
            rel_ru = os.path.relpath(ru_file.full_path, self.project.base_dir_path)
            logging.error(f"Ошибка при синхронизации {rel_en} <-> {rel_ru}: {e}")

    def cleanup_empty_directories(self):
        """Clean up empty directories after processing"""
        locale_dirs = [self.project.en_locale_dir_path, self.project.ru_locale_dir_path]
        
        for locale_dir in locale_dirs:
            if os.path.exists(locale_dir):
                self._remove_empty_dirs_recursive(locale_dir, locale_dir)

    def _remove_empty_dirs_recursive(self, directory: str, base_dir: str):
        """Recursively remove empty directories"""
        if not os.path.exists(directory):
            return
            
        try:
            for item in os.listdir(directory):
                item_path = os.path.join(directory, item)
                if os.path.isdir(item_path):
                    self._remove_empty_dirs_recursive(item_path, base_dir)
        except PermissionError:
            return
        
        if directory != base_dir:
            try:
                if not os.listdir(directory):
                    os.rmdir(directory)
                    rel_path = os.path.relpath(directory, self.project.base_dir_path)
                    logging.info(f"Удалена пустая папка: {rel_path}")
            except (OSError, PermissionError):
                pass

    def execute(self):
        """Execute all advanced processing steps"""
        self.processed_files = []
        
        logging.info("Запуск расширенной обработки локалей...")
        
        # Step 1: Remove untranslated locales
        self.remove_untranslated_locales()
        
        # Step 2: Sync missing localizations
        self.sync_missing_localizations()
        
        # Step 3: Clean up empty directories
        self.cleanup_empty_directories()
        
        logging.info(f"Расширенная обработка завершена. Обработано файлов: {len(set(self.processed_files))}")
        
        return list(set(self.processed_files))


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    processor = AdvancedLocaleProcessor()
    processor.execute()