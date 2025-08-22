#!/usr/bin/env python3
"""
Upload images to GitHub for use in PR comments.
Uses GitHub's comment attachment API to upload images and get URLs.
"""

import os
import sys
import requests
import base64
import json
from typing import Optional, Dict

def upload_image_to_github(image_path: str, github_token: str, repo: str) -> Optional[str]:
    """
    Upload an image to GitHub and return a URL that can be used in comments.
    Uses GitHub's issue attachment API.
    """
    try:
        # Read the image file
        with open(image_path, 'rb') as f:
            image_data = f.read()
        
        # GitHub API endpoint for uploading assets
        # We'll use the user content API which allows for file uploads
        headers = {
            'Authorization': f'token {github_token}',
            'Accept': 'application/vnd.github.v3+json'
        }
        
        # For now, we'll use a simpler approach: base64 encode small images
        # or save to a temporary location and reference them
        
        # Check image size
        image_size = len(image_data)
        if image_size > 1024 * 1024:  # 1MB limit for inline
            print(f"Image {image_path} is too large ({image_size} bytes) for inline embedding")
            return None
        
        # For smaller images, we can use data URLs (base64)
        import mimetypes
        mime_type, _ = mimetypes.guess_type(image_path)
        if mime_type is None:
            mime_type = 'image/png'
        
        base64_data = base64.b64encode(image_data).decode('utf-8')
        data_url = f"data:{mime_type};base64,{base64_data}"
        
        return data_url
        
    except Exception as e:
        print(f"Error uploading image {image_path}: {e}")
        return None

def create_github_gist(files: Dict[str, str], github_token: str, description: str = "XAML Preview Images") -> Optional[str]:
    """
    Create a GitHub Gist with the provided files and return the Gist URL.
    """
    try:
        headers = {
            'Authorization': f'token {github_token}',
            'Accept': 'application/vnd.github.v3+json'
        }
        
        gist_data = {
            'description': description,
            'public': True,
            'files': {}
        }
        
        for filename, content in files.items():
            gist_data['files'][filename] = {'content': content}
        
        response = requests.post(
            'https://api.github.com/gists',
            headers=headers,
            json=gist_data
        )
        
        if response.status_code == 201:
            gist_info = response.json()
            return gist_info['html_url']
        else:
            print(f"Failed to create gist: {response.status_code} - {response.text}")
            return None
            
    except Exception as e:
        print(f"Error creating gist: {e}")
        return None

def upload_images_to_imgur(image_paths: list, client_id: str = None) -> Dict[str, str]:
    """
    Upload images to Imgur and return URLs.
    Note: This requires an Imgur client ID.
    """
    if not client_id:
        print("No Imgur client ID provided, skipping Imgur upload")
        return {}
    
    uploaded_urls = {}
    
    for image_path in image_paths:
        try:
            with open(image_path, 'rb') as f:
                image_data = f.read()
            
            headers = {'Authorization': f'Client-ID {client_id}'}
            
            response = requests.post(
                'https://api.imgur.com/3/image',
                headers=headers,
                files={'image': image_data}
            )
            
            if response.status_code == 200:
                data = response.json()
                if data['success']:
                    uploaded_urls[image_path] = data['data']['link']
                    print(f"Uploaded {image_path} to Imgur: {data['data']['link']}")
                else:
                    print(f"Failed to upload {image_path} to Imgur: {data}")
            else:
                print(f"Imgur API error for {image_path}: {response.status_code}")
                
        except Exception as e:
            print(f"Error uploading {image_path} to Imgur: {e}")
    
    return uploaded_urls

def main():
    if len(sys.argv) < 4:
        print("Usage: python3 upload_image_to_github.py <image_directory> <github_token> <repo>")
        return
    
    image_dir = sys.argv[1]
    github_token = sys.argv[2]
    repo = sys.argv[3]
    
    # Find all PNG files in the directory
    image_files = []
    if os.path.exists(image_dir):
        for filename in os.listdir(image_dir):
            if filename.endswith('.png'):
                image_files.append(os.path.join(image_dir, filename))
    
    if not image_files:
        print("No PNG files found in directory")
        return
    
    # Try different upload methods
    results = {}
    
    # Method 1: Try base64 encoding for small images
    for image_path in image_files:
        url = upload_image_to_github(image_path, github_token, repo)
        if url:
            results[image_path] = url
    
    # Output results as JSON for the workflow to consume
    print(json.dumps(results, indent=2))

if __name__ == '__main__':
    main()