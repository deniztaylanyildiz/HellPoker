namespace HellPoker.Core.Game
{
    /// <summary>
    /// The version as players see it: a demo build ("1.0.0-demo") reads "Demo 1.0"; any other reads "v" + the build's own number
    /// ("0.1.6" → "v0.1.6"). The menu's corner, the run log's header and the release zip use it.
    /// </summary>
    public static class ReleaseVersion
    {
        public const string DemoSuffix = "-demo";

        public static bool IsDemo(string bundleVersion) => bundleVersion != null && bundleVersion.EndsWith(DemoSuffix);

        public static string Display(string bundleVersion)
        {
            if (string.IsNullOrEmpty(bundleVersion)) return "";
            if (!IsDemo(bundleVersion)) return "v" + bundleVersion;
            string number = bundleVersion.Substring(0, bundleVersion.Length - DemoSuffix.Length);
            string[] parts = number.Split('.');
            return "Demo " + (parts.Length >= 2 ? parts[0] + "." + parts[1] : number);
        }

        /// <summary>The release's name in file names ("Demo 1.0" → "Demo-1.0"; "0.1.6" stays "0.1.6").</summary>
        public static string FileName(string bundleVersion) => IsDemo(bundleVersion) ? Display(bundleVersion).Replace(' ', '-') : bundleVersion;
    }
}