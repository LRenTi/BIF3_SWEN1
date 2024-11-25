namespace MTCG.MTCG.Api.Attributes;

public class RouteAttribute : Attribute
{
    public string Path { get; }
    public string Method { get; }

    public RouteAttribute(string path, string method)
    {
        Path = path.TrimStart('/').TrimEnd('/');
        Method = method.ToUpper();
    }
} 