using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MTCG;
    /// <summary>This class implements a HTTP server.</summary>
    public sealed class HttpSvr
    {
        /// <summary>TCP listener instance.</summary>
        private TcpListener? _Listener;
        
        /// <summary>Is raised when incoming data is available.</summary>
        public event HttpSvrEventHandler? Incoming;
        
        /// <summary>Gets if the server is available.</summary>
        public bool Active
        {
            get; private set;
        } = false;
        
        /// <summary>Runs the server.</summary>
        public async Task Run()
        {
            if(Active) return;

            Active = true;
            _Listener = new(IPAddress.Parse("127.0.0.1"), 12000);
            _Listener.Start();

            byte[] buf = new byte[256];

            while(Active)
            {
                TcpClient client = _Listener.AcceptTcpClient();
                string data = string.Empty;
                
                while(client.GetStream().DataAvailable || string.IsNullOrWhiteSpace(data))
                {
                    int n = await client.GetStream().ReadAsync(buf, 0, buf.Length);
                    data += Encoding.ASCII.GetString(buf, 0, n);
                }

                Incoming?.Invoke(this, new(client, data));
            }
        }


        /// <summary>Stops the server.</summary>
        public void Stop()
        {
            Active = false;
        }
    }