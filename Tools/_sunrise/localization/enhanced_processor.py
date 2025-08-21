#!/usr/bin/env python3

"""
Enhanced localization processor for Sunrise Station 14
Integrates all enhanced functionality for localization processing
"""

import logging
import argparse
from yamlextractor import YAMLExtractor
from keyfinder import FilesFinder, KeyFinder
from fluentformatter import FluentFormatter
from advanced_processor import AdvancedLocaleProcessor
from project import Project
from file import YAMLFile

def setup_logging(verbose=False):
    """Setup logging configuration"""
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format='%(asctime)s - %(levelname)s - %(message)s',
        datefmt='%H:%M:%S'
    )

def run_basic_processing():
    """Run basic YAML extraction and key finding (existing functionality)"""
    logging.info("=== Запуск базовой обработки ===")
    
    # YAML extraction
    logging.info("Поиск YAML файлов...")
    project = Project()
    yaml_files_paths = project.get_files_paths_by_dir(project.prototypes_dir_path, 'yml')
    
    if not yaml_files_paths:
        logging.info("YAML файлы не найдены!")
        return []
    
    logging.info(f"Найдено {len(yaml_files_paths)} YAML файлов. Обработка...")
    yaml_files = list(map(lambda yaml_file_path: YAMLFile(yaml_file_path), yaml_files_paths))
    
    # Extract YAML locales
    extractor = YAMLExtractor(yaml_files)
    extractor.execute()
    
    # Find and sync keys
    logging.info("Проверка актуальности файлов...")
    files_finder = FilesFinder(project)
    created_files = files_finder.execute()
    
    if created_files:
        logging.info("Форматирование созданных файлов...")
        FluentFormatter.format(created_files)
    
    logging.info("Проверка актуальности ключей...")
    key_finder = KeyFinder(files_finder.get_files_pars())
    changed_files = key_finder.execute()
    
    if changed_files:
        logging.info("Форматирование изменённых файлов...")
        FluentFormatter.format(changed_files)
    
    all_processed = created_files + changed_files
    logging.info(f"Базовая обработка завершена. Обработано файлов: {len(set(all_processed))}")
    return list(set(all_processed))

def run_advanced_processing():
    """Run advanced locale processing"""
    logging.info("=== Запуск расширенной обработки ===")
    
    processor = AdvancedLocaleProcessor()
    processed_files = processor.execute()
    
    logging.info(f"Расширенная обработка завершена. Обработано файлов: {len(processed_files)}")
    return processed_files

def run_final_formatting():
    """Run final formatting on all locale files"""
    logging.info("=== Финальное форматирование ===")
    
    project = Project()
    
    # Format all Russian files
    ru_files = project.get_fluent_files_by_dir(project.ru_locale_dir_path)
    if ru_files:
        logging.info(f"Форматирование {len(ru_files)} русских файлов...")
        FluentFormatter.format(ru_files)
    
    # Format all English files
    en_files = project.get_fluent_files_by_dir(project.en_locale_dir_path)
    if en_files:
        logging.info(f"Форматирование {len(en_files)} английских файлов...")
        FluentFormatter.format(en_files)
    
    total_files = len(ru_files) + len(en_files)
    logging.info(f"Финальное форматирование завершено. Обработано файлов: {total_files}")

def main():
    """Main entry point"""
    parser = argparse.ArgumentParser(description="Enhanced localization processor for Sunrise Station 14")
    parser.add_argument('--verbose', '-v', action='store_true', help='Enable verbose logging')
    parser.add_argument('--basic-only', action='store_true', help='Run only basic processing (YAML extraction + key finding)')
    parser.add_argument('--advanced-only', action='store_true', help='Run only advanced processing (cleanup + sync)')
    parser.add_argument('--format-only', action='store_true', help='Run only final formatting')
    parser.add_argument('--no-format', action='store_true', help='Skip final formatting step')
    
    args = parser.parse_args()
    
    setup_logging(args.verbose)
    
    try:
        if args.format_only:
            run_final_formatting()
        elif args.basic_only:
            run_basic_processing()
        elif args.advanced_only:
            run_advanced_processing()
        else:
            # Run full processing pipeline
            logging.info("🚀 Запуск полной обработки локализации...")
            
            # Step 1: Basic processing
            basic_files = run_basic_processing()
            
            # Step 2: Advanced processing
            advanced_files = run_advanced_processing()
            
            # Step 3: Final formatting (unless disabled)
            if not args.no_format:
                run_final_formatting()
            
            # Summary
            total_processed = len(set(basic_files + advanced_files))
            logging.info(f"🎉 Полная обработка завершена! Всего обработано файлов: {total_processed}")
            
    except KeyboardInterrupt:
        logging.warning("Обработка прервана пользователем")
    except Exception as e:
        logging.error(f"Ошибка во время обработки: {e}")
        if args.verbose:
            import traceback
            traceback.print_exc()
        return 1
    
    return 0

if __name__ == "__main__":
    exit(main())