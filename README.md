# WebSW - SolidWorks Web Controller

A web application that allows you to open and control SolidWorks from your web browser.

## Features

- 🌐 **Web Interface**: Beautiful, modern web page with buttons to control SolidWorks
- 🔒 **Session-Based Locking**: Prevent multiple devices from controlling SolidWorks simultaneously
- 🔧 **Open SolidWorks**: Click a button to open SolidWorks programmatically
- ❌ **Close SolidWorks**: Close SolidWorks from the web interface
- 📊 **Status Check**: Check if SolidWorks is currently running
- 🔄 **Auto-Heartbeat**: Maintains control session with automatic heartbeat
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
   - Click **"Request Control"** to acquire exclusive access to SolidWorks
   - Once you have control, the SolidWorks operation buttons will be enabled
   - Click **"Open SolidWorks"** to open SolidWorks
   - Click **"Close SolidWorks"** to close it
   - Click **"Check Status"** to see if SolidWorks is running
   - Click **"Release Control"** when you're done to allow other devices to take control

## Session-Based Locking

The application implements a session-based locking mechanism to prevent multiple devices from controlling SolidWorks simultaneously:

### How It Works

1. **Request Control**: Before you can operate SolidWorks, you must request control by clicking the "Request Control" button
2. **Exclusive Access**: Only one device can have control at a time
3. **Automatic Heartbeat**: Once you have control, the application sends a heartbeat every 30 seconds to maintain the session
4. **Auto-Expiry**: If a device becomes inactive for 5 minutes, the lock is automatically released
5. **Release Control**: You can manually release control to allow other devices to take over
6. **Auto-Release on Close**: When you close the browser tab, the lock is automatically released

### Key Features

- **Session ID**: Each device/browser gets a unique session ID stored in a cookie
- **Lock Timeout**: Locks expire after 5 minutes of inactivity
- **Heartbeat**: Keeps the session alive every 30 seconds
- **Visual Feedback**: Clear indication of whether you have control or not
- **Conflict Prevention**: Cannot perform operations without acquiring the lock first

## API Endpoints

The application provides the following REST API endpoints:

### Session Management
- `POST /api/SolidWorks/AcquireLock` - Request control of SolidWorks
- `POST /api/SolidWorks/ReleaseLock` - Release control
- `POST /api/SolidWorks/Heartbeat` - Extend lock expiry (sent automatically)
- `GET /api/SolidWorks/LockStatus` - Check lock status

### SolidWorks Operations
- `GET /api/SolidWorks/AvailableVersions` - Get installed SolidWorks versions
- `POST /api/SolidWorks/Open` - Opens SolidWorks (requires lock)
- `POST /api/SolidWorks/Close` - Closes SolidWorks (requires lock)
- `GET /api/SolidWorks/Status` - Gets the current SolidWorks status

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
