using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tracker.Models;
using Tracker.Models.Hideout;
using Tracker.Models.Items;
using Tracker.Models.Users;

namespace Tracker.Areas.Content.Pages
{
    public class HideoutModel : PageModel
    {
        private ApplicationDbContext _db;
        private string? _userName;

        public HideoutModel(ApplicationDbContext db, IHttpContextAccessor contextAccessor)
        {
            _db = db;
            _userName = contextAccessor.HttpContext?.User.Identity?.Name;
        }

        public async Task<IActionResult> OnGetStationsAsync()
        {
            UserInfo? userInfo = await GetUserInfo();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            JsonArray userStations = await GetUserStations(userInfo);
            return new JsonResult(new { success = true, stations = userStations }) { ContentType = "application/json" };
        }

        public async Task<IActionResult> OnPostDowngradeStationAsync([FromBody] int stationId)
        {
            UserInfo? userInfo = await GetUserInfo();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            UserStation userStation = userInfo.UserStations.FirstOrDefault(c => c.StationId == stationId)!;
            Station station = userInfo.Stations.FirstOrDefault(s => s.Id == stationId)!;

            if ((station.Name == "Stash" && userStation.Level > 1) || userStation.Level > 0)
            {
                userStation.Level--;
                await _db.SaveChangesAsync();
            }

            JsonArray userStations = await GetUserStations(userInfo);
            return new JsonResult(new { success = true, stations = userStations }) { ContentType = "application/json" };
        }

        public async Task<IActionResult> OnPostUpgradeStationAsync([FromBody] int stationId)
        {
            UserInfo? userInfo = await GetUserInfo();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            UserStation userStation = userInfo.UserStations.FirstOrDefault(c => c.StationId == stationId)!;
            Station station = userInfo.Stations.FirstOrDefault(s => s.Id == stationId)!;

            if (userStation.Level < station.Modules.Count)
            {
                userStation.Level++;
                await _db.SaveChangesAsync();
            }

            JsonArray userStations = await GetUserStations(userInfo);
            return new JsonResult(new { success = true, stations = userStations }) { ContentType = "application/json" };
        }

        private async Task<UserInfo?> GetUserInfo()
        {
            User? user = await _db.Users.Include(u => u.UserInfo).ThenInclude(ui => ui!.UserStations)
                .Include(u => u.UserInfo)
                .ThenInclude(ui => ui!.Stations)
                .ThenInclude(s => s!.Modules)
                .ThenInclude(m => m!.Requirements)
                .FirstOrDefaultAsync(u => u.Name == _userName);

            return user?.UserInfo;
        }

        private async Task<JsonArray> GetUserStations(UserInfo userInfo)
        {
            List<Station> stations = await _db.Stations.ToListAsync();
            JsonArray userStations = new JsonArray();

            foreach (Station station in stations)
            {
                bool maxLevel = false;
                bool isAvailable = true;
                UserStation userStation = userInfo.UserStations.FirstOrDefault(c => c.StationId == station.Id)!;
                Module? nextModule = station.Modules.FirstOrDefault(m => m.Level == userStation.Level + 1);

                if (nextModule != null)
                {
                    for (int i = nextModule.Requirements.Count - 1; i >= 0; i--)
                    {
                        ModuleRequirement requirement = nextModule.Requirements[i];
                        Item? item = null;

                        if (requirement.Type == "item")
                        {
                            item = await _db.Items.FindAsync(requirement.Name);
                            requirement.Name = item?.ShortName;
                        }
                        else
                        {
                            Station requiredStation = userInfo.Stations.FirstOrDefault(s => s.Name == requirement.Name)!;

                            if (requiredStation == null)
                                continue;

                            UserStation requiredUserStation = userInfo.UserStations
                                .FirstOrDefault(c => c.StationId == requiredStation.Id)!;

                            if (requiredUserStation.Level < requirement.Quantity)
                                isAvailable = false;
                            else
                                nextModule.Requirements.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    maxLevel = true;
                }

                JsonNode? node = JsonSerializer.SerializeToNode(station,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                JsonObject? userStationJson = node?.AsObject();

                if (userStationJson != null)
                {
                    userStationJson["level"] = userStation.Level;
                    userStationJson["maxLevel"] = maxLevel;
                    userStationJson["isAvailable"] = isAvailable;
                    userStations.Add(userStationJson);
                }
            }

            return userStations;
        }
    }
}
