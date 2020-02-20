using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Alfie_Host.Data
{
    static class GroupStorage
    {
        const string GroupsFile = "data\\groups.json";
        public static async Task Save(Dictionary<string, int> groups)
        {
            if (!Directory.Exists(Path.GetDirectoryName(Program.StartUpPath + GroupsFile)))
                Directory.CreateDirectory(Path.GetDirectoryName(Program.StartUpPath + GroupsFile));
            StreamWriter writer = new StreamWriter(Program.StartUpPath + GroupsFile);
            await writer.WriteAsync(JsonConvert.SerializeObject(groups, Formatting.Indented));
            writer.Close();
            writer.Dispose();
            return;
        }

        public static async Task<Dictionary<string, int>> Load()
        {
            if (!File.Exists(Program.StartUpPath + GroupsFile))
                return null;
            StreamReader reader = new StreamReader(Program.StartUpPath + GroupsFile);
            Dictionary<string, int> groups = JsonConvert.DeserializeObject<Dictionary<string, int>>(await reader.ReadToEndAsync());
            reader.Close();
            reader.Dispose();
            return groups;
        }
    }
}
