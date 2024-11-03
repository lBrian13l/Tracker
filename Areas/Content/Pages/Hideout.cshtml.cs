using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tracker.Models;
using Tracker.Models.Hideout;

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
            User? user = await _db.Users.Include(u => u.UserInfo)
                .ThenInclude(ui => ui!.Stations)
                .ThenInclude(s => s!.Modules)
                .ThenInclude(m => m!.Requirements)
                .FirstOrDefaultAsync(u => u.Name == _userName);
            UserInfo? userInfo = user?.UserInfo;
            List<Station> stations = await _db.Stations.ToListAsync();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            JsonArray userStations = new JsonArray();

            foreach (Station station in stations)
            {
                if (userInfo.Stations.Contains(station) == false)
                {
                    userInfo.Stations.Add(station);
                    await _db.SaveChangesAsync();
                }

                bool maxLevel = false;
                bool isAvailable = true;
                UserInfoStationCross cross = userInfo.StationCrosses.FirstOrDefault(c => c.StationId == station.Id)!;
                Module? nextModule = station.Modules.FirstOrDefault(m => m.Level == cross.Level + 1);

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

                        }
                    }
                }
                else
                {
                    maxLevel = true;
                }

                JsonNode? node = JsonSerializer.SerializeToNode(station, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                JsonObject? userStation = node?.AsObject();

                if (userStation != null)
                {
                    userStation["level"] = cross.Level;
                    userStation["maxLevel"] = maxLevel;
                    userStation["isAvailable"] = isAvailable;
                    userStations.Add(userStation);
                }
            }

            return new JsonResult(new { success = true, stations = userStations }) { ContentType = "application/json" };
        }
    }
}
