using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tracker.Models;
using Tracker.Models.Hideout;
using Tracker.Models.Items;
using Tracker.Models.Quests;
using Tracker.Models.Users;

namespace Tracker.Services
{
    public class DatabaseUpdateService
    {
        private ApplicationDbContext _db;
        private string _hideoutPath = "https://raw.githubusercontent.com/TarkovTracker/tarkovdata/master/hideout.json";
        private string _itemsPath = "https://raw.githubusercontent.com/TarkovTracker/tarkovdata/master/items.en.json";
        private string _questsPath = "https://raw.githubusercontent.com/TarkovTracker/tarkovdata/master/quests.json";
        private JsonSerializerOptions _serializerOptions = new() { PropertyNameCaseInsensitive = true };

        public DatabaseUpdateService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task UpdateDatabase()
        {
            HttpClient client = new HttpClient();

            string hideoutJson = await client.GetStringAsync(_hideoutPath);
            List<Station>? stations = DeserializeHideout(hideoutJson);

            if (stations != null)
            {
                await _db.Stations.AddRangeAsync(stations);
                List<UserInfo> userInfos = await _db.UserInfos.ToListAsync();

                foreach (UserInfo userInfo in userInfos)
                {
                    foreach (Station station in stations)
                    {
                        if (station.Name == "Stash")
                            userInfo.UserStations.Add(new UserStation { Station = station, Level = 1 });
                        else
                            userInfo.Stations.Add(station);
                    }
                }
            }

            string itemsJson = await client.GetStringAsync(_itemsPath);
            List<Item> items = DeserializeItems(itemsJson);
            await _db.Items.AddRangeAsync(items);

            string questsJson = await client.GetStringAsync(_questsPath);
            List<Quest> quests = DeserializeQuests(questsJson);
            await _db.Quests.AddRangeAsync(quests);

            _db.SaveChanges();
        }

        private List<Station>? DeserializeHideout(string hideoutJson)
        {
            Dictionary<string, JsonArray> hideoutJsons = JsonSerializer.Deserialize<Dictionary<string, JsonArray>>(hideoutJson)!;

            if (hideoutJsons.ContainsKey("stations") == false || hideoutJsons.ContainsKey("modules") == false)
                return null;

            foreach (JsonObject? station in hideoutJsons["stations"])
            {
                station!["name"] = station?["locales"]?["en"]?.DeepClone();
                string oldName = station!["name"]!.ToString();
                string newName = oldName.Substring(0, 1).ToUpper() + oldName.Substring(1).ToLower();
                station!["name"] = newName;
            }

            List<Station> stations = JsonSerializer.Deserialize<List<Station>>(hideoutJsons["stations"], _serializerOptions)!;

            foreach (JsonObject? module in hideoutJsons["modules"])
            {
                if (module!.ContainsKey("require"))
                {
                    JsonArray requirements = module["require"]!.AsArray();

                    for (int i = requirements.Count - 1; i >= 0; i--)
                        if (requirements[i]!["type"]?.ToString() == "trader" || requirements[i]!["type"]?.ToString() == "skill")
                            requirements.RemoveAt(i);
                }
            }

            List<Module> modules = JsonSerializer.Deserialize<List<Module>>(hideoutJsons["modules"], _serializerOptions)!;

            foreach (Module module in modules)
            {
                Station? station = stations.FirstOrDefault(s => s.Id == module.StationId);
                station?.Modules?.Add(module);
            }

            return stations;
        }

        private List<Item> DeserializeItems(string itemsJson)
        {
            Dictionary<string, Item> itemsDictionary =
                JsonSerializer.Deserialize<Dictionary<string, Item>>(itemsJson, _serializerOptions)!;

            List<Item> itemsList = new List<Item>();

            foreach (var item in itemsDictionary)
                itemsList.Add(item.Value);

            return itemsList;
        }

        private List<Quest> DeserializeQuests(string questsJson)
        {
            List<JsonObject> questJsons = JsonSerializer.Deserialize<List<JsonObject>>(questsJson)!;
            List<Quest> quests = new List<Quest>();
            JsonArray? objectives = null;

            foreach (JsonObject questJson in questJsons)
            {
                if (questJson.ContainsKey("objectives"))
                    objectives = questJson["objectives"]!.AsArray();

                if (objectives == null)
                    continue;

                if (questJson.ContainsKey("require"))
                {
                    JsonObject? require = questJson["require"]!.AsObject();

                    if (require.ContainsKey("quests"))
                    {
                        JsonArray? questsRequire = require["quests"]!.AsArray();

                        for (int i = questsRequire.Count - 1; i >= 0; i--)
                        {
                            if (questsRequire[i]!.GetValueKind() == JsonValueKind.Array)
                            {
                                foreach (JsonNode? node in questsRequire[i]!.AsArray())
                                    questsRequire.Add(node!.DeepClone());

                                questsRequire.RemoveAt(i);
                            }
                        }
                    }
                }

                if (questJson["require"]!["level"] == null)
                    questJson["require"]!["level"] = 1;

                for (int i = objectives.Count - 1; i >= 0; i--)
                    if (objectives[i]!["type"]!.ToString() == "key")
                        objectives.RemoveAt(i);

                foreach (JsonObject? objective in objectives)
                {
                    if (objective!["type"]!.ToString() == "reputation" &&
                        objective!.ContainsKey("target") && objective["target"]!.GetValueKind() == JsonValueKind.Number)
                        objective["target"] = ((Trader)(int)objective["target"]!).ToString();

                    if (objective.ContainsKey("with") && objective["with"]![0]!.GetValueKind() == JsonValueKind.Object)
                        objective["with"]!.AsArray().Clear();
                }

                Quest quest = JsonSerializer.Deserialize<Quest>(questJson, _serializerOptions)!;
                quests.Add(quest);
            }

            return quests;
        }
    }
}
