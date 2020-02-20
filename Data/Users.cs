using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using UserData = System.Collections.Generic.Dictionary<string, object>;

namespace Alfie_Host.Data
{
    static class UserStorage
    {
        const string UsersFolder = "data\\users";
        const string Extention = ".json";
        
        public static async Task Create(ulong id)
        {
            if (File.Exists(Program.StartUpPath + UsersFolder + "\\" + id + Extention))
                return;
            if (!Directory.Exists(Program.StartUpPath + UsersFolder))
                Directory.CreateDirectory(Program.StartUpPath + UsersFolder);
            var data = new UserData();
            StreamWriter writer = new StreamWriter(Program.StartUpPath + UsersFolder + "\\" + id + Extention);
            await writer.WriteAsync(JsonConvert.SerializeObject(data, Formatting.Indented));
            writer.Close();
            writer.Dispose();
            return;
        }

        public static bool Exists(ulong id)
            => File.Exists(Program.StartUpPath + UsersFolder + "\\" + id + Extention);
        
        public static async Task<UserData> GetData(ulong id)
        {
            if (!File.Exists(Program.StartUpPath + UsersFolder + "\\" + id + Extention))
                return null;
            StreamReader reader = new StreamReader(Program.StartUpPath + UsersFolder + "\\" + id + Extention);
            var data = JsonConvert.DeserializeObject<UserData>(await reader.ReadToEndAsync());
            reader.Close();
            reader.Dispose();
            return data;
        }

        public static async Task Save(ulong id, UserData data)
        {
            if (!Directory.Exists(Program.StartUpPath + UsersFolder))
                Directory.CreateDirectory(Program.StartUpPath + UsersFolder);
            StreamWriter writer = new StreamWriter(Program.StartUpPath + UsersFolder + "\\" + id + Extention);
            await writer.WriteAsync(JsonConvert.SerializeObject(data, Formatting.Indented));
            writer.Close();
            writer.Dispose();
            return;
        }
    }
}
