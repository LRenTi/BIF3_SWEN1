namespace MTCG;

/// <summary>
/// This attribute is used to define a route for a handler method.
/// </summary>
public class RouteAttribute : Attribute
{
    public string Path { get; }
    public string Method { get; }

    public RouteAttribute( string method, string path)
    {
        Path = path;
        Method = method.ToUpper();
    }
} 