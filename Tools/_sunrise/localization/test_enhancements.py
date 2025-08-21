#!/usr/bin/env python3

"""
Test script for the enhanced localization functionality
"""

import os
import tempfile
import shutil
from file import FluentFile
from fluentformatter import FluentFormatter
from advanced_processor import AdvancedLocaleProcessor

def test_file_ending():
    """Test that files end with exactly one newline"""
    print("Testing file ending functionality...")
    
    test_cases = [
        "test-key = test value",  # No newline
        "test-key = test value\n",  # One newline
        "test-key = test value\n\n",  # Multiple newlines
        "test-key = test value\n\n\n",  # Many newlines
    ]
    
    for i, content in enumerate(test_cases):
        formatted = FluentFormatter.format_serialized_file_data(content)
        expected = "test-key = test value\n"
        assert formatted == expected, f"Case {i}: Expected {repr(expected)}, got {repr(formatted)}"
    
    print("✓ File ending tests passed")

def test_empty_file_detection():
    """Test empty file detection"""
    print("Testing empty file detection...")
    
    test_cases = [
        ("", True),  # Empty file
        ("   \n\n  ", True),  # Only whitespace
        ("# Just a comment", True),  # Only comments (if parser treats as empty)
        ("test-key = value", False),  # Has actual content
    ]
    
    for content, should_be_empty in test_cases:
        is_empty = FluentFormatter.is_file_empty(content)
        assert is_empty == should_be_empty, f"Content {repr(content)}: Expected {should_be_empty}, got {is_empty}"
    
    print("✓ Empty file detection tests passed")

def test_tag_escaping():
    """Test tag escaping with zero-width space"""
    print("Testing tag escaping...")
    
    test_cases = [
        ("  [bold]text", "  \u200B[bold]text"),  # Should escape
        ("  text", "  text"),  # Should not escape
        ("[color=red]text", "\u200B[color=red]text"),  # Should escape
        ("normal text", "normal text"),  # Should not escape
    ]
    
    for input_line, expected in test_cases:
        result = FluentFormatter.escape_multiline_tags(input_line)
        assert result == expected, f"Input {repr(input_line)}: Expected {repr(expected)}, got {repr(result)}"
    
    print("✓ Tag escaping tests passed")

def test_language_detection():
    """Test language detection functionality"""
    print("Testing language detection...")
    
    processor = AdvancedLocaleProcessor()
    
    test_cases = [
        ("Hello world", True, False),  # English
        ("Привет мир", False, True),  # Russian
        ("Hello мир", True, True),  # Mixed
        ("123 [bold]test[/bold]", True, False),  # Markup with English text
        ("", False, False),  # Empty
    ]
    
    for text, should_have_en, should_have_ru in test_cases:
        has_en = processor.is_english(text)
        has_ru = processor.has_russian(text)
        
        assert has_en == should_have_en, f"Text {repr(text)}: Expected English={should_have_en}, got {has_en}"
        assert has_ru == should_have_ru, f"Text {repr(text)}: Expected Russian={should_have_ru}, got {has_ru}"
    
    print("✓ Language detection tests passed")

def run_all_tests():
    """Run all tests"""
    print("Running localization enhancement tests...\n")
    
    try:
        test_file_ending()
        test_empty_file_detection()
        test_tag_escaping()
        test_language_detection()
        
        print("\n✅ All tests passed successfully!")
        return True
        
    except Exception as e:
        print(f"\n❌ Test failed: {e}")
        return False

if __name__ == "__main__":
    run_all_tests()