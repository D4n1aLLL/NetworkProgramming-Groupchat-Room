using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace AsyncClient
{
    class ChatMessage
    {
        public string Text { get; set; }
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public bool FileAttached { get; set; }
        public UInt16 From { get; set; }
        private const string privatekey = "mysecurekey!";

        public byte[] Message { get; set; }
        public ChatMessage(UInt16 from, string text, string fileName, byte[] fileBytes, bool fileAttached)
        {
            From = from;
            Text = text;
            FileName = fileName;
            FileBytes = fileBytes;
            FileAttached = fileAttached;
        }

        public ChatMessage(byte[] message)
        {
            Message = message;
        }
        public byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(From));
            bytes.AddRange(BitConverter.GetBytes(Text.Length));
            bytes.AddRange(Encoding.ASCII.GetBytes(Text));
            bytes.AddRange(BitConverter.GetBytes(FileAttached));
            if (FileAttached)
            {
                bytes.AddRange(BitConverter.GetBytes(FileName.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(FileName));
                bytes.AddRange(BitConverter.GetBytes(FileBytes.Length));
                bytes.AddRange(FileBytes);
            }

            byte[] data = bytes.ToArray();
            byte[] encrypted = Encrypt(data, Encoding.ASCII.GetBytes(privatekey), data.Length);
            return encrypted;
        }


        public void ToMessage(int rec)
        {
            byte[] temp = Decrypt(Message, Encoding.ASCII.GetBytes(privatekey), rec);
            Array.Clear(Message, 0, Message.Length);
            Message = temp;
            int traversedBytes = 0;
            From = BitConverter.ToUInt16(Message, traversedBytes);
            traversedBytes += 2;
            int txtLength = BitConverter.ToInt32(Message, traversedBytes);
            traversedBytes += 4;
            this.Text = Encoding.ASCII.GetString(Message, traversedBytes, txtLength);
            traversedBytes += txtLength;
            this.FileAttached = BitConverter.ToBoolean(Message, traversedBytes);
            traversedBytes += 1;
            if (FileAttached)
            {
                int fileNameLength = BitConverter.ToInt32(Message, traversedBytes);
                traversedBytes += 4;
                this.FileName = Encoding.ASCII.GetString(Message, traversedBytes, fileNameLength);
                traversedBytes += fileNameLength;
                int fileLength = BitConverter.ToInt32(Message, traversedBytes);
                traversedBytes += 4;
                FileBytes = new byte[fileLength];
                Buffer.BlockCopy(Message, traversedBytes, FileBytes, 0, fileLength);
                traversedBytes += fileLength;
            }
        }


        private byte[] Encrypt(byte[] plainBytes, byte[] key, int rec)
        {
            MD5 md5 = MD5.Create();
            TripleDES des = TripleDES.Create();
            byte[] kb = md5.ComputeHash(key);
            Array.Resize(ref kb, 24);
            des.Key = kb;
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.Zeros;
            ICryptoTransform trans = des.CreateEncryptor();
            byte[] encrypt = trans.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return encrypt;
        }

        private byte[] Decrypt(byte[] encrypt, byte[] key, int rec)
        {
            Array.Resize(ref encrypt, rec);
            MD5 md5 = MD5.Create();
            TripleDES des = TripleDES.Create();
            byte[] kb = md5.ComputeHash(key);
            Array.Resize(ref kb, 24);
            des.Key = kb;
            des.Mode = CipherMode.ECB;
            des.Padding = PaddingMode.Zeros;
            ICryptoTransform tras = des.CreateDecryptor();
            return tras.TransformFinalBlock(encrypt, 0, encrypt.Length);
        }

    }
}
