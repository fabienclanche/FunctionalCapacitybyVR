using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System;
using TestSuite;

namespace Utils
{
    public class JSONSerializer
    {
        static Type[] knownTypes = { typeof(Vector3), typeof(Vector4), typeof(Quaternion) };

        public static T FromJSON<T>(string json)
        {
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(json));
            T @object = FromJSON<T>(stream);
            stream.Close();

            return @object;
        }

        public static T FromJSON<T>(Stream json)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), knownTypes);
            return (T)serializer.ReadObject(json);
        }

        private static string DumpMem(MemoryStream mem)
        {
            mem.Position = 0;
            StreamReader sr = new StreamReader(mem);
            string content = sr.ReadToEnd();
            sr.Close();
            mem.Close();
            return content;
        }

        public static string ToJSON<T>(T @object)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), knownTypes);
            MemoryStream mem = new MemoryStream();
            serializer.WriteObject(mem, @object);

            return DumpMem(mem);
        }

        public static MemoryStream ToJSONStream<T>(T @object)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T), knownTypes);
            MemoryStream mem = new MemoryStream();
            serializer.WriteObject(mem, @object);
            mem.Position = 0;

            return mem;
        }

        public static T FromJSONFile<T>(string filename)
        {
            StreamReader reader = FileReader(filename);
            T @object = FromJSON<T>(reader.BaseStream);
            reader.Close();
            return @object;
        }

        /// <summary>
        /// Creates the parent directory for a given path. For instance, if given a file path, this function will create the parent directory for that file
        /// </summary>
        /// <param name="filepath">A path to a file</param>
        public static void MkDirParent(string filepath)
        {
            Directory.CreateDirectory(Directory.GetParent(Path(filepath)).ToString());
        }

        public static StreamWriter FileWriter(string filepath, bool append = false)
        {
            return new StreamWriter(Path(filepath), append);
        }

        public static bool FileExists(string filepath)
        {
            return File.Exists(Path(filepath));
        }

        public static void DeleteFile(string filepath)
        {
            File.Delete(Path(filepath));
        }

        public static StreamReader FileReader(string filepath)
        {
            return new StreamReader(Path(filepath));
        }

        public static string Path(string filepath)
        {
            return filepath.Replace("/", "\\").Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        public static void ToJSONFile<T>(string filename, T @object)
        {
            StreamWriter writer = FileWriter(filename, false);

            var outputDir = Config.OutputDirectory;
            var lastBackslash = filename.LastIndexOf('\\');
             Debug.Log("...\\" + ((lastBackslash != -1) ? filename.Substring(lastBackslash) : filename) + " written");

            string json = ToJSON<T>(@object);

            StringBuilder buffer = new StringBuilder();
            char last = ' ';
            int indent = 0;

            Action writeBuffer = () =>
            {
                for (int i = 0; i < indent; i++) writer.Write("\t");
                writer.WriteLine(buffer.ToString());
                buffer.Clear();
            };

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == ']')
                {
                    writeBuffer();
                    indent--;
                }

                buffer.Append(c);

                if (c == '[')
                {
                    writeBuffer();
                    indent++;
                }
                else if (c == ',' && last == '}')
                {
                    writeBuffer();
                }
                else if (c == ',' && last == ']')
                {
                    writeBuffer();
                }

                last = c;
            }

            writeBuffer();
            writer.Flush();
            writer.Close();
        }
    }
}