using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using SolidWorks.Interop.sldworks;
using NLog;
using MyApp.Logging;

namespace WebSW.Controllers
{
    public class SolidWorksController : ApiController
    {
        private static ISldWorks _swApp = null;
        private static readonly object _lockObject = new object();
        private static readonly Logger logger;

        // Session locking properties
        private static string _currentSessionId = null;
        private static DateTime _lockExpiry = DateTime.MinValue;
        private static readonly TimeSpan _lockTimeout = TimeSpan.FromMinutes(5);

        // Cube file registry: fileId -> absolute temp path on server
        private static readonly Dictionary<string, string> _cubeFiles = new Dictionary<string, string>();

        static SolidWorksController()
        {
            logger = LoggingService.ConfigureLogger(@"C:\wwwroot");
        }

        /// <summary>
        /// Gets or creates a unique session ID for the current client
        /// </summary>
        private string GetSessionId()
        {
            var cookie = HttpContext.Current.Request.Cookies["SWSessionId"];
            if (cookie == null || string.IsNullOrEmpty(cookie.Value))
            {
                string sessionId = Guid.NewGuid().ToString();
                var newCookie = new HttpCookie("SWSessionId", sessionId)
                {
                    Expires = DateTime.Now.AddHours(8),
                    HttpOnly = true
                };
                HttpContext.Current.Response.Cookies.Add(newCookie);
                logger.Info($"Created new session ID: {sessionId}");
                return sessionId;
            }
            return cookie.Value;
        }

        /// <summary>
        /// Attempts to acquire the session lock for the current client
        /// </summary>
        private bool TryAcquireSessionLock(string sessionId)
        {
            lock (_lockObject)
            {
                // Check if lock expired
                if (_currentSessionId != null && DateTime.Now > _lockExpiry)
                {
                    logger.Info($"Session lock expired for session: {_currentSessionId}");
                    _currentSessionId = null;
                }

                // Check if current session already has lock
                if (_currentSessionId == sessionId)
                {
                    _lockExpiry = DateTime.Now.Add(_lockTimeout);
                    return true;
                }

                // Check if lock is free
                if (_currentSessionId == null)
                {
                    _currentSessionId = sessionId;
                    _lockExpiry = DateTime.Now.Add(_lockTimeout);
                    logger.Info($"Session lock acquired by: {sessionId}");
                    return true;
                }

                // Lock held by another session
                logger.Warn($"Session {sessionId} attempted to acquire lock held by {_currentSessionId}");
                return false;
            }
        }

        /// <summary>
        /// Releases the session lock if held by the current session
        /// </summary>
        private void ReleaseSessionLock(string sessionId)
        {
            lock (_lockObject)
            {
                if (_currentSessionId == sessionId)
                {
                    logger.Info($"Session lock released by: {sessionId}");
                    _currentSessionId = null;
                }
            }
        }

        /// <summary>
        /// Checks if the current session has the lock
        /// </summary>
        private bool HasSessionLock(string sessionId)
        {
            lock (_lockObject)
            {
                // Auto-expire check
                if (_currentSessionId != null && DateTime.Now > _lockExpiry)
                {
                    _currentSessionId = null;
                }

                return _currentSessionId == sessionId;
            }
        }

        [HttpPost]
        [Route("api/SolidWorks/AcquireLock")]
        public HttpResponseMessage AcquireLock()
        {
            try
            {
                string sessionId = GetSessionId();

                if (TryAcquireSessionLock(sessionId))
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = true,
                        message = "Lock acquired successfully",
                        sessionId = sessionId,
                        expiresAt = _lockExpiry
                    });
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, new
                    {
                        success = false,
                        message = "Another device is currently controlling SolidWorks. Please wait or ask them to release control.",
                        lockedByCurrentSession = false
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error acquiring lock: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error acquiring lock: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [Route("api/SolidWorks/ReleaseLock")]
        public HttpResponseMessage ReleaseLock()
        {
            try
            {
                string sessionId = GetSessionId();
                ReleaseSessionLock(sessionId);

                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    message = "Lock released successfully"
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error releasing lock: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error releasing lock: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [Route("api/SolidWorks/Heartbeat")]
        public HttpResponseMessage Heartbeat()
        {
            try
            {
                string sessionId = GetSessionId();

                lock (_lockObject)
                {
                    if (_currentSessionId == sessionId)
                    {
                        _lockExpiry = DateTime.Now.Add(_lockTimeout);
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = true,
                            message = "Heartbeat received, lock extended",
                            expiresAt = _lockExpiry
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.Forbidden, new
                        {
                            success = false,
                            message = "You don't hold the lock",
                            hasLock = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error processing heartbeat: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error processing heartbeat: {ex.Message}"
                });
            }
        }

        [HttpGet]
        [Route("api/SolidWorks/LockStatus")]
        public HttpResponseMessage GetLockStatus()
        {
            try
            {
                string sessionId = GetSessionId();
                bool hasLock = HasSessionLock(sessionId);

                lock (_lockObject)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = true,
                        hasLock = hasLock,
                        isLocked = _currentSessionId != null,
                        expiresAt = hasLock ? _lockExpiry : (DateTime?)null
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting lock status: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error getting lock status: {ex.Message}"
                });
            }
        }

        [HttpGet]
        [Route("api/SolidWorks/AvailableVersions")]
        public HttpResponseMessage GetAvailableVersions()
        {
            try
            {
                var versions = RegistryHelper.GetAvailableVersions();
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    success = true,
                    versions = versions
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting available versions: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error getting available versions: {ex.Message}",
                    versions = new List<string>()
                });
            }
        }

        [HttpPost]
        [Route("api/SolidWorks/Open")]
        public HttpResponseMessage OpenSolidWorks([FromBody] OpenSolidWorksRequest request)
        {
            try
            {
                string sessionId = GetSessionId();

                // Check if session has lock
                if (!HasSessionLock(sessionId))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        message = "You must acquire the lock before opening SolidWorks. Another device may be in control."
                    });
                }

                lock (_lockObject)
                {
                    if (_swApp != null)
                    {
                        // Check if SolidWorks is still running
                        try
                        {
                            var version = _swApp.RevisionNumber();
                            return Request.CreateResponse(HttpStatusCode.OK, new
                            {
                                success = true,
                                message = "SolidWorks is already open.",
                                version = version
                            });
                        }
                        catch
                        {
                            // SolidWorks was closed, reset reference
                            _swApp = null;
                        }
                    }

                    // Open SolidWorks with specified version
                    string versionToOpen = request?.Version;
                    _swApp = SolidWorksHelper.OpenSolidWorks(versionToOpen);

                    if (_swApp != null)
                    {
                        var version = _swApp.RevisionNumber();
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = true,
                            message = "SolidWorks opened successfully!",
                            version = version
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                        {
                            success = false,
                            message = "Failed to open SolidWorks. Please ensure SolidWorks is installed."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error opening SolidWorks: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error opening SolidWorks: {ex.Message}"
                });
            }
        }

        [HttpPost]
        [Route("api/SolidWorks/Close")]
        public HttpResponseMessage CloseSolidWorks()
        {
            try
            {
                string sessionId = GetSessionId();

                // Check if session has lock
                if (!HasSessionLock(sessionId))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        message = "You must have the lock to close SolidWorks. Another device may be in control."
                    });
                }

                lock (_lockObject)
                {
                    if (_swApp != null)
                    {
                        SolidWorksHelper.CloseSolidWorks(_swApp);
                        _swApp = null;
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = true,
                            message = "SolidWorks closed successfully."
                        });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = true,
                            message = "SolidWorks is not open."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error closing SolidWorks: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error closing SolidWorks: {ex.Message}"
                });
            }
        }

        [HttpGet]
        [Route("api/SolidWorks/Status")]
        public HttpResponseMessage GetStatus()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_swApp != null)
                    {
                        try
                        {
                            var version = _swApp.RevisionNumber();
                            return Request.CreateResponse(HttpStatusCode.OK, new
                            {
                                success = true,
                                isOpen = true,
                                version = version
                            });
                        }
                        catch
                        {
                            _swApp = null;
                            return Request.CreateResponse(HttpStatusCode.OK, new
                            {
                                success = true,
                                isOpen = false
                            });
                        }
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new
                        {
                            success = true,
                            isOpen = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting status: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error getting status: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Creates a 100 mm cube in SolidWorks, saves it as a .SLDPRT temp file,
        /// and returns a fileId that can be used to download it.
        /// Requires the session lock.
        /// </summary>
        [HttpPost]
        [Route("api/SolidWorks/CreateCube")]
        public HttpResponseMessage CreateCube()
        {
            try
            {
                string sessionId = GetSessionId();

                if (!HasSessionLock(sessionId))
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, new
                    {
                        success = false,
                        message = "You must acquire the lock before creating a cube."
                    });
                }

                lock (_lockObject)
                {
                    if (_swApp == null)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new
                        {
                            success = false,
                            message = "SolidWorks is not open. Please open SolidWorks first."
                        });
                    }

                    logger.Info($"Session {sessionId} requested cube creation.");

                    string filePath = SolidWorksHelper.CreateCube(_swApp);
                    string fileId = Guid.NewGuid().ToString("N");

                    lock (_cubeFiles)
                    {
                        _cubeFiles[fileId] = filePath;
                    }

                    logger.Info($"Cube created. fileId={fileId}, path={filePath}");

                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        success = true,
                        message = "Cube created successfully!",
                        fileId = fileId
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error creating cube: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error creating cube: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Streams the previously created cube .SLDPRT file as a download and deletes
        /// the temp file afterwards.
        /// </summary>
        [HttpGet]
        [Route("api/SolidWorks/DownloadCube")]
        public HttpResponseMessage DownloadCube(string fileId)
        {
            try
            {
                if (string.IsNullOrEmpty(fileId))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    {
                        success = false,
                        message = "fileId is required."
                    });
                }

                string filePath;
                lock (_cubeFiles)
                {
                    if (!_cubeFiles.TryGetValue(fileId, out filePath))
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, new
                        {
                            success = false,
                            message = "File not found. The cube may not have been created yet or the ID is invalid."
                        });
                    }
                }

                if (!File.Exists(filePath))
                {
                    return Request.CreateResponse(HttpStatusCode.Gone, new
                    {
                        success = false,
                        message = "The cube file no longer exists on the server."
                    });
                }

                logger.Info($"Serving cube download for fileId={fileId}, path={filePath}");

                // Read into memory so we can delete the temp file before sending
                byte[] fileBytes = File.ReadAllBytes(filePath);

                try { File.Delete(filePath); } catch (Exception ex) { logger.Warn(ex, $"Could not delete temp cube file: {filePath}"); }
                lock (_cubeFiles) { _cubeFiles.Remove(fileId); }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(fileBytes)
                };

                response.Content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");
                response.Content.Headers.ContentDisposition =
                    new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = "cube.SLDPRT"
                    };

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error downloading cube: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = $"Error downloading cube: {ex.Message}"
                });
            }
        }
    }

    public class OpenSolidWorksRequest
    {
        public string Version { get; set; }
    }
}

