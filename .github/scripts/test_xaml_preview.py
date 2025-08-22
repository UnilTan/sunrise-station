#!/usr/bin/env python3
"""
Test script for XAML preview functionality.
Creates a sample XAML file and tests the entire pipeline.
"""

import os
import sys
import tempfile
import shutil
import json

def create_test_xaml():
    """Create a test XAML file for testing."""
    xaml_content = """<ui:FancyWindow xmlns="https://spacestation14.io"
            xmlns:ui="clr-namespace:Content.Client.UserInterface.Controls"
            MinSize="400 300" 
            Resizable="True" 
            Title="Test Window">
    <BoxContainer Orientation="Vertical" Margin="10">
        <Label Text="Welcome to the Test UI" />
        <BoxContainer Orientation="Horizontal" HorizontalExpand="True">
            <Button Text="Test Button 1" HorizontalExpand="True" Margin="0 0 5 0"/>
            <Button Text="Test Button 2" HorizontalExpand="True" Margin="5 0 0 0"/>
        </BoxContainer>
        <SplitContainer Orientation="Horizontal" VerticalExpand="True">
            <BoxContainer Orientation="Vertical" Margin="5">
                <Label Text="Left Panel" />
                <LineEdit PlaceHolder="Enter text here..." />
                <CheckBox Text="Enable feature" />
            </BoxContainer>
            <TabContainer VerticalExpand="True">
                <PanelContainer Name="Tab1">
                    <ScrollContainer>
                        <RichTextLabel Text="This is tab content with scrollable text area." />
                    </ScrollContainer>
                </PanelContainer>
                <PanelContainer Name="Tab2">
                    <Label Text="Second tab content" />
                </PanelContainer>
            </TabContainer>
        </SplitContainer>
    </BoxContainer>
</ui:FancyWindow>"""
    
    with tempfile.NamedTemporaryFile(mode='w', suffix='.xaml', delete=False) as f:
        f.write(xaml_content)
        return f.name

def test_pipeline():
    """Test the complete XAML preview pipeline."""
    print("Testing XAML Preview Pipeline...")
    
    # Create test XAML file
    test_xaml = create_test_xaml()
    print(f"Created test XAML: {test_xaml}")
    
    try:
        # Create output directory
        output_dir = tempfile.mkdtemp()
        print(f"Output directory: {output_dir}")
        
        # Test mockup generation
        mockup_file = os.path.join(output_dir, "test_mockup.png")
        
        print("Testing mockup generator...")
        os.system(f'python3 ./.github/scripts/xaml_mockup_generator.py "{test_xaml}" "{mockup_file}"')
        
        if os.path.exists(mockup_file):
            print(f"✓ Mockup generated: {mockup_file}")
            file_size = os.path.getsize(mockup_file)
            print(f"  File size: {file_size} bytes")
        else:
            print("✗ Failed to generate mockup")
            return False
        
        # Test image upload
        print("Testing image upload...")
        result = os.popen(f'python3 ./.github/scripts/upload_images_for_comment.py "{output_dir}" "dummy_token" "test/repo" "123"').read()
        
        try:
            image_urls = json.loads(result)
            if image_urls and "test_mockup.png" in image_urls:
                print("✓ Image upload successful")
                print(f"  Data URL length: {len(image_urls['test_mockup.png'])}")
            else:
                print("✗ Image upload failed")
                return False
        except json.JSONDecodeError:
            print(f"✗ Failed to parse image upload result: {result}")
            return False
        
        # Save image URLs for preview test
        urls_file = os.path.join(output_dir, "image_urls.json")
        with open(urls_file, 'w') as f:
            json.dump(image_urls, f)
        
        # Test preview generation
        print("Testing preview generation...")
        result = os.popen(f'python3 ./.github/scripts/xaml-preview.py --modified "{test_xaml}" --image-urls "{urls_file}"').read()
        
        if "PREVIEW_CONTENT<<EOF" in result and "🖼️ Visual Previews Available" in result:
            print("✓ Preview generation successful")
            # Count lines for a rough size check
            lines = result.split('\n')
            print(f"  Generated {len(lines)} lines of preview content")
        else:
            print("✗ Preview generation failed")
            print(f"Result: {result[:200]}...")
            return False
        
        print("\n✅ All tests passed! The XAML preview pipeline is working correctly.")
        return True
        
    finally:
        # Cleanup
        if os.path.exists(test_xaml):
            os.unlink(test_xaml)
        if 'output_dir' in locals() and os.path.exists(output_dir):
            shutil.rmtree(output_dir)

if __name__ == '__main__':
    test_pipeline()