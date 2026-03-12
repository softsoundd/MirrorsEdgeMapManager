using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MirrorsEdgeMapManager.Helpers;

public static class ConfigTemplateProvider
{
    public static bool TryGetTemplateContent(string templateFileName, out string content)
    {
        content = string.Empty;
        var normalisedName = Path.GetFileName(templateFileName);
        if (string.IsNullOrWhiteSpace(normalisedName))
            return false;

        var assembly = Assembly.GetExecutingAssembly();
        var resourceSuffix = $"ConfigTemplates.{normalisedName}";
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(resourceName))
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream, GetEncodingForTemplate(normalisedName), detectEncodingFromByteOrderMarks: true);
                content = reader.ReadToEnd();
                return true;
            }
        }

        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var outputPath = Path.Combine(appDirectory, "ConfigTemplates", normalisedName);
        if (File.Exists(outputPath))
        {
            content = File.ReadAllText(outputPath, GetEncodingForTemplate(normalisedName));
            return true;
        }

        var projectPath = Path.GetFullPath(Path.Combine(appDirectory, @"..\..\..\"));
        var projectTemplatePath = Path.Combine(projectPath, "ConfigTemplates", normalisedName);
        if (File.Exists(projectTemplatePath))
        {
            content = File.ReadAllText(projectTemplatePath, GetEncodingForTemplate(normalisedName));
            return true;
        }

        return false;
    }

    public static bool MergeTemplateIntoIniFile(string templateFileName, string targetPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!TryGetTemplateContent(templateFileName, out var templateContent))
        {
            errorMessage = $"{templateFileName} template is unavailable.";
            return false;
        }

        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"MEMM_{Path.GetFileNameWithoutExtension(templateFileName)}_{Guid.NewGuid():N}{Path.GetExtension(templateFileName)}");

        try
        {
            File.WriteAllText(tempFilePath, templateContent, GetEncodingForTemplate(templateFileName));
            IniFileHelper.MergeIniFiles(tempFilePath, targetPath);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch
                {
                }
            }
        }
    }

    public static bool WriteTemplateToFile(string templateFileName, string targetPath, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!TryGetTemplateContent(templateFileName, out var templateContent))
        {
            errorMessage = $"{templateFileName} template is unavailable.";
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(targetPath, templateContent, GetEncodingForTemplate(templateFileName));
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    private static Encoding GetEncodingForTemplate(string templateFileName)
    {
        return templateFileName.EndsWith(".int", StringComparison.OrdinalIgnoreCase)
            ? Encoding.GetEncoding(1252)
            : Encoding.UTF8;
    }
}
