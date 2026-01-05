using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace WebSW
{
    public static class RegistryHelper
    {
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
        /// </summary>
        /// <param name="version">SolidWorks version (e.g., "2025", "2024")</param>
        /// <returns>The ProgID suffix number</returns>
        public static string GetProgIdSuffix(string version)
        {
            switch (version)
            {
                case "2018": return "26";
                case "2019": return "27";
                case "2020": return "28";
                case "2021": return "29";
                case "2022": return "30";
                case "2023": return "31";
                case "2024": return "32";
                case "2025": return "33";
                default: return "31"; // Default to 2023
            }
        }
    }
}

