# WebSW - SolidWorks Web Controller

A web application that allows you to open and control SolidWorks from your web browser.

## Features

- 🌐 **Web Interface**: Beautiful, modern web page with buttons to control SolidWorks
- 🔧 **Open SolidWorks**: Click a button to open SolidWorks programmatically
- ❌ **Close SolidWorks**: Close SolidWorks from the web interface
- 📊 **Status Check**: Check if SolidWorks is currently running
- 📱 **Responsive Design**: Works on desktop and mobile devices

## Project Structure

```
WebSW/
├── WebSW.sln                    # Solution file
└── WebSW/
    ├── WebSW.csproj             # Project file
    ├── Global.asax               # Web application entry point
    ├── Global.asax.cs            # Application startup code
    ├── Web.config               # Web application configuration
    ├── index.html               # Main web page
    ├── Controllers/
    │   └── SolidWorksController.cs  # Web API controller
    ├── App_Start/
    │   └── WebApiConfig.cs      # Web API configuration
    ├── SolidWorksHelper.cs      # SolidWorks helper class
    └── Swxdll/
        └── 2023/
            └── redist/
                ├── SolidWorks.Interop.sldworks.dll
                └── SolidWorks.Interop.swconst.dll
```

## Requirements

- .NET Framework 4.7.2 or higher
- Visual Studio 2017 or later
- SolidWorks installed on the system
- IIS Express (included with Visual Studio)

## How to Use

1. **Open the Solution**
   - Open `WebSW.sln` in Visual Studio

2. **Restore NuGet Packages**
   - Right-click the solution → Restore NuGet Packages
   - Or build the solution (Visual Studio will restore packages automatically)

3. **Run the Application**
   - Press F5 or click the Run button
   - The web application will start on `http://localhost:64615/`
   - Your default browser will open automatically

4. **Use the Web Interface**
   - Click **"Open SolidWorks"** to open SolidWorks
   - Click **"Close SolidWorks"** to close it
   - Click **"Check Status"** to see if SolidWorks is running

## API Endpoints

The application provides the following REST API endpoints:

- `POST /api/SolidWorks/Open` - Opens SolidWorks
- `POST /api/SolidWorks/Close` - Closes SolidWorks
- `GET /api/SolidWorks/Status` - Gets the current status

## SolidWorks Version

The application is configured for SolidWorks 2021 (Application.33).

To change the version, edit `SolidWorksHelper.cs` line 25:
- 30 = SolidWorks 2018
- 31 = SolidWorks 2019
- 32 = SolidWorks 2020
- 33 = SolidWorks 2021
- 34 = SolidWorks 2022
- 35 = SolidWorks 2023
- 36 = SolidWorks 2024

## Notes

- Make sure SolidWorks is installed on your system
- The DLLs are included in the project and will be copied to the output directory
- The application requires SolidWorks to be properly licensed
- The web interface uses modern JavaScript (no jQuery required)
- The application runs on IIS Express by default

## Troubleshooting

- **"Failed to open SolidWorks"**: Ensure SolidWorks is installed and licensed
- **Port already in use**: Change the port in the project properties or Web.config
- **NuGet packages not found**: Restore NuGet packages from the solution context menu
