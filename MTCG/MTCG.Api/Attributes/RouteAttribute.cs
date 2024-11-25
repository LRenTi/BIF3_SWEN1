namespace MTCG;

public class RouteAttribute : Attribute
{
    public string Path { get; }
    public string Method { get; }

    public RouteAttribute( string method, string path)
    {
        Path = path.TrimStart('/').TrimEnd('/');
        Method = method.ToUpper();
    }
} 