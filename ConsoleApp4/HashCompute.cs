using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CompareBySizeAndSHA246
{
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
            using (SHA256Managed hashAlgorithm = new SHA256Managed())
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
                        hashAlgorithm.TransformFinalBlock(oldBuffer, 0, oldBytesRead);
                    }
                    else
                    {
                        hashAlgorithm.TransformBlock(oldBuffer, 0, oldBytesRead, oldBuffer, 0);
                    }

                    //BackgroundWorker.ReportProgress((int)​((double)totalBytesRead * 100 / size));
                } while (bytesRead != 0);

                return BitConverter.ToString(hashAlgorithm.Hash).Replace("-", String.Empty);
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
