using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
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
        /// Creates a cube part in SolidWorks, saves it to a temp file, and returns the file path.
        /// </summary>
        /// <param name="swApp">Active SolidWorks application instance.</param>
        /// <param name="sideLength">Cube side length in metres (default 0.1 = 100 mm).</param>
        /// <returns>Absolute path to the saved .SLDPRT file.</returns>
        public static string CreateCube(ISldWorks swApp, double sideLength = 0.1)
        {
            if (swApp == null)
                throw new InvalidOperationException("SolidWorks is not open.");

            IModelDoc2 swDoc = null;

            try
            {
                // Get the default part template path
                string templatePath = swApp.GetUserPreferenceStringValue(
                    (int)swUserPreferenceStringValue_e.swDefaultTemplatePart);

                if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
                {
                    // Fall back to a common default location
                    templatePath = @"C:\ProgramData\SolidWorks\SOLIDWORKS 2023\templates\Part.prtdot";
                    logger.Warn($"Default part template not found via preferences; trying fallback: {templatePath}");
                }

                logger.Info($"Creating new part document using template: {templatePath}");
                swDoc = (IModelDoc2)swApp.NewDocument(templatePath, 0, 0, 0);

                if (swDoc == null)
                    throw new InvalidOperationException("Failed to create a new part document.");

                // Select the Front Plane for the sketch
                bool selected = swDoc.Extension.SelectByID2(
                    "Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);

                if (!selected)
                    throw new InvalidOperationException("Could not select Front Plane.");

                // Open a 2D sketch on the Front Plane
                swDoc.SketchManager.InsertSketch(true);

                // Draw a centred rectangle; coordinates in metres
                double half = sideLength / 2.0;
                swDoc.SketchManager.CreateCenterRectangle(0, 0, 0, half, half, 0);

                // Exit the sketch
                swDoc.SketchManager.InsertSketch(true);

                // Extrude the sketch to create the cube
                // FeatureExtrusion2 blind extrude in one direction by sideLength
                IFeature extrudeFeature = (IFeature)swDoc.FeatureManager.FeatureExtrusion2(
                    true,        // Sd   – single direction
                    false,       // Flip – do not flip direction
                    false,       // Dir  – normal to sketch plane
                    (int)swEndConditions_e.swEndCondBlind,  // T1 – blind
                    (int)swEndConditions_e.swEndCondBlind,  // T2 – blind (unused, single dir)
                    sideLength,  // D1 – extrusion depth
                    sideLength,  // D2 – unused (single dir)
                    false, false, false, false, // Dchk1, Dchk2, Ddir1, Ddir2
                    0.01745, 0.01745,           // Dang1, Dang2 – draft angles (unused)
                    false, false,               // OffsetReverse1, OffsetReverse2
                    false,                      // TranslateSurface
                    true,                       // Merge
                    true,                       // UseFeatScope
                    true,                       // UseAutoSelect
                    false,                      // AssemblyFeatureScope
                    (int)swStartConditions_e.swStartSketchPlane,
                    0,                          // StartOffset
                    false                       // FlipStartOffset
                );

                if (extrudeFeature == null)
                    throw new InvalidOperationException("Extrusion feature creation failed.");

                logger.Info("Cube extrusion created successfully.");

                // Save to a unique temp file
                string fileName = $"cube_{Guid.NewGuid():N}.SLDPRT";
                string filePath = Path.Combine(Path.GetTempPath(), fileName);

                int saveErrors = 0;
                int saveWarnings = 0;
                bool saved = swDoc.Extension.SaveAs(
                    filePath,
                    (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                    null,
                    ref saveErrors,
                    ref saveWarnings);

                if (!saved || saveErrors != 0)
                    throw new InvalidOperationException($"Failed to save cube file. Errors: {saveErrors}, Warnings: {saveWarnings}");

                logger.Info($"Cube saved to: {filePath}");

                // Close the document without prompting
                swApp.CloseDoc(swDoc.GetTitle());
                Marshal.ReleaseComObject(swDoc);
                swDoc = null;

                return filePath;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error creating cube: {ex.Message}");

                // Attempt cleanup on failure
                if (swDoc != null)
                {
                    try { swApp.CloseDoc(swDoc.GetTitle()); } catch { }
                    Marshal.ReleaseComObject(swDoc);
                }

                throw;
            }
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

