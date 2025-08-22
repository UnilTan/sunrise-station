#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Content.IntegrationTests;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace Content.XamlRenderer;

internal sealed class Program
{
    internal static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Content.XamlRenderer <xaml-file-paths...>");
            Console.WriteLine("Renders XAML files to PNG images for preview.");
            return;
        }

        var xamlFiles = new List<string>();
        var outputDir = "xaml-previews";
        
        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--output" && i + 1 < args.Length)
            {
                outputDir = args[i + 1];
                i++; // Skip next argument as it's the output directory
            }
            else if (File.Exists(args[i]) && args[i].EndsWith(".xaml"))
            {
                xamlFiles.Add(args[i]);
            }
        }

        if (xamlFiles.Count == 0)
        {
            Console.WriteLine("No valid XAML files provided.");
            return;
        }

        Directory.CreateDirectory(outputDir);
        
        Console.WriteLine($"Rendering {xamlFiles.Count} XAML file(s) to {outputDir}/");

        var testContext = new ExternalTestContext("Content.XamlRenderer", Console.Out);
        PoolManager.Startup();

        try
        {
            await using var pair = await PoolManager.GetServerClient(testContext: testContext);
            var renderer = new XamlRenderer(pair);
            
            foreach (var xamlFile in xamlFiles)
            {
                try
                {
                    Console.WriteLine($"Rendering {xamlFile}...");
                    var image = await renderer.RenderXamlFile(xamlFile);
                    
                    if (image != null)
                    {
                        var outputFileName = Path.GetFileNameWithoutExtension(xamlFile) + ".png";
                        var outputPath = Path.Combine(outputDir, outputFileName);
                        
                        await image.SaveAsPngAsync(outputPath);
                        Console.WriteLine($"Saved preview to {outputPath}");
                        
                        image.Dispose();
                    }
                    else
                    {
                        Console.WriteLine($"Failed to render {xamlFile}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error rendering {xamlFile}: {ex.Message}");
                }
            }
        }
        finally
        {
            PoolManager.Shutdown();
        }
        
        Console.WriteLine("Rendering complete.");
    }
}