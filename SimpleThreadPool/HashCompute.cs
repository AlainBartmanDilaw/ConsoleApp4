using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CompareBySizeAndSHA246
{

    class Algorithm : IDisposable
    {
        public virtual byte[] Hash1 { get { return sha1.Hash; } }
        public virtual byte[] Hash256 { get { return sha256.Hash; } }

        SHA256Managed sha256;
        SHA1Managed sha1;
        public Algorithm()
        {
            sha256 = new SHA256Managed();
            sha1 = new SHA1Managed();
        }
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            sha1.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
            return sha256.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            sha1.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
            return sha256.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
        }

        public void Dispose()
        {
            sha1.Dispose();
            sha256.Dispose();
        }
    }

    class HashCompute
    {
        public static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public static string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        public static string GetSHA256(string file)
        {
            long size;
            byte[] buffer;
            byte[] oldBuffer;
            int bytesRead;
            int oldBytesRead;
            long totalBytesRead = 0;

            using (FileStream stream = File.OpenRead(file))
            using (Algorithm algorithm = new Algorithm())
            //using (SHA256Managed hashAlgorithm = new SHA256Managed())
            //using (SHA1Managed hashAlgorithmSHA1 = new SHA1Managed())
            {
                size = stream.Length;
                buffer = new byte[4096];
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                totalBytesRead += bytesRead;

                do
                {
                    oldBytesRead = bytesRead;
                    oldBuffer = buffer;
                    buffer = new byte[4096];
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    totalBytesRead += bytesRead;
                    if (bytesRead == 0)
                    {
                        //hashAlgorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                        //hashAlgorithmSHA1.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                        algorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                    }
                    else
                    {
                        //hashAlgorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
                        //hashAlgorithmSHA1.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
                        algorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
                    }

                    //BackgroundWorker.ReportProgress((int)​((double)totalBytesRead * 100 / size));
                } while (bytesRead != 0);

                String sha256 = BitConverter.ToString(algorithm.Hash256).Replace("-", String.Empty);
                String sha1 = BitConverter.ToString(algorithm.Hash1).Replace("-", String.Empty);
                Console.WriteLine("{0}", file);
                Console.WriteLine("SHA256 : {0}", sha256);
                Console.WriteLine("SHA1   : {0}", sha1);

                return sha256;
            }
        }

        public static string GetChecksumBuffered(Stream stream)
        {
            using (var bufferedStream = new BufferedStream(stream, 1024 * 32))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(bufferedStream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }
    }
}
