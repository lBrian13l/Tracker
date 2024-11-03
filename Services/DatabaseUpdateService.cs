using System.Text.Json;
using System.Text.Json.Nodes;
using Tracker.Models;
using Tracker.Models.Hideout;
using Tracker.Models.Quests;

namespace Tracker.Services
{
    public class DatabaseUpdateService
    {
        private ApplicationDbContext _db;
        private string _hideoutPath = "https://raw.githubusercontent.com/TarkovTracker/tarkovdata/master/hideout.json";
        private string _itemsPath = "https://raw.githubusercontent.com/TarkovTracker/tarkovdata/master/items.en.json";
        private string _questsPath = "https://raw.githubusercontent.com/TarkovTracker/tarkovdata/master/quests.json";

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
                await _db.Stations.AddRangeAsync(stations);

            string itemsJson = await client.GetStringAsync(_itemsPath);
            List<Item> items = DeserializeItems(itemsJson);
            await _db.Items.AddRangeAsync(items);

            string questsJson = await client.GetStringAsync(_questsPath);
            List<Quest> quests = await DeserializeQuests(questsJson);
            await _db.Quests.AddRangeAsync(quests);

            _db.SaveChanges();
        }

        private List<Station>? DeserializeHideout(string hideoutJson)
        {
            Dictionary<string, JsonArray> hideoutJsons = JsonSerializer.Deserialize<Dictionary<string, JsonArray>>(hideoutJson)!;

            if (hideoutJsons.ContainsKey("stations") == false || hideoutJsons.ContainsKey("modules") == false)
                return null;

            foreach (JsonObject? station in hideoutJsons["stations"])
                station!["name"] = station?["locales"]?["en"]?.DeepClone();

            List<Station> stations = JsonSerializer.Deserialize<List<Station>>(hideoutJsons["stations"], new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;

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

            List<Module> modules = JsonSerializer.Deserialize<List<Module>>(hideoutJsons["modules"], new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;

            foreach (Module module in modules)
            {
                Station? station = stations.FirstOrDefault(s => s.Id == module.StationId);
                station?.Modules?.Add(module);
            }

            return stations;
        }

        private List<Item> DeserializeItems(string itemsJson)
        {
            Dictionary<string, Item> itemsDictionary = JsonSerializer.Deserialize<Dictionary<string, Item>>(itemsJson, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
            List<Item> itemsList = new List<Item>();

            foreach (var item in itemsDictionary)
                itemsList.Add(item.Value);

            return itemsList;
        }

        private async Task<List<Quest>> DeserializeQuests(string questsJson)
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

                bool cont = false;
                ProblemQuest problemQuest = new ProblemQuest();

                if (questJson.ContainsKey("require"))
                {
                    JsonObject? require = questJson["require"]!.AsObject();

                    if (require.ContainsKey("quests"))
                    {
                        JsonArray? questsRequire = require["quests"]!.AsArray();

                        //foreach (JsonNode? questRequire in questsRequire)
                        //{
                        //    if (questRequire!.GetValueKind() == JsonValueKind.Array)
                        //    {
                        //        foreach (JsonNode? q in questRequire.AsArray())
                        //            problemQuest.Values.Add(q.ToString());

                        //        problemQuest.FieldName = "require";
                        //        cont = true;
                        //    }
                        //}

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
                {
                    //problemQuest.FieldName = "level";
                    //cont = true;

                    questJson["require"]!["level"] = 1;
                }

                for (int i = objectives.Count - 1; i >= 0; i--)
                    if (objectives[i]!["type"]!.ToString() == "key")
                        objectives.RemoveAt(i);

                foreach (JsonObject? objective in objectives)
                {
                    if (objective!.ContainsKey("target") && objective["target"]!.GetValueKind() == JsonValueKind.Number)
                        objective["target"] = objective["target"]!.ToString();
                    //else if (objective.ContainsKey("target") && objective["target"]!.GetValueKind() != JsonValueKind.String)
                    //{
                    //    foreach (JsonNode? item in objective["target"]!.AsArray())
                    //        problemQuest.Values.Add(item?.ToString()!);

                    //    problemQuest.FieldName = "target";
                    //    cont = true;
                    //}

                    if (objective.ContainsKey("with") && objective["with"]![0]!.GetValueKind() == JsonValueKind.Object)
                    {
                        objective["with"]!.AsArray().Clear();

                        //foreach (JsonNode? item in objective["with"]!.AsArray())
                        //{
                        //    if (item!.GetValueKind() == JsonValueKind.Object && item.AsObject().ContainsKey("name"))
                        //    {
                        //        problemQuest.Values.Add(item!["name"]!.ToString());
                        //        problemQuest.FieldName = "with";
                        //        cont = true;
                        //    }
                        //}
                    }
                }
                Console.WriteLine($"_________________ID: {questJson["id"]!.ToString()}");
                if (cont)
                {
                    problemQuest.Name = questJson["title"]!.ToString();
                    await _db.Problems.AddAsync(problemQuest);
                    await _db.SaveChangesAsync();
                    continue;
                }

                Quest quest = JsonSerializer.Deserialize<Quest>(questJson, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
                quests.Add(quest);
            }

            //List<int> ids = new List<int>();
            //List<Quest> problemQuests = new List<Quest>();

            //foreach (Quest quest in quests)
            //{
            //    foreach (QuestObjective objective in quest.Objectives)
            //    {
            //        if (!ids.Contains(objective.Id))
            //            ids.Add(objective.Id);
            //        else
            //        {
            //            ProblemQuest problemQuest = new ProblemQuest();
            //            problemQuest.Name = quest.Title;
            //            problemQuest.FieldName = "objectiveID";
            //            problemQuest.Values.Add(objective.Id.ToString());
            //            _db.Problems.Add(problemQuest);
            //            _db.SaveChanges();
            //            problemQuests.Add(quest);
            //            break;
            //        }
            //    }
            //}

            //foreach (Quest quest in problemQuests)
            //{
            //    quests.Remove(quest);
            //}

            return quests;
        }
    }
}
