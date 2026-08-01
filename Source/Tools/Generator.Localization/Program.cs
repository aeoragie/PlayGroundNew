using Generator.Localization;
using Microsoft.Extensions.Configuration;
using System.Text;

Console.Title = "Localization Code Generator";
Console.OutputEncoding = Encoding.UTF8;

try
{
    Console.WriteLine("🌐 Localization Code Generator");
    Console.WriteLine("==============================");
    Console.WriteLine();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .Build();

    var section = configuration.GetSection("Localization");
    string resourcesPath = section["ResourcesPath"]!;
    string baseCulture = section["BaseCulture"] ?? "ko";
    string outputPath = section["OutputPath"]!;
    string generatedNamespace = section["Namespace"]!;

    Console.WriteLine($"📂 Resources: {resourcesPath} (base culture: {baseCulture})");
    Console.WriteLine();

    var generator = new LocalizationGenerator();
    string code = generator.Generate(resourcesPath, baseCulture, generatedNamespace);

    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await File.WriteAllTextAsync(outputPath, code);

    Console.WriteLine($"✅ Generated typed accessors → {outputPath}");
    Console.WriteLine("🎉 Localization code generation completed successfully!");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("❌ Error occurred during localization code generation:");
    Console.WriteLine($"   {ex.Message}");

    if (args.Contains("--verbose"))
    {
        Console.WriteLine();
        Console.WriteLine("Stack Trace:");
        Console.WriteLine(ex.StackTrace);
    }

    return 1;
}
