#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using Content.IntegrationTests;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;
using Robust.Shared.UserInterface;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Content.XamlRenderer;

public sealed class XamlRenderer
{
    private readonly PairTracker _pair;
    private IUserInterfaceManager? _uiManager;
    private IClydeTexture? _renderTexture;
    
    public XamlRenderer(PairTracker pair)
    {
        _pair = pair;
    }

    public async Task<Image<Rgba32>?> RenderXamlFile(string xamlFilePath)
    {
        if (!File.Exists(xamlFilePath))
        {
            Console.WriteLine($"XAML file not found: {xamlFilePath}");
            return null;
        }

        try
        {
            // Read XAML content
            var xamlContent = await File.ReadAllTextAsync(xamlFilePath);
            
            // Get UI manager from client
            _uiManager = _pair.Client.ResolveDependency<IUserInterfaceManager>();
            
            // Parse and create the control
            var control = _uiManager.LoadXaml(xamlContent);
            
            if (control == null)
            {
                Console.WriteLine($"Failed to parse XAML: {xamlFilePath}");
                return null;
            }

            // Set up a reasonable size for the preview
            var previewSize = new Vector2i(800, 600);
            
            // Try to get the size from the control if it has explicit sizing
            if (control is Control ctrl)
            {
                if (ctrl.Width > 0 && ctrl.Height > 0)
                {
                    previewSize = new Vector2i((int)ctrl.Width, (int)ctrl.Height);
                }
                else if (ctrl.MinWidth > 0 && ctrl.MinHeight > 0)
                {
                    previewSize = new Vector2i((int)ctrl.MinWidth, (int)ctrl.MinHeight);
                }
            }

            // Create a temporary root container
            var rootControl = new Control();
            rootControl.AddChild(control);
            rootControl.Size = previewSize;
            
            // Add to UI hierarchy temporarily
            _uiManager.RootControl.AddChild(rootControl);
            
            try
            {
                // Force layout update
                rootControl.Measure(previewSize);
                rootControl.Arrange(UIBox2.FromDimensions(Vector2.Zero, previewSize));
                
                // Render to image
                var image = await RenderControlToImage(rootControl, previewSize);
                
                return image;
            }
            finally
            {
                // Clean up
                _uiManager.RootControl.RemoveChild(rootControl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception rendering XAML {xamlFilePath}: {ex}");
            return null;
        }
    }

    private async Task<Image<Rgba32>> RenderControlToImage(Control control, Vector2i size)
    {
        // Create image buffer
        var image = new Image<Rgba32>(size.X, size.Y);
        
        // This is a simplified approach - in a real implementation,
        // we would need to properly render the UI control to a texture
        // and then copy that texture data to the image
        
        // For now, create a placeholder that shows the control structure
        var backgroundColor = Color4.FromHex("#2e3440");
        
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(new Rgba32(backgroundColor.R, backgroundColor.G, backgroundColor.B, backgroundColor.A));
            
            // Add some basic visual representation
            var textColor = new Rgba32(216, 222, 233, 255); // Light text color
            
            // This is a placeholder - real implementation would need proper UI rendering
            // For now, we'll create a simple preview showing it's a XAML file
        });

        return image;
    }
}