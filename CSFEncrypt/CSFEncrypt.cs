using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CryptoZip;

namespace CSFEncrypt
{
    public partial class CSFEncrypt : Form
    {
        public const string IV_STRING = "DUEKGUAMZTCUAPQK";

        public CSFEncrypt()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = Environment.CurrentDirectory;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.ShowDialog();
            txtPath.Text = folderBrowserDialog.SelectedPath;
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            string path = txtPath.Text;
            if (Directory.Exists(path))
            {
                if (txtKey.Text != txtKeyVer.Text)
                {
                    MessageBox.Show("Passwords do not match!", "Augmentation Failure",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    byte[] key = DeriveBytes(txtKey.Text);
                    long csize = 0;
                    int fileCount = 0;
                    progressBar.Value = 0;

                    bool encrypt = rdEncrypt.Checked;
                    long totalSize = GetDirectorySize(path, encrypt ? "*.*" : "*.cz55s");
                    progressBar.Maximum = (int)totalSize;
                    new Thread(new ThreadStart(delegate
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            btnExecute.Enabled = false;
                        });
                        try
                        {
                            string[] files = Directory.GetFiles(path, encrypt ? "*.*" : "*.cz55s", SearchOption.AllDirectories);
                            foreach (string file in files)
                            {
                                FileInfo fi = new FileInfo(file);
                                fileCount++;
                                csize += fi.Length;

                                if (encrypt)
                                {
                                    CryptoZipLib.Zip(new FileInfo(file), key, Directory.GetParent(file).FullName,
                                        null);
                                    FileInfo encFile = new FileInfo(file + ".cz55s");
                                    string newPath = Path.Combine(encFile.Directory.FullName, Path.GetRandomFileName() + ".cz55s");
                                    encFile.MoveTo(newPath);
                                }
                                else
                                {
                                    CryptoZipLib.Unzip(new FileInfo(file), key);
                                }

                                BeginInvoke((MethodInvoker)delegate
                                {
                                    progressBar.Value = (int)csize;
                                    lblStatus.Text = fileCount + "/" + files.Length + " augmented, "
                                        + (100.0 * csize / totalSize) + "%\n" + fi.Name;
                                });
                                File.Delete(file);
                            }
                            BeginInvoke((MethodInvoker)delegate
                            {
                                lblStatus.Text = "Augmenting folder names...";
                            });
                            var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories)
                            .OrderByDescending(
                                p => p.Count(c => c == Path.DirectorySeparatorChar
                                || c == Path.AltDirectorySeparatorChar));

                            foreach (string directory in directories)
                            {
                                DirectoryInfo dinfo = new DirectoryInfo(directory);
                                if (encrypt)
                                {
                                    byte[] bytes = EncryptStringToBytes_Aes(dinfo.Name, key,
                                        Encoding.ASCII.GetBytes(IV_STRING));
                                    string newName = Convert.ToBase64String(bytes
                                        ).Replace("/", "-");
                                    string newPath = Path.Combine(dinfo.Parent.FullName, newName);
                                    dinfo.MoveTo(newPath);
                                }
                                else
                                {
                                    byte[] bytes = Convert.FromBase64String(dinfo.Name.Replace("-", "/"));
                                    string newName = DecryptStringFromBytes_Aes(
                                        bytes,
                                        key,
                                        Encoding.ASCII.GetBytes(IV_STRING));
                                    string newPath = Path.Combine(dinfo.Parent.FullName, newName);
                                    dinfo.MoveTo(newPath);
                                }
                            }
                            BeginInvoke((MethodInvoker)delegate
                            {
                                lblStatus.Text = "Operations completed!";
                            });
                            MessageBox.Show("Augmentation complete!", "Augmentation Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("An error occurred during augmentation:\n" + ex.Message, "Augmentation Failure",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        BeginInvoke((MethodInvoker)delegate
                        {
                            btnExecute.Enabled = true;
                        });
                    })).Start();


                }
            }
            else
            {
                MessageBox.Show("Directory does not exist!", "Directory Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        static readonly byte[] SALT = { 1, 2, 23, 234, 38, 48, 134, 63, 248, 4 };

        public byte[] DeriveBytes(string pass)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(pass, SALT))
            {
                byte[] key = deriveBytes.GetBytes(32);
                return key;
            }
        }


        static long GetDirectorySize(string p, string match)
        {
            string[] a = Directory.GetFiles(p, match, SearchOption.AllDirectories);
            long b = 0;
            foreach (string name in a)
            {
                FileInfo info = new FileInfo(name);
                b += info.Length;
            }
            return b;
        }
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }


            // Return the encrypted bytes from the memory stream.
            return encrypted;

        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }
    }
}
