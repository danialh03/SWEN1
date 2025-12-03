using System.Net;



namespace DhProjekt.Server;

public sealed class HttpRestServer: IDisposable
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // private members                                                                                                  //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>HTTP listener object.</summary>
    private readonly HttpListener _Listener;



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // constructors                                                                                                     //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Creates a new instance of this class.</summary>
    /// <param name="port">Port number for the server.</param>
    public HttpRestServer(int port = 8080)
    {
        _Listener = new();
        _Listener.Prefixes.Add($"http://+:{port}/");
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public events                                                                                                    //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>The event is raised when a request has been received.</summary>
    public event EventHandler<HttpRestEventArgs>? RequestReceived;



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public properties                                                                                                //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Gets a value indicating if the server is running.</summary>
    public bool Running
    {
        get; private set;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // public methods                                                                                                   //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Starts an runs the server.</summary>
    public void Run()
    {
        if(Running) return;

        _Listener.Start();
        Running = true;

        while(Running)
        {
            HttpListenerContext context = _Listener.GetContext();

            _ = Task.Run(() =>
            {
                HttpRestEventArgs args = new(context);
                RequestReceived?.Invoke(this, args);

                if(!args.Responded)
                {
                    args.Respond((int) HttpStatusCode.NotFound, new() { ["success"] = false, ["reason"] = "Not found." });
                }
            });
        }
    }


    /// <summary>Stops the server.</summary>
    public void Stop()
    {
        _Listener.Close();
        Running = false;
    }



    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // [interface] IDisposable                                                                                          //
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    
    /// <summary>Disposes the object and releases used resources.</summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Dispose()
    {
        ((IDisposable) _Listener).Dispose();
    }
}
