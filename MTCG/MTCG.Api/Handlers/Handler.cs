using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json;
using MTCG.Models;

namespace MTCG;

    /// <summary>This class provides an abstract implementation of the
    /// <see cref="IHandler"/> interface. It also implements static methods
    /// that handles an incoming HTTP request by discovering and calling
    /// available handlers.</summary>
    public abstract class Handler: IHandler
    {
        /// <summary>List of available handlers.</summary>
        private static List<IHandler>? _Handlers = null;
        
        /// <summary>Discovers and returns all available handler implementations.</summary>
        /// <returns>Returns a list of available handlers.</returns>
        private static List<IHandler> _GetHandlers()
        {
            List<IHandler> rval = new();

            foreach(Type i in Assembly.GetExecutingAssembly().GetTypes()
                              .Where(m => m.IsAssignableTo(typeof(IHandler)) && (!m.IsAbstract)))
            {
                IHandler? h = (IHandler?) Activator.CreateInstance(i);
                if(h != null)
                {
                    rval.Add(h);
                }
            }

            return rval;
        }
        
        /// <summary>Handles an incoming HTTP request.</summary>
        /// <param name="e">Event arguments.</param>
        public static async Task HandleEventAsync(HttpSvrEventArgs e)
        {
            _Handlers ??= _GetHandlers();

            foreach(IHandler i in _Handlers)
            {
                if(await i.Handle(e)) return;
            }
            e.Reply(HttpStatusCode.BAD_REQUEST);
        }
        public virtual async Task<bool> Handle(HttpSvrEventArgs e)
        {
            // Methodensuche via Reflection
            var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<RouteAttribute>() != null);

            // Request-Pfad aufräumen (ohne führenden/trailing /)
            var normalizedPath = e.Path.Trim('/').Trim();

            foreach (var method in methods)
            {
                var route = method.GetCustomAttribute<RouteAttribute>()!;
        
                // Methode (GET, POST, etc.) vergleichen
                if (!string.Equals(route.Method, e.Method, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Versuche Platzhalter-Abgleich
                var placeholders = MatchRoute(route.Path, normalizedPath);
                if (placeholders != null) // => Erfolg
                {
                    // Du kannst nun placeholders in e.RouteParams o.Ä. speichern:
                    e.RouteParams.Clear();
                    foreach (var kvp in placeholders)
                    {
                        e.RouteParams[kvp.Key] = kvp.Value;
                    }

                    // Methode asynchron aufrufen
                    var resultTask = (Task<(int Status, JsonObject? Reply)>)method.Invoke(this, new object[] { e });
                    var result = await resultTask;
                    e.Reply(result.Status, result.Reply?.ToJsonString());
                    return true;
                }
            }

            return false;
        }
        
        private Dictionary<string, string>? MatchRoute(string routeDefinition, string requestPath)
        {
            var routeParts = routeDefinition.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var requestParts = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Gleiche Anzahl Segmente erforderlich
            if (routeParts.Length != requestParts.Length)
                return null;

            var placeholders = new Dictionary<string, string>();
            for (int i = 0; i < routeParts.Length; i++)
            {
                string patternSegment = routeParts[i];
                string requestSegment = requestParts[i];

                if (patternSegment.StartsWith("{") && patternSegment.EndsWith("}"))
                {
                    // => Platzhalter, z.B. "{username}"
                    string key = patternSegment[1..^1]; // "username"
                    placeholders[key] = requestSegment; // z.B. "testuser"
                }
                else
                {
                    // Statisches Segment, muss exakt übereinstimmen
                    if (!string.Equals(patternSegment, requestSegment, StringComparison.OrdinalIgnoreCase))
                    {
                        return null; // passt nicht
                    }
                }
            }

            return placeholders;
        }
        
        protected (int Status, JsonObject? Reply) Ok<T>(T data) => 
            (HttpStatusCode.OK, JsonSerializer.SerializeToNode(ApiResponseDto<T>.SuccessResponse(data))?.AsObject());

        protected (int Status, JsonObject? Reply) Ok(string message) => 
            (HttpStatusCode.OK, JsonSerializer.SerializeToNode(ApiResponseDto<object>.SuccessResponse(message))?.AsObject());

        protected (int Status, JsonObject? Reply) TokenOk(string token, string message) => 
            (HttpStatusCode.OK, JsonSerializer.SerializeToNode(ApiResponseDto<object>.TokenResponse(token, message))?.AsObject());

        protected (int Status, JsonObject? Reply) BadRequest(string message) => 
            (HttpStatusCode.BAD_REQUEST, JsonSerializer.SerializeToNode(ApiResponseDto<object>.ErrorResponse(message))?.AsObject());

        protected (int Status, JsonObject? Reply) Unauthorized(string message = "Unauthorized") => 
            (HttpStatusCode.UNAUTHORIZED, JsonSerializer.SerializeToNode(ApiResponseDto<object>.ErrorResponse(message))?.AsObject());
    }