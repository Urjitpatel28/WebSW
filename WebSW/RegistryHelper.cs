using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using NLog;
using MyApp.Logging;

namespace WebSW
{
    public static class RegistryHelper
    {
        private static readonly Logger logger;

        static RegistryHelper()
        {
            logger = LoggingService.ConfigureLogger(@"C:\wwwroot");
        }
        /// <summary>
        /// Gets all available SolidWorks versions from the registry.
        /// </summary>
        /// <returns>List of available SolidWorks versions (e.g., "2025", "2024", etc.)</returns>
        public static List<string> GetAvailableVersions()
        {
            var versions = new List<string>();
            string[] versionNumbers = { "2025", "2024", "2023", "2022", "2021", "2020", "2019", "2018" };
            string[] valueNames = { "SolidWorks Folder", "InstallDir" };

            // Check both 64-bit and 32-bit registry views
            foreach (RegistryView regView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView))
                {
                    foreach (string version in versionNumbers)
                    {
                        string registryKey = $"SOFTWARE\\SolidWorks\\SOLIDWORKS {version}\\Setup";
                        using (RegistryKey key = baseKey.OpenSubKey(registryKey))
                        {
                            if (key == null) continue;

                            foreach (string valueName in valueNames)
                            {
                                object pathValue = key.GetValue(valueName);
                                if (pathValue == null) continue;

                                string exePath = Path.Combine(pathValue.ToString(), "SLDWORKS.exe");
                                if (File.Exists(exePath))
                                {
                                    if (!versions.Contains(version))
                                    {
                                        versions.Add(version);
                                    }
                                    break; // Found valid installation for this version
                                }
                            }
                        }
                    }
                }
            }

            // Fallback to CurrentUser (optional)
            foreach (RegistryView regView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, regView))
                {
                    foreach (string version in versionNumbers)
                    {
                        if (versions.Contains(version)) continue; // Already found

                        string registryKey = $"SOFTWARE\\SolidWorks\\SOLIDWORKS {version}\\Setup";
                        using (RegistryKey key = baseKey.OpenSubKey(registryKey))
                        {
                            if (key == null) continue;

                            foreach (string valueName in valueNames)
                            {
                                object pathValue = key.GetValue(valueName);
                                if (pathValue == null) continue;

                                string exePath = Path.Combine(pathValue.ToString(), "SLDWORKS.exe");
                                if (File.Exists(exePath))
                                {
                                    if (!versions.Contains(version))
                                    {
                                        versions.Add(version);
                                    }
                                    break; // Found valid installation for this version
                                }
                            }
                        }
                    }
                }
            }

            return versions;
        }

        /// <summary>
        /// Gets the SolidWorks installation path for a specific version.
        /// </summary>
        /// <param name="version">SolidWorks version (e.g., "2025", "2024")</param>
        /// <returns>The path to SLDWORKS.exe, or null if not found</returns>
        public static string GetSolidWorksPath(string version)
        {
            string[] valueNames = { "SolidWorks Folder", "InstallDir" };

            // Check both 64-bit and 32-bit registry views
            foreach (RegistryView regView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, regView))
                {
                    string registryKey = $"SOFTWARE\\SolidWorks\\SOLIDWORKS {version}\\Setup";
                    using (RegistryKey key = baseKey.OpenSubKey(registryKey))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in valueNames)
                            {
                                object pathValue = key.GetValue(valueName);
                                if (pathValue == null) continue;

                                string exePath = Path.Combine(pathValue.ToString(), "SLDWORKS.exe");
                                if (File.Exists(exePath))
                                {
                                    return exePath;
                                }
                            }
                        }
                    }
                }
            }

            // Fallback to CurrentUser
            foreach (RegistryView regView in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, regView))
                {
                    string registryKey = $"SOFTWARE\\SolidWorks\\SOLIDWORKS {version}\\Setup";
                    using (RegistryKey key = baseKey.OpenSubKey(registryKey))
                    {
                        if (key != null)
                        {
                            foreach (string valueName in valueNames)
                            {
                                object pathValue = key.GetValue(valueName);
                                if (pathValue == null) continue;

                                string exePath = Path.Combine(pathValue.ToString(), "SLDWORKS.exe");
                                if (File.Exists(exePath))
                                {
                                    return exePath;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the ProgID suffix for a SolidWorks version.
        /// Formula: ProgID Number = Year - 1992
        /// Note: Versions before 2006 may not have versioned ProgIDs
        /// </summary>
        /// <param name="version">SolidWorks version (e.g., "2025", "2024")</param>
        /// <returns>The ProgID suffix number</returns>
        /// <exception cref="ArgumentException">Thrown when version is not a valid year format</exception>
        public static string GetProgIdSuffix(string version)
        {
            try
            {
                if (!int.TryParse(version, out int year))
                {
                    throw new ArgumentException($"Invalid version format: '{version}'. Expected a valid year (e.g., '2024').");
                }
                
                // Formula: ProgID = Year - 1992
                // Example: SolidWorks 2024 = 2024 - 1992 = 32
                int progIdNumber = year - 1992;
                
                if (progIdNumber < 0)
                {
                    throw new ArgumentException($"Version year {year} is before 1992, which is invalid for SolidWorks.");
                }
                
                return progIdNumber.ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error calculating ProgID suffix: {ex.Message}");
                throw;
            }
        }
    }
}

