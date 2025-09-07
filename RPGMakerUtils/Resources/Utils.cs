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
            if (Directory.Exists(Path.Combine(gamePath, "www", "js")))
                return RPGMakerVersion.MV;
            if (Directory.Exists(Path.Combine(gamePath, "js")))
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

        /// <summary>
        /// Extracts a ZIP archive to a directory asynchronously.
        /// </summary>
        /// <param name="zipPath">The path to the ZIP file.</param>
        /// <param name="extractDirectory">
        /// The destination directory where the archive contents will be extracted. It 
        /// will be created if it does not exist.
        /// </param>
        /// <param name="overwrite">
        /// When <c>true</c>, existing files at the destination will be overwritten.
        /// When <c>false</c>, extraction will fail if a file already exists.
        /// </param>
        /// <param name="autoStrip">
        /// When <c>true</c>, and the ZIP archive contains exactly one top-level folder 
        /// with the same name as <paramref name="extractDirectory"/>, that folder is 
        /// stripped so that its contents are placed directly into the destination.
        /// </param>
        /// <returns><c>true</c> if extraction succeeds, otherwise <c>false</c>.</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="IOException"></exception>
        public static async Task<bool> ExtractZipAsync(string zipPath, string extractDirectory, bool overwrite = false)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
                throw new FileNotFoundException("Zip file not found.", zipPath);

            if (File.Exists(extractDirectory))
                throw new IOException("The extraction path points to an existing file.");

            try
            {
                await Task.Run(() =>
                {
                    using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                    {
                        // Ensure destination exists
                        Directory.CreateDirectory(extractDirectory);

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            string destinationPath = Path.Combine(extractDirectory, entry.FullName);

                            // Ensure directory exists
                            string directoryPath = Path.GetDirectoryName(destinationPath);
                            if (!string.IsNullOrEmpty(directoryPath))
                                Directory.CreateDirectory(directoryPath);

                            if (string.IsNullOrEmpty(entry.Name)) // It's a directory entry
                                continue;

                            if (File.Exists(destinationPath) && overwrite)
                                File.Delete(destinationPath);

                            // Extract, will throw if file exists and overwrite == false
                            entry.ExtractToFile(destinationPath, overwrite);
                        }
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Extraction failed: " + ex.Message);
                return false;
            }
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

        public static async Task<bool> CopyEmbeddedFileAsync(string resourceName, string targetFilePath)
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
                        string directory = Path.GetDirectoryName(targetFilePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        using (var fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                        {
                            resourceStream.CopyTo(fileStream);
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
