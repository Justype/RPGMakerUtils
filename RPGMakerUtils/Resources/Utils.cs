using Newtonsoft.Json;
using RPGMakerUtils.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RPGMakerUtils.Resources
{
    internal class Utils
    {
        public static RPGMakerVersion GetGameVersion(string gamePath)
        {
            if (string.IsNullOrWhiteSpace(gamePath))
                return RPGMakerVersion.Unknown;
            if (Directory.Exists(Path.Combine(gamePath, "www", "data")))
                return RPGMakerVersion.MV;
            if (Directory.Exists(Path.Combine(gamePath, "data")))
                return RPGMakerVersion.MZ;
            return RPGMakerVersion.Unknown;
        }

        public static bool IsJsonDictionary(string json)
        {
            try
            {
                var doc = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (doc == null)
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> CreateZipFromFileAsync(string sourceFilePath, string zipPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(sourceFilePath) && File.Exists(zipPath))
                        return false;
                    using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(sourceFilePath, Path.GetFileName(sourceFilePath));
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    // Optionally log or handle error
                    Console.WriteLine("Zipping failed: " + ex.Message);
                    return false;
                }
            });
        }

        public static async Task<bool> CreateZipFromFolderAsync(string sourceFolderPath, string zipPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(sourceFolderPath) && File.Exists(zipPath))
                        return false;

                    ZipFile.CreateFromDirectory(sourceFolderPath, zipPath, CompressionLevel.Optimal, includeBaseDirectory: true);

                    return true;
                }
                catch (Exception ex)
                {
                    // Optionally log or handle error
                    Console.WriteLine("Zipping failed: " + ex.Message);
                    return false;
                }
            });
        }

        public static async Task<bool> ExtractEmbeddedZipAsync(string resourceName, string targetDirectory)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();

                    using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (resourceStream == null)
                        {
                            Console.WriteLine($"Error: Embedded resource '{resourceName}' not found.");
                            return false;
                        }

                        if (!Directory.Exists(targetDirectory))
                        {
                            Directory.CreateDirectory(targetDirectory);
                        }

                        using (ZipArchive archive = new ZipArchive(resourceStream, ZipArchiveMode.Read))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                string destinationPath = Path.Combine(targetDirectory, entry.FullName);

                                // Ensure directory structure exists
                                string directory = Path.GetDirectoryName(destinationPath);
                                if (!string.IsNullOrEmpty(directory))
                                    Directory.CreateDirectory(directory);

                                // Skip empty directory entries
                                if (string.IsNullOrEmpty(entry.Name))
                                    continue;

                                // Overwrite the file
                                entry.ExtractToFile(destinationPath, overwrite: true);
                            }
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Extraction failed: " + ex.Message);
                    return false;
                }
            });
        }

    }
}
