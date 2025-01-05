using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Tracker.Models;
using Tracker.Models.Hideout;
using Tracker.Models.Items;
using Tracker.Models.Quests;
using Tracker.Models.Users;

namespace Tracker.Areas.Content.Pages
{
    public class ItemsModel : PageModel
    {
        private ApplicationDbContext _db;
        private string? _userName;

        public ItemsModel(ApplicationDbContext db, IHttpContextAccessor contextAccessor)
        {
            _db = db;
            _userName = contextAccessor.HttpContext?.User.Identity?.Name;
        }

        public async Task<IActionResult> OnGetItemsAsync()
        {
            UserInfo? userInfo = await GetUserInfoAsync();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            List<ItemDto> requiredItems = await GetRequiredItems(userInfo);
            return new JsonResult(new { success = true, items = requiredItems }) { ContentType = "application/json" };
        }

        public async Task<IActionResult> OnPostAddItem([FromBody] ItemDto item)
        {
            UserInfo? userInfo = await GetUserInfoAsync();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            UserItem? userItem = userInfo.UserItems.FirstOrDefault(ui => ui.RelatedId == item.RelatedId &&
                ui.RelateType == item.RelateType && ui.ItemId == item.Id);

            if (userItem == null)
            {
                userItem = new UserItem()
                {
                    RelateType = item.RelateType,
                    RelatedId = item.RelatedId,
                    Quantity = 1,
                    ItemId = item.Id
                };

                userInfo.UserItems.Add(userItem);
            }
            else
            {
                int? maxQuantityNullable;

                if (item.RelateType == RelateType.Quest)
                {
                    QuestObjective? objective = await _db.QuestObjectives.FindAsync(item.RelatedId);
                    maxQuantityNullable = objective?.Number;
                }
                else
                {
                    ModuleRequirement? requirement = await _db.ModuleRequirements.FindAsync(item.RelatedId);
                    maxQuantityNullable = requirement?.Quantity;
                }

                int maxQuantity = maxQuantityNullable ?? 0;
                userItem.Quantity = Math.Min(userItem.Quantity + 1, maxQuantity);
            }

            await _db.SaveChangesAsync();
            return new JsonResult(new { success = true, quantity = userItem.Quantity}) { ContentType = "application/json" };
        }

        public async Task<IActionResult> OnPostRemoveItem([FromBody] ItemDto item)
        {
            UserInfo? userInfo = await GetUserInfoAsync();

            if (userInfo != null)
            {
                UserItem? userItem = userInfo.UserItems.FirstOrDefault(ui => ui.RelatedId == item.RelatedId &&
                    ui.RelateType == item.RelateType && ui.ItemId == item.Id);

                if (userItem != null)
                {
                    userItem.Quantity = Math.Max(userItem.Quantity - 1, 0);
                    await _db.SaveChangesAsync();
                    return new JsonResult(new { success = true, quantity = userItem.Quantity }) { ContentType = "application/json" };
                }
            }

            return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });
        }

        private async Task<UserInfo?> GetUserInfoAsync()
        {
            User? user = await _db.Users.Include(u => u.UserInfo).ThenInclude(ui => ui!.UserItems)
                .Include(u => u.UserInfo).ThenInclude(ui => ui!.Quests)
                .Include(u => u.UserInfo).ThenInclude(ui => ui!.UserStations)
                .FirstOrDefaultAsync(u => u.Name == _userName);

            return user?.UserInfo;
        }

        private async Task<List<ItemDto>> GetRequiredItems(UserInfo userInfo)
        {
            List<UserItem> userItems = userInfo.UserItems;
            Dictionary<int, int> userStationLevels = userInfo.UserStations.ToDictionary(us => us.StationId, us => us.Level);
            List<int> completedQuestIds = userInfo.Quests.Select(q => q.Id).ToList();
            List<ItemDto> hideoutItems = await GetHideoutItems(userStationLevels);
            List<ItemDto> questItems = await GetQuestItems(completedQuestIds);
            List<ItemDto> allItems = hideoutItems.Concat(questItems).ToList();

            foreach (ItemDto item in allItems)
            {
                UserItem? userItem = userItems.FirstOrDefault(ui => ui.RelatedId == item.RelatedId &&
                    ui.RelateType == item.RelateType && ui.ItemId == item.Id);

                if (userItem != null)
                    item.Quantity = userItem.Quantity;
            }

            return allItems;
        }

        private async Task<List<ItemDto>> GetHideoutItems(Dictionary<int, int> userStationLevels)
        {
            List<ModuleRequirement> moduleRequirements = await _db.ModuleRequirements.Include(mr => mr.Module)
                .Where(mr => mr.Type == "item").ToListAsync();

            List<ItemDto> hideoutItems = moduleRequirements
                .Where(mr => userStationLevels[mr.Module.StationId] < mr.Module.Level)
                .Select(mr => new ItemDto
                {
                    Id = mr.Name!,
                    ShortName = _db.Items.FirstOrDefault(i => i.Id == mr.Name)!.ShortName,
                    RelateType = RelateType.Hideout,
                    RelatedId = mr.Id,
                    RelatedName = mr.Module.Name + $" {mr.Module.Level}",
                    RequiredQuantity = mr.Quantity
                })
                .ToList();

            return hideoutItems;
        }

        private async Task<List<ItemDto>> GetQuestItems(List<int> completedQuestIds)
        {
            List<ItemDto> questItems = await _db.QuestObjectives
                .Where(qo => completedQuestIds.Contains(qo.QuestId) == false && (qo.Type == "find" || qo.Type == "collect"))
                .Select(qo => new ItemDto
                {
                    Id = qo.Target!,
                    ShortName = _db.Items.FirstOrDefault(i => i.Id == qo.Target)!.ShortName,
                    RelateType = RelateType.Quest,
                    RelatedId = qo.Id,
                    RelatedName = _db.Quests.FirstOrDefault(q => q.Id == qo.QuestId)!.Title,
                    RequiredQuantity = qo.Number,
                    FoundInRaid = qo.Type == "find"
                })
                .ToListAsync();

            return questItems;
        }
    }
}
