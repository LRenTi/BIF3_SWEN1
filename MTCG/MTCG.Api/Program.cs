using System;
using MTCG.Data;

namespace MTCG;
    /// <summary>This class contains the main entry point of the application.</summary>
    internal class Program
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // public constants                                                                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Determines if debug token ("UserName-debug") will be accepted.</summary>
        public const bool ALLOW_DEBUG_TOKEN = true;

        
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // entry point                                                                                                      //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        /// <summary>Application entry point.</summary>
        /// <param name="args">Command line arguments.</param>
        static void Main(string[] args)
        {
            Console.WriteLine("Server started!");
            HttpSvr svr = new();
            svr.Incoming += Svr_Incoming; //(sender, e) => { Handler.HandleEvent(e); };

            svr.Run();
        }
        
        private static void Svr_Incoming(object sender, HttpSvrEventArgs e)
        {
            Handler.HandleEvent(e);
        }
    }