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
        public static void HandleEvent(HttpSvrEventArgs e)
        {
            _Handlers ??= _GetHandlers();

            foreach(IHandler i in _Handlers)
            {
                if(i.Handle(e)) return;
            }
            e.Reply(HttpStatusCode.BAD_REQUEST);
        }
        
        public virtual bool Handle(HttpSvrEventArgs e)
        {
            var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<RouteAttribute>() != null);

            foreach (var method in methods)
            {
                var route = method.GetCustomAttribute<RouteAttribute>()!;
                var normalizedPath = e.Path.TrimEnd('/', ' ', '\t').TrimStart('/');
                
                if (route.Path == normalizedPath && route.Method == e.Method)
                {
                    var result = ((int Status, JsonObject? Reply))method.Invoke(this, new[] { e })!;
                    e.Reply(result.Status, result.Reply?.ToJsonString());
                    return true;
                }
            }

            return false;
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