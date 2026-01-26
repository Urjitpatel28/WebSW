# Session-Based Locking Implementation

## Overview
This document describes the session-based locking mechanism implemented to prevent multiple devices from controlling SolidWorks simultaneously.

## Problem Solved
Previously, when multiple devices accessed the web application, they could all control SolidWorks at the same time, causing conflicts and unexpected behavior. The new implementation ensures only one device can control SolidWorks at any given time.

## Implementation Details

### Backend Changes (SolidWorksController.cs)

#### New Static Properties
- `_currentSessionId`: Stores the session ID of the device currently holding the lock
- `_lockExpiry`: DateTime when the current lock expires
- `_lockTimeout`: TimeSpan set to 5 minutes for automatic lock expiry

#### New Methods

1. **GetSessionId()**
   - Creates or retrieves a unique session ID for each device
   - Stores session ID in a cookie (`SWSessionId`) that expires in 8 hours
   - Uses `HttpOnly` flag for security

2. **TryAcquireSessionLock(string sessionId)**
   - Attempts to acquire the lock for a given session
   - Auto-expires old locks that have timed out
   - Returns `true` if lock acquired or already held by the session
   - Returns `false` if lock held by another session

3. **ReleaseSessionLock(string sessionId)**
   - Releases the lock if held by the given session
   - Logs the release operation

4. **HasSessionLock(string sessionId)**
   - Checks if a session currently has the lock
   - Performs auto-expiry check

#### New API Endpoints

1. **POST /api/SolidWorks/AcquireLock**
   - Requests control of SolidWorks
   - Returns success if lock acquired
   - Returns HTTP 409 (Conflict) if another device has control

2. **POST /api/SolidWorks/ReleaseLock**
   - Releases control voluntarily
   - Always returns success

3. **POST /api/SolidWorks/Heartbeat**
   - Extends lock expiry time
   - Called every 30 seconds by the client
   - Returns HTTP 403 (Forbidden) if session doesn't hold lock

4. **GET /api/SolidWorks/LockStatus**
   - Checks current lock status
   - Returns whether caller has lock and if system is locked

#### Modified Endpoints
- **Open** and **Close** now check for session lock before executing
- Return HTTP 403 if session doesn't have the lock

### Frontend Changes (index.html)

#### New Global Variables
- `hasLock`: Boolean tracking if current device has control
- `heartbeatInterval`: Stores the heartbeat interval ID

#### New UI Elements
- **Lock Status Banner**: Shows whether user has control
- **Request Control Button**: Acquires the lock
- **Release Control Button**: Releases the lock
- **Disabled State**: Control buttons are disabled until lock is acquired

#### New Functions

1. **checkLockStatus()**
   - Checks if current session has the lock
   - Updates UI accordingly
   - Called on page load and every 10 seconds

2. **acquireLock()**
   - Calls AcquireLock API endpoint
   - Updates UI on success/failure
   - Starts heartbeat on success

3. **releaseLock()**
   - Calls ReleaseLock API endpoint
   - Updates UI
   - Stops heartbeat

4. **startHeartbeat()**
   - Starts interval timer (30 seconds)
   - Calls Heartbeat API endpoint
   - Updates UI if lock is lost

5. **stopHeartbeat()**
   - Clears heartbeat interval

6. **updateLockUI(lockStatus)**
   - Updates all UI elements based on lock status
   - Shows/hides appropriate buttons
   - Enables/disables control buttons

#### Modified Functions
- **openSolidWorks()** and **closeSolidWorks()** now check for lock before executing
- All API calls include `credentials: 'include'` to send cookies

#### Event Listeners
- **beforeunload**: Releases lock when page is closed
- **visibilitychange**: (Optional) Can auto-release when tab is hidden
- **load**: Checks lock status on page load
- **interval**: Checks lock status every 10 seconds

## Security Features

1. **HttpOnly Cookies**: Session IDs stored in HttpOnly cookies to prevent XSS attacks
2. **Server-Side Validation**: All operations validate lock ownership on server
3. **Auto-Expiry**: Locks automatically expire after 5 minutes of inactivity
4. **Heartbeat Mechanism**: Ensures active sessions maintain control

## Configuration Parameters

### Configurable Timeouts (in SolidWorksController.cs)
```csharp
private static readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(5);
```

### Configurable Cookie Expiry (in GetSessionId())
```csharp
Expires = DateTime.Now.AddHours(8)
```

### Configurable Heartbeat Interval (in index.html)
```javascript
}, 30000); // Every 30 seconds
```

### Configurable Lock Status Check Interval (in index.html)
```javascript
setInterval(checkLockStatus, 10000); // Every 10 seconds
```

## User Experience Flow

1. User opens web page
2. UI shows "Another device may be in control" status
3. User clicks "Request Control" button
4. If available, lock is acquired and control buttons are enabled
5. User performs SolidWorks operations (Open, Close, Status)
6. Application sends heartbeat every 30 seconds to maintain lock
7. When done, user clicks "Release Control" or closes browser
8. Lock is released, allowing other devices to take control

## Edge Cases Handled

1. **Multiple Tabs Same Browser**: Each tab gets same session ID, shares control
2. **Browser Crash**: Lock auto-expires after 5 minutes
3. **Network Interruption**: Lock expires if heartbeat fails
4. **Simultaneous Requests**: Server-side locking prevents race conditions
5. **Cookie Deletion**: New session ID assigned, must re-acquire lock

## Testing Recommendations

1. Open page on Device A, acquire lock
2. Open page on Device B, verify cannot acquire lock
3. On Device A, click Release Control
4. On Device B, verify can now acquire lock
5. On Device A, close browser tab
6. On Device B, verify can acquire lock
7. Let lock expire (wait 5 minutes), verify auto-release

## Future Enhancements

1. **Admin Override**: Add admin endpoint to force-release locks
2. **Lock Queue**: Implement queue system for waiting devices
3. **User Identification**: Add username/device name to locks
4. **Lock History**: Log all lock acquisitions and releases
5. **Distributed Lock**: Use Redis for multi-server support
6. **WebSocket Notifications**: Real-time updates when lock becomes available
