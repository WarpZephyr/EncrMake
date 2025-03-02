using EncrMake.Crypto;
using EncrMake.Helpers;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace EncrMake
{
    internal class Program
    {
        const string KeyFileName = "key.txt";
        const string IvFileName = "iv.txt";
        const string Extension = ".encr.bin";

        static bool ShowWarning;
        static bool ShowError;

        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
                Process(args);
#if !DEBUG
            }
            catch (Exception ex)
            {
                Error($"An error occurred: {ex}");
            }
#endif

            Console.WriteLine("Finished.");
            if (ShowWarning || ShowError)
            {
                Console.ReadKey();
            }
        }

        #region Processing

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Process(string[] args)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string? programFolder = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(programFolder))
            {
                Error("Could not find program folder.");
                return;
            }

            string keyPath = Path.Combine(programFolder, KeyFileName);
            if (!File.Exists(keyPath))
            {
                Error($"Could not find {KeyFileName} in program folder.");
                return;
            }

            string ivPath = Path.Combine(programFolder, IvFileName);
            if (!File.Exists(ivPath))
            {
                Error($"Could not find {IvFileName} in program folder.");
                return;
            }

            byte[] key = File.ReadAllText(keyPath).HexToBytes();
            byte[] iv = File.ReadAllText(ivPath).HexToBytes();
            var cipher = new AesCipher(key, iv, System.Security.Cryptography.CipherMode.CBC, System.Security.Cryptography.PaddingMode.Zeros, 128, 128);
            foreach (string arg in args)
            {
#if !DEBUG
                try
                {
#endif
                    ProcessArgument(arg, cipher);
#if !DEBUG
                }
                catch (Exception ex)
                {
                    Error($"An unknown error occurred while processing: {arg}\nError: {ex}");
                }
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ProcessArgument(string arg, AesCipher cipher)
        {
            if (File.Exists(arg))
            {
                ProcessFile(arg, cipher);
            }
            else if (Directory.Exists(arg))
            {
                ProcessDirectory(arg, cipher);
            }
            else
            {
                Warn($"Neither file nor folder could be found at: {arg}");
            }
        }

        static void ProcessDirectory(string folder, AesCipher cipher)
        {
            foreach (string path in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                ProcessFile(path, cipher);
            }
        }

        static void ProcessFile(string path, AesCipher cipher)
        {
            string newPath;
            byte[] result;
            if (path.EndsWith(Extension, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"Decrypting: {Path.GetFileName(path)}");
                var encr = ENCR.Read(cipher.Decrypt(File.ReadAllBytes(path)));
                newPath = ProcessDecryptPath(path, encr);
                result = encr.Bytes;
            }
            else
            {
                string fileName = Path.GetFileName(path);
                Console.WriteLine($"Encrypting: {fileName}");
                var encr = new ENCR(fileName, File.ReadAllBytes(path));
                newPath = $"{path}{Extension}";
                result = cipher.Encrypt(encr.Write());
            }

            IOHelper.BackupFile(newPath);
            File.WriteAllBytes(newPath, result);
        }

        static string ProcessDecryptPath(string path, ENCR encr)
        {
            string newPath;
            string? directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Warn($"Could not get folder name for file, defaulting to something else: {directory}");
                newPath = path[..^Extension.Length];
            }
            else
            {
                // Just in case some weirdo puts root info in the name
                string fileName = PathHelper.Unroot(encr.Name);
                if (fileName == string.Empty)
                {
                    Warn($"True file name only had a root, defaulting to something else: {encr.Name}");
                    newPath = path[..^Extension.Length];
                }
                else
                {
                    newPath = Path.Combine(directory, fileName);
                    string? safeDirectory = Path.GetDirectoryName(newPath);
                    if (!string.IsNullOrWhiteSpace(safeDirectory) && safeDirectory != directory)
                    {
                        // Just in case some weirdo puts directory info in the name
                        Directory.CreateDirectory(safeDirectory);
                    }
                }
            }

            return newPath;
        }

        #endregion

        #region Logging

        static void Warn(string message)
        {
            Console.WriteLine($"Warn: {message}");
            ShowWarning = true;
        }

        static void Error(string message)
        {
            Console.WriteLine($"Error: {message}");
            ShowError = true;
        }

        #endregion

    }
}
