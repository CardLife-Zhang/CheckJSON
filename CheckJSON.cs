using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Net;
using System.Web;

namespace CheckJSON
{
    [DataContract]
    public class FileData
    {
        [DataMember]
        public string filename { get; set; }
        [DataMember]
        public string hash { get; set; }
    }
    [DataContract]
    public class FileInfoCollection
    {
        [DataMember]
        public string version { get; set; }
        [DataMember]
        public List<FileData> files { get; set; }
    }
    class CheckJSON
    {
        static void Main(string[] args)
        {
            string json;
            Console.WriteLine("CardLife JSON File Checker");

            //string json = CreateJson();
            string url = "https://raw.githubusercontent.com/CardLife-Zhang/CheckJSON/master/FileInfo.json";

            Console.WriteLine(" - Retreiving information on files");
            
            using (WebClient client = new WebClient())
            {
                json = client.DownloadString(url);
            }
            
            Console.WriteLine(" - Processing local files");

            FileInfoCollection filedat =  readdat(json);

            if (CheckFiles(filedat))
            {
                Console.WriteLine("File verification succeeded - your files appear to be correct");
            } else
            {
                Console.WriteLine("File verification failed - you will not be able to connect to servers.");
            }
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();
        }
        static string CreateJson()
        {
            var testdat = new FileInfoCollection();
            testdat.version = "00.00";
            testdat.files = new List<FileData> { };

            foreach (string line in File.ReadLines(@"c:\games\CardLife\CheckJSON.txt", Encoding.UTF8))
            {
                String[] strlist = line.Split(':');
                testdat.files.Add(new FileData { filename = strlist[0].Trim(), hash = strlist[1].Trim() });

            }

            //Convert object to JSON
            var stream1 = new MemoryStream();
            var ser = new DataContractJsonSerializer(typeof(FileInfoCollection));

            ser.WriteObject(stream1, testdat);
            stream1.Position = 0;
            var sr = new StreamReader(stream1);
            return sr.ReadToEnd();
        }

        static FileInfoCollection readdat(string json)
        {
            var FileInfoColObj = new FileInfoCollection();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var ser = new DataContractJsonSerializer(FileInfoColObj.GetType());
            FileInfoColObj = ser.ReadObject(ms) as FileInfoCollection;
            ms.Close();
            return FileInfoColObj;
        }

        static bool CheckFiles(FileInfoCollection filedat)
        {
            // Need to read in paths
            bool rc = true;
            string result;
            string folderpath = "GameData";
            string[] files = Directory.GetFiles(folderpath, "*json", SearchOption.AllDirectories);
            Array.Sort<string>(files, (string s1, string s2) => string.Compare(s1, s2, StringComparison.Ordinal));

            int fdidx = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string s = files[i].Substring(folderpath.Length + 1);
                // compare filenames here
                int comp = String.Compare(s, filedat.files[fdidx].filename, StringComparison.InvariantCulture);
                if (comp <0)
                {
                    //extra file
                    Console.WriteLine(String.Format("  ! Unexpected file {0} - this shouldn't be here.", s));
                    rc = false;
                } else if (comp >0)
                {
                    //missing file
                    Console.WriteLine(String.Format("  ! Missing file {0}", filedat.files[fdidx].filename));
                    // Stop the loop from moving forward
                    i--;
                    // Find the next file in our list
                    fdidx++;
                    rc = false;
                } else
                {
                    // The same, do comparison
                    MD5 md2 = MD5.Create();
                    byte[] namebytes = Encoding.UTF8.GetBytes(s);
                    md2.TransformBlock(namebytes, 0, namebytes.Length, namebytes, 0);
                    byte[] contentbytes = System.IO.File.ReadAllBytes(files[i]);
                    md2.TransformFinalBlock(contentbytes, 0, contentbytes.Length);
                    result = BitConverter.ToString(md2.Hash).Replace("-", "");
                    if (!result.Equals(filedat.files[fdidx].hash))
                    {
                        Console.WriteLine(String.Format(
                            "  ! File {0} has incorrect hash - {1} rather than {2}"), 
                            s, 
                            result,  
                            filedat.files[fdidx].hash);
                        rc = false;
                    }
                    fdidx++;
                }
            }
            return rc;
        }
    }
}
