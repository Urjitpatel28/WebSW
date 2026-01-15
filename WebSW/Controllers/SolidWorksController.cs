using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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

        static SolidWorksController()
        {
            logger = LoggingService.ConfigureLogger(@"C:\wwwroot");
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
    }

    public class OpenSolidWorksRequest
    {
        public string Version { get; set; }
    }
}

