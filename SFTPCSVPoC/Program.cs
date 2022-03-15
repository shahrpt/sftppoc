using Renci.SshNet;
using System;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace SFTPCSVPoC
{
    class Program
    {
        static string host = ConfigurationManager.AppSettings.Get("Host");
        static string username = ConfigurationManager.AppSettings.Get("Username");
        static string password = ConfigurationManager.AppSettings.Get("Pass");
        static string workingdirectory = ConfigurationManager.AppSettings.Get("WorkingDirectory");
        static int Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Port"));
        static string uploadfile = @"D:\Projects\SFTP\tsv_on_the_server.txt";

        //1) Read data from a database table into a dataTable
        static string data = File.ReadAllText(@"D:\Projects\SFTP\db_data.json");
        static void Main(string[] args)
        {
            var data = File.ReadAllText(@"D:\Projects\SFTP\db_data.json");
            DataTable dataTable = (DataTable)JsonConvert.DeserializeObject(data, (typeof(DataTable)));

            UploadDataTableUsingAppendByBlock(dataTable, 1);
            UploadDataTableUsingAppend(dataTable);

            //UploadTSVMemoryStreamInc();

            //UploadCompleteTSV();
            

        }
        public static void UploadDataTableUsingAppendByBlock(DataTable dataTable, int blockSize = 1) //size in Mbs
        {
            blockSize *= 1024; //1024 * 1024;  //1 MB size
            int total = 0;
            int num = 0;

            using (var sftpClient = new SftpClient(host, Port, username, password))
            {
                sftpClient.Connect();
                StringBuilder fileContent = new StringBuilder();
                using (StreamWriter writer = sftpClient.AppendText(workingdirectory + "TestFile.txt"))
                {
                    foreach (var col in dataTable.Columns)
                    {
                        fileContent.Append(col.ToString() + "\t");
                    }

                    fileContent.Replace("\t", System.Environment.NewLine, fileContent.Length - 1, 1);
                    // Add size of newlines
                    //total += Environment.NewLine.Length;

                    string line = fileContent.ToString();
                    
                    int length = line.Length;

                    if (total + length >= blockSize)
                    {
                        // write the block size
                        writer.Write(fileContent.ToString());
                        num++;
                        total = 0;
                        fileContent.Clear();
                    }

                    else total += length;

                    foreach (DataRow dr in dataTable.Rows)
                    {
                        foreach (var column in dr.ItemArray)
                        {
                            fileContent.Append("\"" + column.ToString() + "\"\t");
                        }
                        fileContent.Replace("\t", System.Environment.NewLine, fileContent.Length - 1, 1);
                        line = fileContent.ToString();
                        
                        length = line.Length;
                        
                        // Add size of newlines
                        //total += Environment.NewLine.Length;

                        if (total + length >= blockSize)
                        {
                            // write the block size
                            writer.Write(fileContent.ToString());
                            num++;
                            total = 0;
                            fileContent.Clear();
                            Console.WriteLine("Block {num} is pushed");
                        }
                        // Add length of line in bytes to running size
                        else total += length;
                    }
                    if(fileContent.ToString().Length>0)
                    {
                        writer.Write(fileContent.ToString());
                        num++;
                        total = 0;
                        Console.WriteLine("Block {num} is pushed");
                    }
                }
            }
        }
        static void UploadDataTableUsingAppend(DataTable dataTable)
        {

            using (var sftpClient = new SftpClient(host, Port, username, password))
            {
                sftpClient.Connect();
                StringBuilder fileContent = new StringBuilder();
                using (StreamWriter writer = sftpClient.AppendText(workingdirectory + "TestFile.txt"))
                {
                    foreach (var col in dataTable.Columns)
                    {
                        fileContent.Append(col.ToString() + "\t");
                    }

                    fileContent.Replace("\t", System.Environment.NewLine, fileContent.Length - 1, 1);
                    writer.WriteLine(fileContent.ToString());
                    
                    fileContent.Clear();

                    foreach (DataRow dr in dataTable.Rows)
                    {
                        foreach (var column in dr.ItemArray)
                        {
                            fileContent.Append("\"" + column.ToString() + "\"\t");
                        }

                        fileContent.Replace("\t", System.Environment.NewLine, fileContent.Length - 1, 1);
                        writer.WriteLine(fileContent.ToString());
                        fileContent.Clear();
                    }
                }
            }
        }
        static void UploadCompleteTSV()
        {
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

        static void UploadTSVMemoryStreamInc()
        {

            DataTable dataTable = (DataTable)JsonConvert.DeserializeObject(data, (typeof(DataTable)));

            //2) Format into TSV format, not writing it
            var tsvContent = dataTable.WriteToTSFFormat();
            Console.WriteLine("Creating client and connecting");
            var fileName = Path.GetFileName(uploadfile);
            var fileNameWOExt = Path.GetFileNameWithoutExtension(fileName);
            using (var sftpClient = new SftpClient(host, Port, username, password))
            {
                sftpClient.Connect();
                Console.WriteLine("Connected to {0}", host);

                sftpClient.BufferSize = 4 * 1024; // bypass Payload error large files
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] bytes = Encoding.ASCII.GetBytes("ABCD");
                    for (int i = 0; i < 5; i++) //test five liness
                    {
                        string random = new Random().Next(1000, 9000).ToString();
                        bytes = Encoding.ASCII.GetBytes(random);
                        ms.Write(bytes, 0, bytes.Length);
                        Console.WriteLine("Position is: " + ms.Position);
                        // Reset the pointer
                        //ms.Seek(0, SeekOrigin.Begin);
                        ms.Position = 0;
                        Console.WriteLine("uploading stream");
                        //3) Upload the code to SFTP server
                        sftpClient.UploadFile(ms, fileName, canOverride:true);
                        var selectedFiles = sftpClient.ListDirectory(workingdirectory).Where(file=>file.Name.ToLower().StartsWith(fileNameWOExt.ToLower()));
                       
                    }
                    bytes = Encoding.ASCII.GetBytes("EFGH");

                    ms.Write(bytes, 0, bytes.Length);
                    ms.Position = 0;
                    sftpClient.UploadFile(ms, Path.GetFileName(uploadfile));


                    bytes = Encoding.ASCII.GetBytes("EFGH");

                    ms.Write(bytes, 0, bytes.Length);
                    ms.Position = 0;
                    sftpClient.UploadFile(ms, Path.GetFileName(uploadfile));
                    Console.WriteLine("Upload successful");
                }

            }
        }
    }
}
