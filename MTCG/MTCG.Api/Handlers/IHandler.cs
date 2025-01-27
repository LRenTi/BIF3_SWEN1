using System;

namespace MTCG;
    /// <summary>Handlers that handle HTTP requests implement this interface.</summary>
    public interface IHandler
    {

        /// <summary>Tries to handle a HTTP request.</summary>
        /// <param name="e">Event arguments.</param>
        /// <returns>Returns TRUE if the request was handled by this instance,
        ///          otherwise returns FALSE.</returns>
        public Task<bool> Handle(HttpSvrEventArgs e);
    }