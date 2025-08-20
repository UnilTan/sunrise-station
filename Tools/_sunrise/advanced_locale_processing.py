#!/usr/bin/env python3
"""
Advanced localization processing script.
Runs all the advanced functionality improvements for the localization system.
"""

import sys
import os
import logging

# Add the localization tools to the path
sys.path.append(os.path.join(os.path.dirname(__file__), 'localization'))

from project import Project
from file_cleanup import FileCleanup
from locale_validator import LocaleValidator, CrossLanguageSynchronizer


def main():
    """Run advanced localization processing."""
    logging.basicConfig(level=logging.INFO, format='%(levelname)s: %(message)s')
    
    project = Project()
    
    print("=== Расширенная обработка локализации ===")
    
    # Step 1: Remove untranslated prototype locales
    print("\n1. Удаление непереведенных локалей из прототипов...")
    validator = LocaleValidator(project)
    ru_cleanup_results = validator.clean_untranslated_prototype_locales('ru-RU')
    print(f"   Очистка RU прототипов: обработано {ru_cleanup_results['files_processed']} файлов")
    print(f"   Удалено {ru_cleanup_results['entries_removed']} непереведенных записей")
    print(f"   Удалено {ru_cleanup_results['files_removed']} пустых файлов")
    
    # Step 2: Cross-language synchronization
    print("\n2. Синхронизация строковых локалей между языками...")
    synchronizer = CrossLanguageSynchronizer(project)
    sync_results = synchronizer.synchronize_strings_locales()
    print(f"   EN→RU: добавлено {sync_results['en_to_ru']} записей")
    print(f"   RU→EN: добавлено {sync_results['ru_to_en']} записей")
    print(f"   Обработано {sync_results['files_processed']} файлов")
    
    # Step 3: File cleanup
    print("\n3. Очистка файлов и директорий...")
    cleanup = FileCleanup(project.base_dir_path)
    
    total_processed = 0
    total_empty_removed = 0
    total_dirs_removed = 0
    
    for locale in ['en-US', 'ru-RU']:
        locale_path = os.path.join(project.locales_dir_path, locale)
        results = cleanup.process_locale_files(locale_path)
        
        total_processed += results['processed']
        total_empty_removed += len(results['removed_empty'])
        total_dirs_removed += len(results['removed_dirs'])
        
        print(f"   {locale}: {results['processed']} файлов, "
              f"{len(results['removed_empty'])} пустых файлов удалено, "
              f"{len(results['removed_dirs'])} пустых директорий удалено")
        
        if results['failed'] > 0:
            print(f"   ВНИМАНИЕ: Не удалось обработать {results['failed']} файлов в {locale}")
    
    print(f"\n=== Итоги ===")
    print(f"Всего обработано файлов: {total_processed}")
    print(f"Удалено пустых файлов: {total_empty_removed}")
    print(f"Удалено пустых директорий: {total_dirs_removed}")
    print(f"Удалено непереведенных записей: {ru_cleanup_results['entries_removed']}")
    print(f"Синхронизировано записей: {sync_results['en_to_ru'] + sync_results['ru_to_en']}")
    print("\nОбработка завершена успешно!")


if __name__ == "__main__":
    main()