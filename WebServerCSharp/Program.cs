/* Downloaded from Code Project
 * https://www.codeproject.com/articles/1505/create-your-own-web-server-using-c
 * 
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

// To make a C# webserver you only really needed to add the
// System.Net library, Threading is advised because it allows 
// your webserver to be multithreaded.

namespace WebServerCSharp
{
    class WebServer
    {
        // create an instance of HTTP Listener, this allows you to send requests
        // to your webserver. Without listener your web server would be active but
        //  you would be unable to interact.
        // HttpListener is inherited from System.Net
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(IReadOnlyCollection<string> prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
            {
                // apparently if you're running a dinosaur of an OS you won't have listener.
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            // URI prefixes are required eg: "http://localhost:8080/test/"
            if (prefixes == null || prefixes.Count == 0)
            {
                throw new ArgumentException("URI prefixes are required");
            }

            if (method == null)
            {
                throw new ArgumentException("responder method required");
            }

            foreach (var s in prefixes)
            {
                _listener.Prefixes.Add(s);
            }
            // _responderMethod is what provides our response to localhost:8080/test
            // this var is an HttpListenerRequest
            _responderMethod = method;
            _listener.Start();
        }

        // Appears to be the default constructor for WebServer
        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method)
        {
        }

        // The Run method creates our working thread of our web server(s)
        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Console.WriteLine("Webserver running...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                if (ctx == null)
                                {
                                    return;
                                }

                                var rstr = _responderMethod(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch
                            {
                                // ignored
                            }
                            finally
                            {
                                // always close the stream
                                if (ctx != null)
                                {
                                    ctx.Response.OutputStream.Close();
                                }
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception ex)
                {
                    // ignored
                }
            });
        }
        // The stop method of the HttpListener class closes all active listener threads running
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
    // The program class is our actual page that will be returned by the webserver when a valid
    // request is passed i.e. localhost:8080/test in our case.
    internal class Program
    {
        public static string SendResponse(HttpListenerRequest request)
        {
            return string.Format("<HTML><BODY>My web page.<br>{0}</BODY></HTML>", DateTime.Now);
        }

        private static void Main(string[] args)
        {
            var ws = new WebServer(SendResponse, "http://localhost:8080/test/");
            ws.Run();
            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }
    }
}

/*A simple webserver. Press a key to quit.
 * Webserver running...
 * C:\Users\david\source\repos\WebServerCSharp\WebServerCSharp\bin\Debug\netcoreapp3.1\WebServerCSharp.exe (process 11688) exited with code 0.
 * To automatically close the console when debugging stops, enable Tools->Options->Debugging->Automatically close the console when debugging stops.
 * Press any key to close this window . . .
 * 
 * 
 * My web page.
 * 12/9/2019 7:40:17 AM
 */

