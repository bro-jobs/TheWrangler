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
 * POST /resume  - Resume incomplete orders from previous session
 *
 * IMPLEMENTATION:
 * Uses TcpListener instead of HttpListener because HttpListener
 * requires assembly references not available in RebornBuddy's runtime.
 *
 * NOTES FOR CLAUDE:
 * - Uses raw TCP sockets with manual HTTP parsing
 * - Server runs on a background thread
 * - All responses are JSON (except /health which is plain text)
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ff14bot.Helpers;
using Newtonsoft.Json;

namespace TheWrangler
{
    /// <summary>
    /// HTTP server for remote control of TheWrangler.
    /// Uses raw TcpListener for compatibility with RebornBuddy.
    /// </summary>
    public class RemoteServer : IDisposable
    {
        #region Fields

        private readonly WranglerController _controller;
        private TcpListener _listener;
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
                _listener = new TcpListener(IPAddress.Any, _port);
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
            catch (SocketException ex)
            {
                Log($"Failed to start server: {ex.Message}");
                Log("Note: The port may already be in use.");
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
                    // AcceptTcpClient blocks until a client connects
                    var client = _listener.AcceptTcpClient();

                    // Handle request on thread pool
                    ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
                }
                catch (SocketException)
                {
                    // Expected when stopping
                    if (_isRunning)
                        Log("Socket exception occurred.");
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Log($"Error in listener loop: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles a connected client.
        /// </summary>
        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    // Set timeouts
                    stream.ReadTimeout = 5000;
                    stream.WriteTimeout = 5000;

                    // Read HTTP request
                    var request = ReadHttpRequest(stream);
                    if (request == null)
                    {
                        return; // Invalid request
                    }

                    // Process request and get response
                    var response = ProcessRequest(request);

                    // Send response
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                    stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling client: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads and parses an HTTP request from the stream.
        /// </summary>
        private HttpRequest ReadHttpRequest(NetworkStream stream)
        {
            try
            {
                var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, true);

                // Read request line
                var requestLine = reader.ReadLine();
                if (string.IsNullOrEmpty(requestLine))
                    return null;

                var parts = requestLine.Split(' ');
                if (parts.Length < 2)
                    return null;

                var request = new HttpRequest
                {
                    Method = parts[0].ToUpper(),
                    Path = parts[1].ToLower(),
                    Headers = new Dictionary<string, string>()
                };

                // Read headers
                string line;
                int contentLength = 0;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex).Trim().ToLower();
                        var value = line.Substring(colonIndex + 1).Trim();
                        request.Headers[key] = value;

                        if (key == "content-length")
                        {
                            int.TryParse(value, out contentLength);
                        }
                    }
                }

                // Read body if present
                if (contentLength > 0)
                {
                    var buffer = new char[contentLength];
                    var read = reader.Read(buffer, 0, contentLength);
                    request.Body = new string(buffer, 0, read);
                }

                return request;
            }
            catch (Exception ex)
            {
                Log($"Error reading request: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes an HTTP request and returns the response string.
        /// </summary>
        private string ProcessRequest(HttpRequest request)
        {
            int statusCode = 200;
            string statusText = "OK";
            string contentType = "application/json";
            string body;

            try
            {
                // Handle CORS preflight
                if (request.Method == "OPTIONS")
                {
                    return BuildResponse(200, "OK", "text/plain", "",
                        "Access-Control-Allow-Origin: *",
                        "Access-Control-Allow-Methods: GET, POST, OPTIONS",
                        "Access-Control-Allow-Headers: Content-Type");
                }

                // Route request
                switch (request.Path)
                {
                    case "/status":
                        body = HandleStatus();
                        break;

                    case "/health":
                        body = "ok";
                        contentType = "text/plain";
                        break;

                    case "/run":
                        if (request.Method != "POST")
                        {
                            statusCode = 405;
                            statusText = "Method Not Allowed";
                            body = JsonConvert.SerializeObject(new { error = "Method not allowed" });
                        }
                        else
                        {
                            body = HandleRun(request.Body);
                        }
                        break;

                    case "/stop":
                        if (request.Method != "POST")
                        {
                            statusCode = 405;
                            statusText = "Method Not Allowed";
                            body = JsonConvert.SerializeObject(new { error = "Method not allowed" });
                        }
                        else
                        {
                            body = HandleStop();
                        }
                        break;

                    case "/resume":
                        if (request.Method != "POST")
                        {
                            statusCode = 405;
                            statusText = "Method Not Allowed";
                            body = JsonConvert.SerializeObject(new { error = "Method not allowed" });
                        }
                        else
                        {
                            body = HandleResume();
                        }
                        break;

                    default:
                        statusCode = 404;
                        statusText = "Not Found";
                        body = JsonConvert.SerializeObject(new { error = "Not found" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing request: {ex.Message}");
                statusCode = 500;
                statusText = "Internal Server Error";
                body = JsonConvert.SerializeObject(new { error = ex.Message });
            }

            return BuildResponse(statusCode, statusText, contentType, body,
                "Access-Control-Allow-Origin: *");
        }

        /// <summary>
        /// Builds an HTTP response string.
        /// </summary>
        private string BuildResponse(int statusCode, string statusText, string contentType,
            string body, params string[] extraHeaders)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"HTTP/1.1 {statusCode} {statusText}");
            sb.AppendLine($"Content-Type: {contentType}; charset=utf-8");
            sb.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(body)}");
            sb.AppendLine("Connection: close");

            foreach (var header in extraHeaders)
            {
                sb.AppendLine(header);
            }

            sb.AppendLine(); // Empty line between headers and body
            sb.Append(body);

            return sb.ToString();
        }

        /// <summary>
        /// Handles GET /status - returns current state.
        /// </summary>
        private string HandleStatus()
        {
            // Get character info if available
            string characterName = "Unknown";
            try
            {
                if (ff14bot.Core.Me != null)
                {
                    characterName = ff14bot.Core.Me.Name ?? "Unknown";
                }
            }
            catch
            {
                // Ignore errors - character info may not be available
            }

            var status = new
            {
                state = GetStateString(),
                isExecuting = _controller.IsExecuting,
                hasPendingOrder = _controller.HasPendingOrder,
                hasIncompleteOrders = _controller.HasIncompleteOrders(),
                currentFile = WranglerSettings.Instance.JsonFileName ?? "None",
                apiStatus = _controller.ApiStatus,
                botRunning = TheWranglerBotBase.IsBotRunning,
                characterName = characterName,
                runtimeSeconds = _controller.ExecutionRuntimeSeconds,
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
        private string HandleRun(string body)
        {
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

        /// <summary>
        /// Handles POST /resume - resumes incomplete orders.
        /// </summary>
        private string HandleResume()
        {
            if (_controller.IsExecuting)
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Already executing" });
            }

            if (!_controller.HasIncompleteOrders())
            {
                return JsonConvert.SerializeObject(new { success = false, error = "No incomplete orders" });
            }

            bool resumed = _controller.ResumeIncompleteOrders();

            if (resumed)
            {
                // Start bot if not running
                if (!TheWranglerBotBase.IsBotRunning)
                {
                    TheWranglerBotBase.StartBot();
                }

                return JsonConvert.SerializeObject(new { success = true, message = "Resuming incomplete orders" });
            }
            else
            {
                return JsonConvert.SerializeObject(new { success = false, error = "Failed to resume orders" });
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Simple HTTP request representation.
        /// </summary>
        private class HttpRequest
        {
            public string Method { get; set; }
            public string Path { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public string Body { get; set; }
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
