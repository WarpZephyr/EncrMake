namespace EncrMake.Helpers
{
    internal static class PathHelper
    {
        internal static string Unroot(string path)
        {
            int rootIndex = path.IndexOf(':');
            if (rootIndex > -1)
            {
                int nextIndex = rootIndex + 1;
                if (nextIndex < path.Length)
                {
                    return path[(nextIndex)..];
                }
                else
                {
                    return string.Empty;
                }
            }

            return path;
        }
    }
}
