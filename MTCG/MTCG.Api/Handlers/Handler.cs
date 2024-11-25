using System.Reflection;
using System.Text.Json.Nodes;

namespace MTCG;

    /// <summary>This class provides an abstract implementation of the
    /// <see cref="IHandler"/> interface. It also implements static methods
    /// that handles an incoming HTTP request by discovering and calling
    /// available handlers.</summary>
    public abstract class Handler: IHandler
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private members                                                                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>List of available handlers.</summary>
        private static List<IHandler>? _Handlers = null;

        

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // private static methods                                                                                           //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
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



        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public static methods                                                                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
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

        protected static JsonObject CreateErrorReply(string message) => new()
        {
            ["success"] = false,
            ["message"] = message
        };
    }