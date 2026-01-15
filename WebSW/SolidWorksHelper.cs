using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using NLog;
using MyApp.Logging;

namespace WebSW
{
    public static class SolidWorksHelper
    {
        private static readonly Logger logger;

        static SolidWorksHelper()
        {
            logger = LoggingService.ConfigureLogger(@"C:\wwwroot");
        }
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
                    logger.Info($"Connected to existing SolidWorks instance (Version {version}).");
                    swApp.Visible = true;
                    return swApp;
                }
                catch (COMException)
                {
                    logger.Info($"No running SolidWorks instance found for version {version}. Proceeding to launch.");
                }

                // Step 2: Launch SolidWorks using Activator.CreateInstance
                logger.Info($"Launching SolidWorks version {version} using ProgID: {progId}");

                try
                {
                    Type swType = Type.GetTypeFromProgID(progId);
                    
                    if (swType == null)
                    {
                        throw new COMException($"ProgID '{progId}' not found. SolidWorks version {version} may not be registered in COM.");
                    }

                    swApp = (ISldWorks)Activator.CreateInstance(swType);
                    swApp.Visible = true;

                    logger.Info($"SolidWorks started successfully (Version {version}).");
                    
                    // Optional: Wait a moment for full initialization
                    Thread.Sleep(2000);
                }
                catch (COMException comEx)
                {
                    logger.Error(comEx, $"COM Error launching SolidWorks: {comEx.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error creating SolidWorks instance: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error opening SolidWorks: {ex.Message}");
                logger.Debug($"Exception Type: {ex.GetType().Name}");
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
                    
                    logger.Info("SolidWorks closed and resources released.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Error closing SolidWorks: {ex.Message}");
                }
            }
        }
    }
}

