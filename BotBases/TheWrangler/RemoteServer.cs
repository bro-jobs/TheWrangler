/*
 * RemoteServer.cs - HTTP Server for Remote Control
 * =================================================
 *
 * PURPOSE:
 * Provides an HTTP API for controlling TheWrangler remotely.
 * Allows a master program to control multiple Wrangler instances
 * across a local network.
 *
 * ENDPOINTS:
 * GET  /status  - Returns current status as JSON
 * GET  /health  - Simple health check (returns "ok")
 * POST /run     - Start execution with JSON body: {"jsonPath":"..."} or {"json":"..."}
 * POST /stop    - Trigger StopGently
 *
 * USAGE:
 * The server starts automatically when TheWrangler BotBase is selected
 * if remote control is enabled in settings.
 *
 * NOTES FOR CLAUDE:
 * - HttpListener requires appropriate permissions on Windows
 * - Server runs on a background thread
 * - All responses are JSON (except /health which is plain text)
 */

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ff14bot.Helpers;
using Newtonsoft.Json;

namespace TheWrangler
{
    /// <summary>
    /// HTTP server for remote control of TheWrangler.
    /// </summary>
    public class RemoteServer : IDisposable
    {
        #region Fields

        private readonly WranglerController _controller;
        private HttpListener _listener;
        private Thread _listenerThread;
        private volatile bool _isRunning;
        private readonly int _port;

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if the server is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// The port the server is listening on.
        /// </summary>
        public int Port => _port;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new RemoteServer instance.
        /// </summary>
        /// <param name="controller">The controller to use for operations</param>
        /// <param name="port">The port to listen on</param>
        public RemoteServer(WranglerController controller, int port)
        {
            _controller = controller;
            _port = port;
        }

        #endregion

        #region Server Control

        /// <summary>
        /// Starts the HTTP server.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                Log("Server already running.");
                return;
            }

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://+:{_port}/");

                _listener.Start();
                _isRunning = true;

                _listenerThread = new Thread(ListenerLoop)
                {
                    IsBackground = true,
                    Name = "TheWrangler Remote Server"
                };
                _listenerThread.Start();

                Log($"Remote server started on port {_port}");
            }
            catch (HttpListenerException ex)
            {
                Log($"Failed to start server: {ex.Message}");
                Log("Note: You may need to run as administrator or add URL reservation:");
                Log($"  netsh http add urlacl url=http://+:{_port}/ user=Everyone");
                _isRunning = false;
            }
            catch (Exception ex)
            {
                Log($"Error starting server: {ex.Message}");
                _isRunning = false;
            }
        }

        /// <summary>
        /// Stops the HTTP server.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            try
            {
                _listener?.Stop();
                _listener?.Close();
            }
            catch (Exception ex)
            {
                Log($"Error stopping server: {ex.Message}");
            }

            Log("Remote server stopped.");
        }

        #endregion

        #region Request Handling

        /// <summary>
        /// Main listener loop - runs on background thread.
        /// </summary>
        private void ListenerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // GetContext blocks until a request comes in
                    var context = _listener.GetContext();

                    // Handle request on thread pool
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping
                    if (_isRunning)
                        Log("Listener exception occurred.");
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Log($"Error in listener loop: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles an individual HTTP request.
        /// </summary>
        private void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Add CORS headers for browser access
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // Handle preflight
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath.ToLower();
                var method = request.HttpMethod;

                string responseBody;
                string contentType = "application/json";

                switch (path)
                {
                    case "/status":
                        responseBody = HandleStatus();
                        break;

                    case "/health":
                        responseBody = "ok";
                        contentType = "text/plain";
                        break;

                    case "/run":
                        if (method != "POST")
                        {
                            response.StatusCode = 405;
                            responseBody = JsonConvert.SerializeObject(new { error = "Method not allowed" });
                        }
                        else
                        {
                            responseBody = HandleRun(request);
                        }
                        break;

                    case "/stop":
                        if (method != "POST")
                        {
                            response.StatusCode = 405;
                            responseBody = JsonConvert.SerializeObject(new { error = "Method not allowed" });
                        }
                        else
                        {
                            responseBody = HandleStop();
                        }
                        break;

                    default:
                        response.StatusCode = 404;
                        responseBody = JsonConvert.SerializeObject(new { error = "Not found" });
                        break;
                }

                // Send response
                response.ContentType = contentType;
                var buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Log($"Error handling request: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    var errorBody = JsonConvert.SerializeObject(new { error = ex.Message });
                    var buffer = Encoding.UTF8.GetBytes(errorBody);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch { }
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        /// <summary>
        /// Handles GET /status - returns current state.
        /// </summary>
        private string HandleStatus()
        {
            var status = new
            {
                state = GetStateString(),
                isExecuting = _controller.IsExecuting,
                hasPendingOrder = _controller.HasPendingOrder,
                currentFile = WranglerSettings.Instance.JsonFileName ?? "None",
                apiStatus = _controller.ApiStatus,
                botRunning = TheWranglerBotBase.IsBotRunning,
                timestamp = DateTime.UtcNow.ToString("o")
            };

            return JsonConvert.SerializeObject(status);
        }

        /// <summary>
        /// Gets a human-readable state string.
        /// </summary>
        private string GetStateString()
        {
            if (_controller.IsExecuting)
                return "executing";
            if (_controller.HasPendingOrder)
                return "pending";
            if (!TheWranglerBotBase.IsBotRunning)
                return "stopped";
            return "idle";
        }

        /// <summary>
        /// Handles POST /run - starts execution.
        /// </summary>
        private string HandleRun(HttpListenerRequest request)
        {
            // Read request body
            string body;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Empty request body" });
            }

            try
            {
                dynamic data = JsonConvert.DeserializeObject(body);
                string json = null;

                // Option 1: jsonPath - load from file
                if (data.jsonPath != null)
                {
                    string jsonPath = (string)data.jsonPath;
                    if (!File.Exists(jsonPath))
                    {
                        return JsonConvert.SerializeObject(new { success = false, error = $"File not found: {jsonPath}" });
                    }
                    json = File.ReadAllText(jsonPath);

                    // Update settings so UI reflects the file
                    WranglerSettings.Instance.LastJsonPath = jsonPath;
                    WranglerSettings.Instance.Save();
                }
                // Option 2: json - use directly
                else if (data.json != null)
                {
                    json = (string)data.json;
                }
                else
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Must provide 'jsonPath' or 'json'" });
                }

                // Check if already executing
                if (_controller.IsExecuting)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Already executing" });
                }

                // Queue the order
                bool queued = _controller.QueueOrderJson(json);

                if (queued)
                {
                    // Start bot if not running
                    if (!TheWranglerBotBase.IsBotRunning)
                    {
                        TheWranglerBotBase.StartBot();
                    }

                    return JsonConvert.SerializeObject(new { success = true, message = "Order queued" });
                }
                else
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Failed to queue order" });
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Handles POST /stop - triggers StopGently.
        /// </summary>
        private string HandleStop()
        {
            if (!_controller.IsExecuting)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Nothing executing" });
            }

            _controller.RequestStopGently();
            return JsonConvert.SerializeObject(new { success = true, message = "Stop requested" });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #region Logging

        private void Log(string message)
        {
            Logging.Write($"[TheWrangler] [Remote] {message}");
        }

        #endregion
    }
}
