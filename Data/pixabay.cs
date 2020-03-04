using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Alfie_Host.Data
{
    public static class Pixabay
    {
        const string KeyFile = "data\\pixabay\\key.txt";

        public static async Task<string> GetKey()
        {
            if (!File.Exists(Program.StartUpPath + KeyFile))
            {
                if (!Directory.Exists(Path.GetDirectoryName(Program.StartUpPath + KeyFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(Program.StartUpPath + KeyFile));
                StreamWriter writer = new StreamWriter(Program.StartUpPath + KeyFile);
                await writer.WriteAsync("API key goes here");
                writer.Close();
                writer.Dispose();
                return "nothing";
            }
                
            StreamReader reader = new StreamReader(Program.StartUpPath + KeyFile);
            string result = await reader.ReadToEndAsync();
            reader.Close();
            reader.Dispose();
            return result;
        }
    }
}
