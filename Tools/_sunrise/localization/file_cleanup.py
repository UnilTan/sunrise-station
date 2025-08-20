#!/usr/bin/env python3
"""
File cleanup utilities for localization files.
Handles empty file and directory removal, file formatting improvements.
"""

import os
import typing
import logging
from pathlib import Path


class FileCleanup:
    """Handles file and directory cleanup operations."""
    
    def __init__(self, base_path: str):
        self.base_path = Path(base_path)
        self.removed_files = []
        self.removed_dirs = []
    
    def remove_empty_files(self, directory: str, extensions: typing.List[str] = None) -> typing.List[str]:
        """Remove empty files with specified extensions."""
        if extensions is None:
            extensions = ['.ftl']
        
        removed_files = []
        dir_path = Path(directory)
        
        for ext in extensions:
            for file_path in dir_path.rglob(f"*{ext}"):
                if file_path.is_file() and file_path.stat().st_size == 0:
                    try:
                        file_path.unlink()
                        removed_files.append(str(file_path))
                        logging.info(f"Removed empty file: {file_path}")
                    except Exception as e:
                        logging.error(f"Failed to remove {file_path}: {e}")
        
        self.removed_files.extend(removed_files)
        return removed_files
    
    def remove_empty_directories(self, directory: str) -> typing.List[str]:
        """Remove empty directories recursively."""
        removed_dirs = []
        dir_path = Path(directory)
        
        # Walk bottom-up to remove empty directories
        for root, dirs, files in os.walk(str(dir_path), topdown=False):
            root_path = Path(root)
            try:
                # Skip if directory is not empty
                if any(root_path.iterdir()):
                    continue
                
                # Remove empty directory
                root_path.rmdir()
                removed_dirs.append(str(root_path))
                logging.info(f"Removed empty directory: {root_path}")
            except OSError:
                # Directory not empty or other error
                pass
            except Exception as e:
                logging.error(f"Failed to remove directory {root_path}: {e}")
        
        self.removed_dirs.extend(removed_dirs)
        return removed_dirs
    
    def ensure_single_trailing_newline(self, file_path: str) -> bool:
        """Ensure file ends with exactly one newline."""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            if not content:
                return False
            
            # Remove trailing whitespace and ensure single newline
            content = content.rstrip() + '\n'
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            
            return True
        except Exception as e:
            logging.error(f"Failed to process {file_path}: {e}")
            return False
    
    def trim_file_edges_only(self, file_path: str) -> bool:
        """Trim whitespace only at beginning and end of file, preserving internal formatting."""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            if not content:
                return False
            
            # Only strip leading/trailing whitespace from entire content
            # This preserves internal indentation and spacing between entries
            content = content.strip() + '\n'
            
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write(content)
            
            return True
        except Exception as e:
            logging.error(f"Failed to process {file_path}: {e}")
            return False
    
    def process_locale_files(self, locale_dir: str) -> dict:
        """Process all locale files in directory for formatting improvements."""
        results = {
            'processed': 0,
            'failed': 0,
            'removed_empty': [],
            'removed_dirs': []
        }
        
        locale_path = Path(locale_dir)
        
        # First pass: format existing files
        for file_path in locale_path.rglob("*.ftl"):
            if file_path.is_file():
                if file_path.stat().st_size == 0:
                    continue  # Will be handled by remove_empty_files
                
                success1 = self.trim_file_edges_only(str(file_path))
                success2 = self.ensure_single_trailing_newline(str(file_path))
                
                if success1 and success2:
                    results['processed'] += 1
                else:
                    results['failed'] += 1
        
        # Second pass: remove empty files and directories
        results['removed_empty'] = self.remove_empty_files(locale_dir)
        results['removed_dirs'] = self.remove_empty_directories(locale_dir)
        
        return results