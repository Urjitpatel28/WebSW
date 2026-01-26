# Session Locking - Quick Start Guide

## What Changed?

Your SolidWorks web application now has **exclusive device control**. Only one device can operate SolidWorks at a time.

## How to Use

### Step 1: Request Control
Before you can use SolidWorks, click the **"Request Control"** button.

- ✅ **Green Banner**: You have control - buttons are enabled
- ⚠️ **Yellow Banner**: Another device has control - buttons are disabled

### Step 2: Operate SolidWorks
Once you have control:
- Click **"Open SolidWorks"** to start SolidWorks
- Click **"Close SolidWorks"** to close it
- Click **"Check Status"** to see if it's running

### Step 3: Release Control
When you're done:
- Click **"Release Control"** to let others take over
- Or just close the browser - it auto-releases

## Visual Flow

```
Device A                     Server                      Device B
   |                           |                            |
   |--[Request Control]------->|                            |
   |<------[GRANTED]-----------|                            |
   |                           |                            |
   |                           |<----[Request Control]------|
   |                           |------[DENIED]------------->|
   |                           |                            |
   |--[Open SolidWorks]------->|                            |
   |<------[SUCCESS]-----------|                            |
   |                           |                            |
   |--[Heartbeat]------------->|                            |
   |--[Heartbeat]------------->|                            |
   |                           |                            |
   |--[Release Control]------->|                            |
   |<------[SUCCESS]-----------|                            |
   |                           |                            |
   |                           |<----[Request Control]------|
   |                           |------[GRANTED]------------>|
   |                           |                            |
```

## Key Features

### 🔒 Exclusive Access
- Only ONE device can control SolidWorks at a time
- Other devices must wait for control to be released

### ⏰ Auto-Timeout
- If you become inactive for 5 minutes, control is auto-released
- Prevents permanent locks from crashed browsers

### 💓 Heartbeat
- Your browser sends a "heartbeat" every 30 seconds
- Keeps your control active while you're using it

### 🔄 Auto-Release
- Closing the browser tab automatically releases control
- No manual cleanup needed

## Troubleshooting

### "Another device is controlling SolidWorks"
- Wait for the other user to finish
- Or wait 5 minutes for auto-timeout
- Contact the other user to release control

### "You must acquire the lock first"
- Click **"Request Control"** before using SolidWorks buttons
- If it fails, another device has control

### Lost Control While Working
- Your session timed out (5 min inactivity)
- Network connection interrupted
- Another device forced control (shouldn't happen normally)
- Solution: Click **"Request Control"** again

## Configuration

All timing settings can be adjusted in the code:

| Setting | Default | Location |
|---------|---------|----------|
| Lock Timeout | 5 minutes | `SolidWorksController.cs` line 23 |
| Heartbeat Interval | 30 seconds | `index.html` line 167 |
| Status Check Interval | 10 seconds | `index.html` line 391 |
| Cookie Expiry | 8 hours | `SolidWorksController.cs` line 41 |

## Technical Details

For developers who want to understand the implementation:

- See `SESSION_LOCKING_IMPLEMENTATION.md` for full technical documentation
- Session IDs stored in HttpOnly cookies
- Server-side validation prevents bypassing
- Thread-safe locking mechanism
- Automatic cleanup of expired sessions

## API Examples

### Acquire Lock
```javascript
fetch('/api/SolidWorks/AcquireLock', {
    method: 'POST',
    credentials: 'include'
})
```

### Release Lock
```javascript
fetch('/api/SolidWorks/ReleaseLock', {
    method: 'POST',
    credentials: 'include'
})
```

### Check Lock Status
```javascript
fetch('/api/SolidWorks/LockStatus', {
    method: 'GET',
    credentials: 'include'
})
```
