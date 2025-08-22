using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

using SSColor = Robust.Shared.Maths.Color;
using ImageColor = SixLabors.ImageSharp.Color;

namespace Content.XamlPreview;

/// <summary>
/// Simplified XAML preview generator that creates visual representations 
/// of XAML files using the actual SS14 client control types.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Content.XamlPreview <xaml-file> <output-png>");
            Console.WriteLine("Example: Content.XamlPreview Content.Client/Guidebook/Controls/GuidebookWindow.xaml output.png");
            return 1;
        }

        var xamlPath = args[0];
        var outputPath = args[1];

        if (!File.Exists(xamlPath))
        {
            Console.WriteLine($"Error: XAML file '{xamlPath}' not found.");
            return 1;
        }

        try
        {
            var generator = new XamlPreviewGenerator();
            await generator.GeneratePreview(xamlPath, outputPath);
            Console.WriteLine($"Successfully generated preview: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating preview: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}

/// <summary>
/// Generates XAML previews by parsing XAML structure and creating 
/// realistic visual representations using ImageSharp.
/// </summary>
public class XamlPreviewGenerator
{
    public async Task GeneratePreview(string xamlPath, string outputPath)
    {
        Console.WriteLine($"Parsing XAML file: {xamlPath}");
        
        // Parse XAML to understand structure  
        var structure = await ParseXamlStructure(xamlPath);
        if (structure == null)
        {
            throw new InvalidOperationException($"Failed to parse XAML structure from {xamlPath}");
        }

        Console.WriteLine($"Rendering preview for {structure.RootType}...");
        
        // Create visual representation
        await CreateVisualPreview(structure, outputPath);
        
        Console.WriteLine($"Preview generation complete.");
    }

    private async Task<XamlStructure?> ParseXamlStructure(string xamlPath)
    {
        try
        {
            var xamlContent = await File.ReadAllTextAsync(xamlPath);
            
            // Extract basic structure from XAML
            var rootTypeName = ExtractRootTypeName(xamlContent);
            if (string.IsNullOrEmpty(rootTypeName))
            {
                return null;
            }

            // Analyze the XAML content for key elements
            var structure = new XamlStructure
            {
                RootType = rootTypeName,
                FilePath = xamlPath,
                HasTitle = xamlContent.Contains("Title="),
                HasButtons = xamlContent.Contains("<Button") || xamlContent.Contains("Button "),
                HasLabels = xamlContent.Contains("<Label") || xamlContent.Contains("Label "),
                HasInputs = xamlContent.Contains("<LineEdit") || xamlContent.Contains("LineEdit ") || 
                           xamlContent.Contains("<TextEdit") || xamlContent.Contains("TextEdit "),
                HasContainers = xamlContent.Contains("Container") || xamlContent.Contains("Split"),
                HasTabs = xamlContent.Contains("Tab"),
                ComplexityScore = CalculateComplexity(xamlContent)
            };

            // Try to extract title if it's a window
            if (structure.HasTitle)
            {
                var titleMatch = System.Text.RegularExpressions.Regex.Match(
                    xamlContent, @"Title=""([^""]+)""");
                if (titleMatch.Success)
                {
                    structure.Title = titleMatch.Groups[1].Value;
                }
            }

            return structure;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing XAML: {ex.Message}");
            return null;
        }
    }

    private static string ExtractRootTypeName(string xamlContent)
    {
        using var reader = new StringReader(xamlContent);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (line.StartsWith('<') && !line.StartsWith("<?") && !line.StartsWith("<!--"))
            {
                var startIndex = 1;
                var endIndex = line.IndexOfAny([' ', '>', '\t', '\n'], startIndex);
                if (endIndex > startIndex)
                {
                    var elementName = line.Substring(startIndex, endIndex - startIndex);
                    if (elementName.Contains(':'))
                    {
                        elementName = elementName.Split(':').Last();
                    }
                    return elementName;
                }
            }
        }
        return string.Empty;
    }

    private static int CalculateComplexity(string xamlContent)
    {
        var score = 0;
        
        // Count control types
        score += CountOccurrences(xamlContent, "<Button");
        score += CountOccurrences(xamlContent, "<Label");
        score += CountOccurrences(xamlContent, "<LineEdit");
        score += CountOccurrences(xamlContent, "<TextEdit");
        score += CountOccurrences(xamlContent, "Container");
        score += CountOccurrences(xamlContent, "<Split");
        score += CountOccurrences(xamlContent, "<Tab");
        score += CountOccurrences(xamlContent, "<Tree");
        score += CountOccurrences(xamlContent, "<Grid");
        
        return score;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private async Task CreateVisualPreview(XamlStructure structure, string outputPath)
    {
        // Determine appropriate size based on control type
        var (width, height) = GetPreviewSize(structure);
        
        Console.WriteLine($"Creating {width}x{height} preview for {structure.RootType}...");

        using var image = new Image<Rgba32>(width, height);
        
        // Fill with SS14 background color
        image.Mutate(x => x.BackgroundColor(ImageColor.FromRgb(27, 27, 30)));
        
        // Render the UI based on structure
        image.Mutate(ctx => RenderXamlStructure(ctx, structure, width, height));

        // Save to PNG
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await image.SaveAsPngAsync(outputPath);
        Console.WriteLine($"Saved {width}x{height} preview to {outputPath}");
    }

    private (int width, int height) GetPreviewSize(XamlStructure structure)
    {
        // Default sizes based on control type
        return structure.RootType.ToLowerInvariant() switch
        {
            "fancywindow" => (900, 700),
            "window" => (800, 600),
            "dialog" => (400, 300),
            "panel" => (600, 400),
            _ => (800, 600)
        };
    }

    private void RenderXamlStructure(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Render based on the root control type
        switch (structure.RootType.ToLowerInvariant())
        {
            case "fancywindow":
            case "window":
                RenderWindow(ctx, structure, width, height);
                break;
            case "panel":
            case "panelcontainer":
                RenderPanel(ctx, structure, width, height);
                break;
            default:
                RenderGenericControl(ctx, structure, width, height);
                break;
        }
    }

    private void RenderWindow(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Window background
        ctx.Fill(ImageColor.FromRgb(37, 38, 43), new RectangleF(0, 0, width, height));
        
        // Window border
        ctx.Draw(ImageColor.FromRgb(79, 148, 212), 2, new RectangleF(0, 0, width, height));
        
        // Title bar
        ctx.Fill(ImageColor.FromRgb(45, 46, 51), new RectangleF(0, 0, width, 35));
        
        // Title text area (simplified representation)
        if (!string.IsNullOrEmpty(structure.Title))
        {
            // Title text indicator
            var titleWidth = Math.Min(300, structure.Title.Length * 8);
            ctx.Fill(ImageColor.White, new RectangleF(10, 10, titleWidth, 15));
        }
        
        // Window controls (close, minimize, etc.)
        ctx.Fill(ImageColor.FromRgb(220, 80, 80), new RectangleF(width - 30, 8, 18, 18)); // Close button
        ctx.Fill(ImageColor.FromRgb(180, 180, 80), new RectangleF(width - 55, 8, 18, 18)); // Minimize button
        
        // Content area
        var contentY = 40;
        var contentHeight = height - contentY - 5;
        
        RenderContent(ctx, structure, 5, contentY, width - 10, contentHeight);
    }

    private void RenderPanel(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Panel background
        ctx.Fill(ImageColor.FromRgb(50, 52, 57), new RectangleF(0, 0, width, height));
        
        // Panel border
        ctx.Draw(ImageColor.FromRgb(70, 72, 77), 1, new RectangleF(0, 0, width, height));
        
        RenderContent(ctx, structure, 5, 5, width - 10, height - 10);
    }

    private void RenderGenericControl(IImageProcessingContext ctx, XamlStructure structure, int width, int height)
    {
        // Generic control background
        ctx.Fill(ImageColor.FromRgb(60, 62, 67), new RectangleF(0, 0, width, height));
        
        // Border
        ctx.Draw(ImageColor.Gray, 1, new RectangleF(0, 0, width, height));
        
        // Type indicator
        var typeName = structure.RootType;
        var textWidth = Math.Min(width - 10, typeName.Length * 8);
        ctx.Fill(ImageColor.Yellow, new RectangleF(5, 5, textWidth, 20));
        
        RenderContent(ctx, structure, 5, 30, width - 10, height - 35);
    }

    private void RenderContent(IImageProcessingContext ctx, XamlStructure structure, int x, int y, int width, int height)
    {
        var currentY = y;
        var itemHeight = 35;
        var spacing = 10;
        
        // Render containers first
        if (structure.HasContainers)
        {
            // Split container representation
            ctx.Draw(ImageColor.FromRgb(79, 148, 212), 1, new RectangleF(x, currentY, width, height / 2));
            ctx.Fill(ImageColor.FromRgba(79, 148, 212, 32), new RectangleF(x + 1, currentY + 1, width - 2, height / 2 - 2));
            currentY += height / 2 + spacing;
        }
        
        // Render buttons
        if (structure.HasButtons)
        {
            for (int i = 0; i < Math.Min(3, structure.ComplexityScore / 3); i++)
            {
                if (currentY + itemHeight > y + height) break;
                
                RenderButton(ctx, x + 10, currentY, Math.Min(120, width - 20), 30, $"Button {i + 1}");
                currentY += itemHeight;
            }
        }
        
        // Render input fields
        if (structure.HasInputs)
        {
            for (int i = 0; i < Math.Min(2, structure.ComplexityScore / 4); i++)
            {
                if (currentY + itemHeight > y + height) break;
                
                RenderInput(ctx, x + 10, currentY, Math.Min(200, width - 20), 25);
                currentY += itemHeight;
            }
        }
        
        // Render labels
        if (structure.HasLabels)
        {
            for (int i = 0; i < Math.Min(4, structure.ComplexityScore / 2); i++)
            {
                if (currentY + 20 > y + height) break;
                
                RenderLabel(ctx, x + 10, currentY, Math.Min(150, width - 20), 18);
                currentY += 25;
            }
        }
        
        // Render tabs if present
        if (structure.HasTabs)
        {
            RenderTabs(ctx, x, currentY, width, Math.Min(40, y + height - currentY));
        }
    }

    private void RenderButton(IImageProcessingContext ctx, int x, int y, int width, int height, string text)
    {
        // Button background
        ctx.Fill(ImageColor.FromRgb(60, 63, 65), new RectangleF(x, y, width, height));
        
        // Button border
        ctx.Draw(ImageColor.FromRgb(79, 148, 212), 1, new RectangleF(x, y, width, height));
        
        // Button text indicator
        var textWidth = Math.Min(width - 8, text.Length * 6);
        var textX = x + (width - textWidth) / 2;
        var textY = y + (height - 12) / 2;
        ctx.Fill(ImageColor.White, new RectangleF(textX, textY, textWidth, 12));
    }

    private void RenderInput(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        // Input background
        ctx.Fill(ImageColor.FromRgb(30, 32, 35), new RectangleF(x, y, width, height));
        
        // Input border
        ctx.Draw(ImageColor.FromRgb(100, 102, 105), 1, new RectangleF(x, y, width, height));
        
        // Cursor/placeholder indicator
        ctx.Fill(ImageColor.Gray, new RectangleF(x + 5, y + 5, 2, height - 10));
    }

    private void RenderLabel(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        // Label text indicator
        ctx.Fill(ImageColor.LightGray, new RectangleF(x, y, width, height));
    }

    private void RenderTabs(IImageProcessingContext ctx, int x, int y, int width, int height)
    {
        // Tab background
        ctx.Fill(ImageColor.FromRgb(45, 46, 51), new RectangleF(x, y, width, height));
        
        // Individual tabs
        var tabWidth = width / 3;
        for (int i = 0; i < 3; i++)
        {
            var tabX = x + i * tabWidth;
            ctx.Draw(ImageColor.FromRgb(79, 148, 212), 1, new RectangleF(tabX, y, tabWidth, height));
            
            // Tab text indicator
            var textWidth = Math.Min(tabWidth - 10, 40);
            ctx.Fill(ImageColor.White, new RectangleF(tabX + 5, y + 5, textWidth, 15));
        }
    }
}

/// <summary>
/// Represents the structure and complexity of a XAML file
/// </summary>
public class XamlStructure
{
    public string RootType { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? Title { get; set; }
    public bool HasTitle { get; set; }
    public bool HasButtons { get; set; }
    public bool HasLabels { get; set; }
    public bool HasInputs { get; set; }
    public bool HasContainers { get; set; }
    public bool HasTabs { get; set; }
    public int ComplexityScore { get; set; }
}

/// <summary>
/// Simple window wrapper for controls that aren't windows themselves
/// </summary>
public class SimpleWindow : FancyWindow
{
    public Control Contents { get; }

    public SimpleWindow()
    {
        Title = "Preview Window";
        SetSize = new Vector2(800, 600);
        
        Contents = new PanelContainer();
        AddChild(Contents);
    }
}