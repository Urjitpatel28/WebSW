using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace WebSW
{
    public static class SolidWorksHelper
    {
        /// <summary>
        /// Opens the SolidWorks application.
        /// </summary>
        /// <returns>The SolidWorks application object, or null if failed.</returns>
        public static ISldWorks OpenSolidWorks()
        {
            ISldWorks swApp = null;
            
            try
            {
                // Try to get an existing running instance of SolidWorks
                try
                {
                    // "SldWorks.Application.33" is for SolidWorks 2021
                    // For other versions, change the number:
                    // 30 = 2018, 31 = 2019, 32 = 2020, 33 = 2021, 34 = 2022, 35 = 2023, 36 = 2024
                    swApp = (ISldWorks)Marshal.GetActiveObject("SldWorks.Application.33");
                    Console.WriteLine("Connected to existing SolidWorks instance.");
                }
                catch (COMException)
                {
                    // If no existing instance found, create a new one
                    Console.WriteLine("No existing SolidWorks instance found. Creating new instance...");
                    swApp = new SldWorks();
                    
                    // Make SolidWorks visible
                    swApp.Visible = true;
                    
                    Console.WriteLine("New SolidWorks instance created.");
                }
                
                if (swApp != null)
                {
                    Console.WriteLine($"SolidWorks started successfully!");
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

