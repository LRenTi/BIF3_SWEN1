using System.Reflection;

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
        
        public abstract bool Handle(HttpSvrEventArgs e);
    }