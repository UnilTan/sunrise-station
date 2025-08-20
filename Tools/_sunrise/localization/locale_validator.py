#!/usr/bin/env python3
"""
Advanced localization utilities for handling untranslated locales
and cross-language synchronization.
"""

import os
import typing
import logging
from pathlib import Path
from fluent.syntax import ast
from fluent.syntax.parser import FluentParser
from fluent.syntax.serializer import FluentSerializer

from file import FluentFile
from project import Project


class LocaleValidator:
    """Validates and manages locale translations, removing untranslated entries."""
    
    def __init__(self, project: Project):
        self.project = project
        self.parser = FluentParser()
        self.serializer = FluentSerializer(with_junk=True)
        self.removed_entries = []
    
    def is_entry_untranslated(self, entry: ast.Message, locale: str) -> bool:
        """Check if a locale entry is untranslated (identical to English)."""
        if not isinstance(entry, ast.Message):
            return False
        
        # Get the entry content
        serialized = self.serializer.serialize(ast.Resource(body=[entry]))
        
        # Simple heuristic: if it contains common English words or patterns
        # and we're checking Russian locale, it's likely untranslated
        if locale == 'ru-RU':
            english_indicators = [
                'the ', 'and ', 'or ', 'of ', 'to ', 'in ', 'for ',
                'with ', 'by ', 'from ', 'at ', 'on ', 'is ', 'are ',
                'was ', 'were ', 'be ', 'been ', 'have ', 'has ', 'had ',
                'do ', 'does ', 'did ', 'will ', 'would ', 'should ',
                'could ', 'may ', 'might ', 'can ', 'cannot ', 'this ',
                'that ', 'these ', 'those ', 'what ', 'when ', 'where ',
                'why ', 'how ', 'who ', 'which ', 'said ', 'says ',
                'say ', 'get ', 'gets ', 'got ', 'put ', 'puts ',
                'use ', 'uses ', 'used ', 'make ', 'makes ', 'made '
            ]
            
            content_lower = serialized.lower()
            return any(indicator in content_lower for indicator in english_indicators)
        
        return False
    
    def remove_untranslated_from_file(self, file_path: str, locale: str) -> int:
        """Remove untranslated entries from a locale file."""
        try:
            fluent_file = FluentFile(file_path)
            content = fluent_file.read_data()
            parsed = self.parser.parse(content)
            
            original_count = len([e for e in parsed.body if isinstance(e, ast.Message)])
            
            # Filter out untranslated entries
            filtered_body = []
            for entry in parsed.body:
                if isinstance(entry, ast.Message) and self.is_entry_untranslated(entry, locale):
                    self.removed_entries.append((file_path, entry.id.name))
                    logging.debug(f"Removing untranslated entry '{entry.id.name}' from {file_path}")
                else:
                    filtered_body.append(entry)
            
            # Only save if we removed something
            if len(filtered_body) != len(parsed.body):
                if filtered_body:  # File still has content
                    parsed.body = filtered_body
                    new_content = self.serializer.serialize(parsed)
                    fluent_file.save_data(new_content)
                else:  # File is now empty, remove it
                    os.remove(file_path)
                    logging.info(f"Removed empty file: {file_path}")
                
                removed_count = original_count - len([e for e in filtered_body if isinstance(e, ast.Message)])
                return removed_count
            
            return 0
        except Exception as e:
            logging.error(f"Error processing {file_path}: {e}")
            return 0
    
    def clean_untranslated_prototype_locales(self, locale: str) -> dict:
        """Remove untranslated entries from all prototype locale files."""
        results = {
            'files_processed': 0,
            'entries_removed': 0,
            'files_removed': 0
        }
        
        locale_prototypes_path = getattr(self.project, f'{locale.split("-")[0]}_locale_prototypes_dir_path')
        
        for file_path in Path(locale_prototypes_path).rglob("*.ftl"):
            if file_path.is_file():
                removed_count = self.remove_untranslated_from_file(str(file_path), locale)
                results['files_processed'] += 1
                results['entries_removed'] += removed_count
                
                # Check if file was removed
                if not file_path.exists():
                    results['files_removed'] += 1
        
        return results


class CrossLanguageSynchronizer:
    """Synchronizes locales between languages, filling gaps from counterpart language."""
    
    def __init__(self, project: Project):
        self.project = project
        self.parser = FluentParser()
        self.serializer = FluentSerializer(with_junk=True)
        self.synchronized_entries = []
    
    def get_counterpart_file_path(self, file_path: str, from_locale: str, to_locale: str) -> str:
        """Get the path of the counterpart file in another locale."""
        return file_path.replace(f'/{from_locale}/', f'/{to_locale}/')
    
    def extract_entries_from_file(self, file_path: str) -> dict:
        """Extract all entries from a locale file."""
        entries = {}
        try:
            if os.path.exists(file_path):
                fluent_file = FluentFile(file_path)
                content = fluent_file.read_data()
                parsed = self.parser.parse(content)
                
                for entry in parsed.body:
                    if isinstance(entry, ast.Message):
                        entries[entry.id.name] = entry
        except Exception as e:
            logging.error(f"Error reading {file_path}: {e}")
        
        return entries
    
    def merge_missing_entries(self, target_file: str, source_entries: dict) -> int:
        """Merge missing entries from source into target file."""
        if not source_entries:
            return 0
        
        try:
            target_entries = self.extract_entries_from_file(target_file)
            missing_entries = []
            
            # Find entries that exist in source but not in target
            for entry_id, entry in source_entries.items():
                if entry_id not in target_entries:
                    missing_entries.append(entry)
                    self.synchronized_entries.append((target_file, entry_id))
            
            if missing_entries:
                # Create or update target file
                if os.path.exists(target_file):
                    fluent_file = FluentFile(target_file)
                    content = fluent_file.read_data()
                    parsed = self.parser.parse(content)
                else:
                    # Create new file
                    os.makedirs(os.path.dirname(target_file), exist_ok=True)
                    parsed = ast.Resource(body=[])
                    fluent_file = FluentFile(target_file)
                
                # Add missing entries
                parsed.body.extend(missing_entries)
                new_content = self.serializer.serialize(parsed)
                fluent_file.save_data(new_content)
                
                logging.debug(f"Added {len(missing_entries)} entries to {target_file}")
                return len(missing_entries)
        
        except Exception as e:
            logging.error(f"Error merging entries into {target_file}: {e}")
        
        return 0
    
    def synchronize_strings_locales(self) -> dict:
        """Synchronize _strings directories between EN and RU."""
        results = {
            'en_to_ru': 0,
            'ru_to_en': 0,
            'files_processed': 0
        }
        
        en_strings_path = os.path.join(self.project.en_locale_dir_path, '_strings')
        ru_strings_path = os.path.join(self.project.ru_locale_dir_path, '_strings')
        
        # Process EN -> RU
        if os.path.exists(en_strings_path):
            for file_path in Path(en_strings_path).rglob("*.ftl"):
                if file_path.is_file():
                    ru_file_path = self.get_counterpart_file_path(str(file_path), 'en-US', 'ru-RU')
                    en_entries = self.extract_entries_from_file(str(file_path))
                    added_count = self.merge_missing_entries(ru_file_path, en_entries)
                    results['en_to_ru'] += added_count
                    if added_count > 0:
                        results['files_processed'] += 1
        
        # Process RU -> EN
        if os.path.exists(ru_strings_path):
            for file_path in Path(ru_strings_path).rglob("*.ftl"):
                if file_path.is_file():
                    en_file_path = self.get_counterpart_file_path(str(file_path), 'ru-RU', 'en-US')
                    ru_entries = self.extract_entries_from_file(str(file_path))
                    added_count = self.merge_missing_entries(en_file_path, ru_entries)
                    results['ru_to_en'] += added_count
                    if added_count > 0:
                        results['files_processed'] += 1
        
        return results