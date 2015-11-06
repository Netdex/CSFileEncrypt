using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Encryption;

namespace CryptoZip
{
    public static class CryptoZipLib
    {
        public const string CRYPTOZIP_EXTENSION = ".cz55s";
        public const string IV_STRING = "DUEKGUAMZTCUAPQK"; //TODO change this
        public const string COMPRESSION_ALGORITHM = "Zip64";
        public const string ENCRYPTION_ALGORITHM = "AES 256-bit (Rijndael)";

        public static void Zip(FileInfo file, byte[] key, PercentChangedEventHandler percentChangedCallback)
        {
            Zip(file, key, file.DirectoryName, percentChangedCallback);
        }

        public static void Zip(FileInfo file, byte[] key, string outputFolder, PercentChangedEventHandler percentChangedCallback)
        {
            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = file.DirectoryName;

            FileStream inputStream = File.OpenRead(file.FullName);
            FileStream outputStream = File.OpenWrite(outputFolder + "\\" + file.Name + CRYPTOZIP_EXTENSION);
            
            Aes aes = new AesManaged();
            byte[] keyBytes = key;
            byte[] ivBytes = Encoding.UTF8.GetBytes(IV_STRING);

            using (CryptoStream cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(keyBytes, ivBytes), CryptoStreamMode.Write))
            {
                using (ZipOutputStream zipOutputStream = new ZipOutputStream(cryptoStream))
                {
                    zipOutputStream.SetLevel(0);
                    zipOutputStream.UseZip64 = UseZip64.On;
                    ZipEntry zipEntry = new ZipEntry(file.Name);
                    zipEntry.DateTime = DateTime.Now;

                    zipOutputStream.PutNextEntry(zipEntry);
                    int read = 0;
                    byte[] buffer = new byte[4096];
                    double totalRead = 0;
                    double lastPercent = 0;

                    do
                    {
                        read = inputStream.Read(buffer, 0, buffer.Length);
                        zipOutputStream.Write(buffer, 0, read);

                        totalRead += read;
                        double percent = Math.Floor((totalRead / inputStream.Length) * 100);

                        if (percent > lastPercent)
                        {
                            lastPercent = percent;

                            if (percentChangedCallback != null)
                                percentChangedCallback(new PercentChangedEventArgs(percent));
                        }

                    } while (read == buffer.Length);

                    zipOutputStream.Finish();
                    zipOutputStream.Flush();
                }
            }
            inputStream.Close();
            outputStream.Close();
        }

        public static void Unzip(FileInfo file, byte[] key)
        {
            Unzip(file, key, file.DirectoryName);
        }

        public static void Unzip(FileInfo file, byte[] key, string outputFolder)
        {
            FileStream inputStream = File.OpenRead(file.FullName);

            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = file.DirectoryName;


            Aes aes = new AesManaged();
            byte[] keyBytes = key;
            byte[] ivBytes = Encoding.UTF8.GetBytes(IV_STRING);

            using (CryptoStream cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(keyBytes, ivBytes), CryptoStreamMode.Read))
            {
                using (ZipInputStream zipInputStream = new ZipInputStream(cryptoStream))
                {
                    ZipEntry zipEntry = zipInputStream.GetNextEntry();
                    FileStream outputStream = File.OpenWrite(outputFolder + "\\" + zipEntry.Name);
                    int read = 0;
                    byte[] buffer = new byte[65536];

                    do
                    {
                        read = zipInputStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, read);
                    } while (read > 0);
                    try
                    {
                        zipInputStream.Close();
                    }
                    catch
                    {
                    }
                    
                    outputStream.Flush();
                    outputStream.Close();
                }
            }
            inputStream.Close();
        }
    }

    public delegate void PercentChangedEventHandler(PercentChangedEventArgs args);

    public class PercentChangedEventArgs : EventArgs
    {
        public double PercentCompleted { get; set; }

        public PercentChangedEventArgs() { }

        public PercentChangedEventArgs(double percentCompleted)
        {
            PercentCompleted = percentCompleted;
        }
    }
}
