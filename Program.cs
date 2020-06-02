using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AviFavsPlus.Config;
using Newtonsoft.Json;
using VRCAPIdotNet.VRCAPI;
using VRCAPIdotNet.VRCAPI.Endpoints;
using VRCAPIdotNet.VRCAPI.Responses;
using static VRCAPIdotNet.VRCAPI.Dependencies;

namespace AviFav__Converter
{
    internal class Program
    {
        public static DirectoryInfo ownPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
        public static FileInfo json_path = ownPath.CombineFile("404Mods", "AviFavorites", "avatars.json");
        public static FileInfo txt_path = ownPath.CombineFile("avatars.txt");

        public static UserSelfRES selfRES = null;
        public static VRCAPIClient client = null;

        public static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            string username; string password;
            var loginFile = new FileInfo("login.txt");
            if (loginFile.Exists)
            {
                Console.WriteLine($"Reading login info from {loginFile.Name}");
                var lines = File.ReadAllLines(loginFile.FullName);
                username = lines[0]; password = lines[1];
            }
            else
            {
                Console.WriteLine("Username:");
                username = Console.ReadLine();
                Console.WriteLine();

                Console.WriteLine("Password:");
                password = Console.ReadLine();
                Console.WriteLine();
            }

            while (currentUser == null)
            {
                client = new VRCAPIClient(username, password);
                selfRES = await client.Auth.Login();

                if (isBanned)
                {
                    throw new Exception("Is banned");
                }

                if (inErrorState)
                {
                    await Task.Delay(3000);
                }
            }
            ConfigRES configRES = await client.Config.Get();

            if (!inErrorState)
            {
                if (!txt_path.Exists || !json_path.Exists) return;
                var txt_avis = txt_path.ReadAllLines().ToList().Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
                Console.WriteLine($"Read {txt_avis.Count} avatars from {txt_path.FullName.Quote()}");
                var json_avis = JsonConvert.DeserializeObject<List<SavedAvi>>(json_path.ReadAllText());
                Console.WriteLine($"Read {json_avis.Count} avatars from {json_path.FullName.Quote()}");
                foreach (var avi in txt_avis)
                {
                    try
                    {
                        var hasAvi = json_avis.Where(a => a.AvatarID == avi).FirstOrDefault();
                        if (hasAvi is null)
                        {
                            AvatarRES aRES = await client.Avatars.GetSingle(avi);
                            Console.WriteLine(aRES.ToJSON());
                            json_avis.Add(new SavedAvi() { Name = aRES.name, AvatarID = avi, ThumbnailImageUrl = aRES.thumbnailImageUrl });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        json_avis.Add(new SavedAvi() { Name = avi, AvatarID = avi, ThumbnailImageUrl = "" });
                        // Console.ReadLine();
                    }
                }
                json_avis = json_avis.OrderByDescending(a => a.Name).ToList();
                var json = JsonConvert.SerializeObject(json_avis, Formatting.Indented);
                Console.WriteLine(json);
                Console.ReadKey();
                json_path.WriteAllText(json);
            }
        }
    }
}