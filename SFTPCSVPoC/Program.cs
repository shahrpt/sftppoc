using Renci.SshNet;
using System;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Newtonsoft.Json;

namespace SFTPCSVPoC
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = ConfigurationManager.AppSettings.Get("Host");
            string username = ConfigurationManager.AppSettings.Get("Username");
            string password = ConfigurationManager.AppSettings.Get("Pass");
            string workingdirectory = ConfigurationManager.AppSettings.Get("WorkingDirectory");
            int Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Port"));
            string uploadfile = @"D:\Projects\SFTP\tsv_on_the_server.txt";

            //1) Read data from a database table into a dataTable
            var data = File.ReadAllText(@"D:\Projects\SFTP\db_data.json");
            DataTable dataTable = (DataTable)JsonConvert.DeserializeObject(data, (typeof(DataTable)));

            //2) Format into TSV format, not writing it
            var tsvContent = dataTable.WriteToTSFFormat();
            Console.WriteLine("Creating client and connecting");
            using (var sftpClient = new SftpClient(host, Port, username, password))
            {
                sftpClient.Connect();
                Console.WriteLine("Connected to {0}", host);

                sftpClient.BufferSize = 4 * 1024; // bypass Payload error large files
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(tsvContent);
                    ms.Write(bytes, 0, bytes.Length);
                    Console.WriteLine("Position is: " + ms.Position);
                    // Reset the pointer
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.Position = 0;
                    Console.WriteLine("uploading stream");
                    //3) Upload the code to SFTP server
                    sftpClient.UploadFile(ms, Path.GetFileName(uploadfile));
                    Console.WriteLine("Upload successful");
                }

            }

        }
    }
}
