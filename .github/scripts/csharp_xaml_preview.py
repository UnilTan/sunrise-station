#!/usr/bin/env python3
"""
C# XAML Preview Generator integration script.
This script builds and runs the C# XAML preview tool, then uploads the images.
"""

import subprocess
import sys
import os
import json
import base64
import argparse
from pathlib import Path

def build_csharp_tool():
    """Build the C# XAML preview tool."""
    print("Building C# XAML preview tool...")
    
    # Build the preview tool
    result = subprocess.run([
        "dotnet", "build", 
        "Content.XamlPreview/Content.XamlPreview.csproj",
        "--configuration", "Release",
        "--no-restore"
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"Failed to build C# preview tool:")
        print(result.stderr)
        return False
    
    print("Successfully built C# XAML preview tool")
    return True

def generate_preview_with_csharp(xaml_file, output_file):
    """Generate a preview using the C# tool."""
    print(f"Generating preview for {xaml_file}...")
    
    # Run the C# preview generator
    result = subprocess.run([
        "dotnet", "run",
        "--project", "Content.XamlPreview/Content.XamlPreview.csproj",
        "--configuration", "Release",
        "--no-build",
        "--", xaml_file, output_file
    ], capture_output=True, text=True)
    
    if result.returncode != 0:
        print(f"Failed to generate preview for {xaml_file}:")
        print(result.stderr)
        return False
    
    print(f"Successfully generated preview: {output_file}")
    return True

def embed_image_as_data_url(image_path):
    """Convert image to base64 data URL for inline embedding."""
    try:
        with open(image_path, 'rb') as f:
            image_data = f.read()
        
        # Convert to base64
        base64_data = base64.b64encode(image_data).decode('utf-8')
        
        # Create data URL
        data_url = f"data:image/png;base64,{base64_data}"
        return data_url
    except Exception as e:
        print(f"Failed to embed image {image_path}: {e}")
        return None

def main():
    parser = argparse.ArgumentParser(description='Generate XAML previews using C# tool')
    parser.add_argument('--modified', default='', help='Modified XAML files (space-separated)')
    parser.add_argument('--added', default='', help='Added XAML files (space-separated)')
    parser.add_argument('--removed', default='', help='Removed XAML files (space-separated)')
    parser.add_argument('--output-dir', default='xaml-previews', help='Output directory for images')
    
    args = parser.parse_args()
    
    # Parse file lists
    modified_files = args.modified.split() if args.modified else []
    added_files = args.added.split() if args.added else []
    removed_files = args.removed.split() if args.removed else []
    
    # Filter for XAML files
    xaml_files = []
    for files_list in [modified_files, added_files]:
        for file in files_list:
            if file.endswith('.xaml') and os.path.exists(file):
                xaml_files.append(file)
    
    if not xaml_files:
        print("No XAML files to process")
        return 0
    
    # Build the C# tool
    if not build_csharp_tool():
        return 1
    
    # Create output directory
    os.makedirs(args.output_dir, exist_ok=True)
    
    # Generate previews
    image_urls = {}
    successful_previews = 0
    
    for xaml_file in xaml_files:
        # Generate output filename
        base_name = os.path.splitext(os.path.basename(xaml_file))[0]
        output_file = os.path.join(args.output_dir, f"{base_name}_preview.png")
        
        # Generate preview
        if generate_preview_with_csharp(xaml_file, output_file):
            # Convert to data URL for embedding
            data_url = embed_image_as_data_url(output_file)
            if data_url:
                image_urls[xaml_file] = data_url
                successful_previews += 1
            else:
                print(f"Failed to embed image for {xaml_file}")
        else:
            print(f"Failed to generate preview for {xaml_file}")
    
    # Output the image URLs as JSON for the workflow
    with open('image_urls.json', 'w') as f:
        json.dump(image_urls, f, indent=2)
    
    print(f"Generated {successful_previews} previews out of {len(xaml_files)} XAML files")
    
    # Generate preview content for GitHub comment
    if successful_previews > 0:
        content = generate_preview_content(modified_files, added_files, removed_files, image_urls)
        
        # Output for GitHub Actions
        print(f"PREVIEW_CONTENT<<EOF")
        print(content)
        print("EOF")
    
    return 0 if successful_previews > 0 else 1

def generate_preview_content(modified_files, added_files, removed_files, image_urls):
    """Generate the GitHub comment content with embedded images."""
    
    # Filter for XAML files
    modified_xaml = [f for f in modified_files if f.endswith('.xaml')]
    added_xaml = [f for f in added_files if f.endswith('.xaml')]
    removed_xaml = [f for f in removed_files if f.endswith('.xaml')]
    
    total_files = len(modified_xaml) + len(added_xaml) + len(removed_xaml)
    
    content = []
    content.append("# 🎨 XAML Preview Bot (C# Enhanced)")
    content.append("")
    content.append(f"Found **{total_files}** XAML file(s) changed: **{len(modified_xaml)} modified**, **{len(added_xaml)} added**, **{len(removed_xaml)} removed**")
    content.append("")
    content.append("### 🖼️ Live Visual Previews")
    content.append("This PR includes **actual game-rendered previews** showing exactly how the UI will look in Space Station 14.")
    content.append("")
    
    # Process each type of change
    for file_list, change_type, emoji in [
        (modified_xaml, "Modified", "📝"),
        (added_xaml, "Added", "➕"),
        (removed_xaml, "Removed", "🗑️")
    ]:
        if not file_list:
            continue
            
        for xaml_file in file_list:
            content.append(f"## {emoji} {change_type}: `{xaml_file}`")
            content.append("")
            
            if change_type != "Removed" and xaml_file in image_urls:
                # Add the rendered preview
                content.append("### 🖼️ Rendered Preview")
                content.append(f"![XAML Preview]({image_urls[xaml_file]})")
                content.append("")
                content.append("*Generated using actual SS14 client rendering for 100% accuracy*")
                content.append("")
            elif change_type == "Removed":
                content.append("*File was removed from the repository.*")
                content.append("")
            else:
                content.append("*Preview generation failed for this file.*")
                content.append("")
    
    content.append("---")
    content.append("*Generated by C# XAML Preview Tool using RobustToolbox rendering*")
    
    return "\n".join(content)

if __name__ == "__main__":
    sys.exit(main())