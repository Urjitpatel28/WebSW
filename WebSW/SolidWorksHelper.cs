using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;

namespace WebSW
{
    public static class SolidWorksHelper
    {
        /// <summary>
        /// Opens the SolidWorks application.
        /// </summary>
        /// <param name="version">SolidWorks version (e.g., "2025", "2024"). If null, uses default version.</param>
        /// <returns>The SolidWorks application object, or null if failed.</returns>
        public static ISldWorks OpenSolidWorks(string version = null)
        {
            ISldWorks swApp = null;
            
            try
            {
                // If version is provided, use it; otherwise try to find any installed version
                if (string.IsNullOrEmpty(version))
                {
                    var availableVersions = RegistryHelper.GetAvailableVersions();
                    if (availableVersions.Count > 0)
                    {
                        version = availableVersions[0]; // Use the first available version
                    }
                    else
                    {
                        version = "2023"; // Default fallback
                    }
                }

                string progIdSuffix = RegistryHelper.GetProgIdSuffix(version);
                string progId = $"SldWorks.Application.{progIdSuffix}";

                // Step 1: Try to attach to existing instance
                try
                {
                    swApp = (ISldWorks)Marshal.GetActiveObject(progId);
                    Console.WriteLine($"Connected to existing SolidWorks instance (Version {version}).");
                    swApp.Visible = true;
                    return swApp;
                }
                catch (COMException)
                {
                    Console.WriteLine($"No running SolidWorks instance found for version {version}. Proceeding to launch.");
                }

                // Step 2: Launch SolidWorks via EXE
                string solidWorksPath = RegistryHelper.GetSolidWorksPath(version);

                if (string.IsNullOrEmpty(solidWorksPath))
                {
                    Console.WriteLine($"SolidWorks installation path not found for version {version}.");
                    return null;
                }

                Console.WriteLine($"Starting SolidWorks from: {solidWorksPath}");
                Process.Start(solidWorksPath);

                // Step 3: Wait for registration and try to attach via GetActiveObject
                int retryCount = 0;
                while (swApp == null && retryCount < 12)
                {
                    Thread.Sleep(5000); // Wait 5 seconds
                    try
                    {
                        swApp = (ISldWorks)Marshal.GetActiveObject(progId);
                    }
                    catch (COMException)
                    {
                        Console.WriteLine($"Waiting for COM registration... Retry {retryCount + 1}");
                    }
                    retryCount++;
                }

                // Step 4: Return result
                if (swApp != null)
                {
                    swApp.Visible = true;
                    Console.WriteLine($"SolidWorks started and registered successfully (Version {version}).");
                }
                else
                {
                    Console.WriteLine($"Failed to connect to SolidWorks after multiple retries (Version {version}).");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening SolidWorks: {ex.Message}");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                return null;
            }
            
            return swApp;
        }
        
        /// <summary>
        /// Closes the SolidWorks application.
        /// </summary>
        /// <param name="swApp">The SolidWorks application object.</param>
        public static void CloseSolidWorks(ISldWorks swApp)
        {
            if (swApp != null)
            {
                try
                {
                    swApp.ExitApp();
                    Marshal.ReleaseComObject(swApp);
                    swApp = null;
                    
                    // Force garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    
                    Console.WriteLine("SolidWorks closed and resources released.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing SolidWorks: {ex.Message}");
                }
            }
        }
    }
}

