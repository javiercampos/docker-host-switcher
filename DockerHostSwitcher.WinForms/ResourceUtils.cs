using System.Drawing;
using System.IO;
using System.Reflection;

namespace DockerHostSwitcher.WinForms
{
    public static class ResourceUtils
    {
        private static string? GetNamespace() => typeof(Program).Namespace;

        private static Stream? GetEmbeddedResourceStream(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = GetNamespace() + ".res." + name;
            return assembly.GetManifestResourceStream(resourceName);
        }

        public static Icon ReadDockerIcon()
        {
            using var stream = GetEmbeddedResourceStream("Docker.ico");
            return new Icon(stream);
        }

        public static Image ReadDockerLogo()
        {
            using var stream = GetEmbeddedResourceStream("Docker-logo.png");
            return new Bitmap(stream);
        }
    }
}