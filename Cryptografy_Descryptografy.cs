using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;


namespace teste
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
            
            // Declare CspParmeters and RsaCryptoServiceProvider
            //objects with global scope of your Form class.

            CspParameters cspp = new CspParameters();
            RSACryptoServiceProvider rsa;

            //Path variables for source, encryption , and
            //decryption folders. Must end with a backslash.

            const string EncrFolder = @"c:\Encrypt\";
            const string Decrfolder = @"c:\Decrypt\";
            const string SrcFolder = @"c:\docs\";

            //Public key fle
            const string PubKeyFile = @"c:\encrypt\rsaPublicKey.txt";

            //key container name for
            //private/public key value pair.
            const string KeyName = "Key01";   
        
        private void ButtonCreateAsmKeys_Click(object sender, EventArgs e)
        {
            cspp.KeyContainerName = KeyName;
            rsa = new RSACryptoServiceProvider(cspp);

            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)        
                label1.Text = "Key: " + cspp.KeyContainerName + " - Public Only";           
            else          
                label1.Text = "Key: " + cspp.KeyContainerName + " - Full Key Pair";           
        }
        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void buttonEncryptFile_Click(object sender, EventArgs e)
        {
            if (rsa == null)
                MessageBox.Show("key not set.");
            else
            {
                //Display a dialog Box to select a file encrypt.
                openFileDialog1.InitialDirectory = SrcFolder;
                if(openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string fName = openFileDialog1.FileName;
                    if(fName != null)
                    {
                        FileInfo fInfo = new FileInfo(fName);
                        //Pass the file name without the path.
                        string name = fInfo.FullName;
                        EncryptFile(name);
                    }
                }
            }
        }
        private void EncryptFile(String inFile)
        {
            //create instance of rinjdael for
            //symetric encryption of the data.

            RijndaelManaged rjnd1 = new RijndaelManaged();
            rjnd1.KeySize = 256;
            rjnd1.BlockSize = 256;
            rjnd1.Mode = CipherMode.CBC;
            ICryptoTransform transform = rjnd1.CreateDecryptor();

            //Use RSACryptoServiceProvider to
            //encrypt the Rihndael Key.
            // rsa is previously instantiaded:
            //rsa = new RSACryptoServiceProvider(cspp);
            byte[] KeyEncrypted = rsa.Encrypt(rjnd1.Key, false);

            //create byte Arrays to contain
            //the length values of the key and IV.
            byte[] Lenk = new byte[6];
            byte[] LenIV = new byte[6];

            int lKey = KeyEncrypted.Length;
            Lenk = BitConverter.GetBytes(lKey);
            int lIV = rjnd1.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);


            //Write the following to the FileStream
            //for the Encrypted file(outFs)
            // - length of the Key
            // - length of the IV
            // - encrypted key
            // - the IV
            // - the encrypted cipher content

            int startFileName = inFile.LastIndexOf("\\") + 1;
            //change the file's extension to ".enc"
            string outFile = EncrFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".enc";
            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {
                outFs.Write(Lenk, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(KeyEncrypted, 0, lKey);
                outFs.Write(rjnd1.IV, 0, lIV);


                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {

                    //By encryption a chunk at
                    // a time, you can save memory
                    //and accommodate large files.

                    int count = 0;
                    int offset = 0;

                    //blockSizeBytes can be any arbitrary size.
                    int blockSizeBytes = rjnd1.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    using(FileStream inFs = new FileStream(inFile, FileMode.Open))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        } while (count > 0);
                        inFs.Close();
                    }
                    try
                    {

                        outStreamEncrypted.FlushFinalBlock();
                        outStreamEncrypted.Close();
                    }
                    catch(System.Security.Cryptography.CryptographicException e)
                    {
                        MessageBox.Show("Erro na Criptografia.");
                    }
                    
                }
                outFs.Close();
            }
        }


        private void OpenFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }
        private void OpenFileDialog2_FileOk(object sender, CancelEventArgs e)
        {

        }
        private void buttonDecryptFile_Click(object sender, EventArgs e)
        {
            if (rsa == null)
                MessageBox.Show("key not set");
            else
            {
                //Display a dialog box to select the encrypted File.
                openFileDialog2.InitialDirectory = EncrFolder;
                if(openFileDialog2.ShowDialog() == DialogResult.OK)
                {
                    string fName = openFileDialog2.FileName;
                    if(fName != null)
                    {
                        FileInfo fi = new FileInfo(fName);
                        string name = fi.Name;
                        DecryptFile(name);
                    }
                }
            }
        }

        private void DecryptFile(String inFile)
        {
            RijndaelManaged rjnd1 = new RijndaelManaged();
            rjnd1.KeySize = 256;
            rjnd1.BlockSize = 256;
            rjnd1.Mode = CipherMode.CBC;

            //Create byte arrays to get the length of
            // the encrypted key and IV.
            //These values were stored as 4 bytes each
            // at the beginning of the encrypted package.
            byte[] Lenk = new byte[4];
            byte[] LenIV = new byte[4];

            //Construct the file name for the decrypted file
            string outFile = Decrfolder + inFile.Substring(0, inFile.LastIndexOf(".")) + ".txt";

            //Use FileStream objects to read the encrypted
            //file(inFs) and save the decrypted file(outfs).
            using(FileStream inFs = new FileStream(EncrFolder + inFile, FileMode.Open))
            {
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(Lenk, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                //Convert the lengths to integer values.
                int lenk = BitConverter.ToInt32(Lenk, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                //Determine the start position of
                //the cipher text (startC)
                //ans its length (lenc)
                int startC = lenk + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                byte[] KeyEncrypted = new byte[lenk];
                byte[] IV = new byte[lenIV];

                //Extract the Key and IV
                //starting from index 8
                // after the length values.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenk);
                inFs.Seek(8 + lenk, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);
                Directory.CreateDirectory(Decrfolder);

                //Use RSACryptoServiceProvider
                //to Decrypt the Rinjdael Key.
                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                //Decrypt the key
                ICryptoTransform transform = rjnd1.CreateDecryptor(KeyDecrypted, IV);

                //Decrypt the cipher text form
                //from the FileStream of the encrypted
                //file (inFs) into the FileStream
                //for the decrypted file (outFs).
                using(FileStream outFs = new FileStream(outFile, FileMode.Create))
                {
                    int count = 0;
                    int offset = 0;

                    //blockSizeBytes can he any arbitrary size.
                    int blockSizeBytes = rjnd1.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];

                    //By decrypting a chunk a time,
                    //you can save memory and
                    // accommodate large files.

                    //Start at the beginning
                    //of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using(CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamDecrypted.Write(data, 0, count);
                        } while (count > 0);

                        outStreamDecrypted.FlushFinalBlock();
                        outStreamDecrypted.Close();
                    }
                    outFs.Close();
                }
                inFs.Close();
            }
        }

        void buttonExportPublicKey_Click(object sender, EventArgs e)
        {
            //Save the public key created by the RSA
            //to a file. Caution, persisting the
            //key to a file is a secutiry risk.

            Directory.CreateDirectory(EncrFolder);
            StreamWriter sw = new StreamWriter(PubKeyFile, false);
            sw.Write(rsa.ToXmlString(false));
            sw.Close();
        }

        private void buttonImportPublicKey_Click(object sender, EventArgs e)
        {
            StreamReader sr = new StreamReader(PubKeyFile);
            cspp.KeyContainerName = KeyName;
            rsa = new RSACryptoServiceProvider(cspp);
            string KeyTxt = sr.ReadToEnd();
            rsa.FromXmlString(KeyTxt);
            rsa.PersistKeyInCsp = true;
            if (rsa.PublicOnly == true)
                label1.Text = "Key: " + cspp.KeyContainerName + " - Public Only";
            else
                label1.Text = "Key: " + cspp.KeyContainerName + "- Full key Pair";
            sr.Close();
        }

        private void buttonGetPrivateKey_Click(object sender, EventArgs e)
        {
            cspp.KeyContainerName = KeyName;

            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;

            if (rsa.PublicOnly == true)
                label1.Text = "Key: " + cspp.KeyContainerName + "- Public Only";
            else
                label1.Text = "Key: " + cspp.KeyContainerName + " - Full Key Pair";
        }
    }
}
