namespace Lyt.QrCode.Decode.Demo;

public static class ResourcesUtilities
{
    private static string ResourcesPath = "Lyt.QrCode.Resources";
    private static Assembly ExecutingAssembly;

    public const string ResourcesExtension = ".json";

    static ResourcesUtilities() => ExecutingAssembly = Assembly.GetExecutingAssembly();

    public static void SetResourcesPath(string resourcePath) => ResourcesUtilities.ResourcesPath = resourcePath;

    public static void SetExecutingAssembly(Assembly executingAssembly)
    {
        ResourcesUtilities.ExecutingAssembly = executingAssembly;
        DumpEmbeddedResourceNames();
    }

    public static List<string> EnumerateEmbeddedResourceNames(string filter)
    {
        List<string> resourceNames = [];
        var list = ExecutingAssembly.GetManifestResourceNames().ToList();
        foreach (string name in list)
        {
            if (name.Contains(ResourcesPath))
            {
                Debug.WriteLine(name);
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    if (!name.Contains(filter))
                    {
                        continue;
                    }
                }

                resourceNames.Add(name);
            }
        }

        return resourceNames;
    }

    public static string? GetFullResourceName(string name)
    {
        var resourceNames = ExecutingAssembly.GetManifestResourceNames().ToList();
        if (resourceNames.Count == 0)
        {
            throw new Exception("No resources. (Make sure to set Build Action to Embedded.)");
        }

        return resourceNames.Single(str => str.EndsWith(name));
    }

    public static string LoadEmbeddedTextResource(string name, out string? resourceName)
    {
        resourceName = ResourcesUtilities.GetFullResourceName(name);
        if (!string.IsNullOrEmpty(resourceName))
        {
            var stream = ExecutingAssembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using (stream)
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        throw new Exception("Failed to load resource: " + name);
    }

    public static byte[] LoadEmbeddedBinaryResource(string name, out string? resourceName)
    {
        resourceName = ResourcesUtilities.GetFullResourceName(name);
        if (!string.IsNullOrEmpty(resourceName))
        {
            var stream = ExecutingAssembly.GetManifestResourceStream(resourceName);
            if (stream is not null)
            {
                using (stream)
                {
                    byte[] bytes = new byte[stream.Length];
                    int bytesRead = stream.Read(bytes, 0, bytes.Length);
                    if (bytesRead != bytes.Length)
                    {
                        throw new Exception("Failed to read resource stream: " + name);
                    }

                    return bytes;
                }
            }
        }

        throw new Exception("Failed to load resource: " + name);
    }

    [Conditional("DEBUG")]
    public static void DumpEmbeddedResourceNames()
    {
        List<string> resourceNames = [];
        var list = ExecutingAssembly.GetManifestResourceNames().ToList();
        foreach (string name in list)
        {
            Debug.WriteLine(name);

            if (name.Contains(ResourcesPath))
            {
                Debug.WriteLine(name);
                resourceNames.Add(name);
            }
        }
    }
}