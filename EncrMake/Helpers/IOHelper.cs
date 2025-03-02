using System.IO;

namespace EncrMake.Helpers
{
    internal static class IOHelper
    {
        internal static void BackupFile(string path)
        {
            if (File.Exists(path))
            {
                string backupPath = path + ".bak";
                if (!File.Exists(backupPath))
                {
                    File.Move(path, backupPath);
                }
            }
        }
    }
}
