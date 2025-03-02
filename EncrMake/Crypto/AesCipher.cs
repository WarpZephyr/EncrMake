using System;
using System.Security.Cryptography;

namespace EncrMake.Crypto
{
    public class AesCipher : IDisposable
    {
        private readonly Aes Aes;
        private readonly ICryptoTransform Decryptor;
        private readonly ICryptoTransform Encryptor;
        private bool disposedValue;

        public AesCipher(byte[] key, byte[] iv, CipherMode mode, PaddingMode paddingMode, int keySize, int blockSize)
        {
            Aes = Aes.Create();
            Aes.Mode = mode;
            Aes.Padding = paddingMode;
            Aes.KeySize = keySize;
            Aes.BlockSize = blockSize;

            Encryptor = Aes.CreateEncryptor(key, iv);
            Decryptor = Aes.CreateDecryptor(key, iv);
        }

        public byte[] Decrypt(byte[] input)
            => Decryptor.TransformFinalBlock(input, 0, input.Length);

        public byte[] Encrypt(byte[] input)
            => Encryptor.TransformFinalBlock(input, 0, input.Length);

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Decryptor.Dispose();
                    Encryptor.Dispose();
                    Aes.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
