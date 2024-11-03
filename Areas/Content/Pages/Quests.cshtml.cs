using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Tracker.Models;
using Tracker.Models.Quests;

namespace Tracker.Areas.Content.Pages
{
    public class QuestsModel : PageModel
    {
        private ApplicationDbContext _db;
        private string? _userName;

        public QuestsModel(ApplicationDbContext db, IHttpContextAccessor contextAccessor)
        {
            _db = db;
            _userName = contextAccessor.HttpContext?.User.Identity?.Name;
        }

        //public async Task OnGetAsync()
        //{
        //    List<Quest> quests = await _db.Quests.Include(q => q.Objectives).ToListAsync();

        //    foreach (Quest quest in quests)
        //        foreach (QuestObjective objective in quest.Objectives)
        //            if (objective.Type == "locate")
        //                System.Diagnostics.Debug.WriteLine(objective.Number);

        //    List<string> objectives = new();

        //    foreach (Quest quest in quests)
        //        foreach (QuestObjective objective in quest.Objectives)
        //            if (objectives.Contains(objective.Type!) == false && objective.With.Count != 0 /*Regex.IsMatch(objective.Target!, @"^\w{24}")*/)
        //                objectives.Add(objective.Type!);
        //}

        public async Task<IActionResult> OnGetQuestsAsync()
        {
            UserInfo? userInfo = await GetUserInfoAsync();

            if (userInfo == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            List<Quest> availableQuests = await GetAvailableQuestsAsync(userInfo);
            return new JsonResult(new { success = true, quests = availableQuests }) { ContentType = "application/json" };
        }

        public async Task<IActionResult> OnPostQuestDoneAsync(int questId)
        {
            UserInfo? userInfo = await GetUserInfoAsync();
            Quest? quest = await _db.Quests.FirstOrDefaultAsync(q => q.Id == questId);

            if (userInfo == null || quest == null)
                return new JsonResult(new { success = false, redirectUrl = Url.Page("/Error") });

            userInfo.Quests.Add(quest);
            await _db.SaveChangesAsync();
            List<Quest> availableQuests = await GetAvailableQuestsAsync(userInfo);
            return new JsonResult(new { success = true, quests = availableQuests }) { ContentType = "application/json" };
        }

        private async Task<UserInfo?> GetUserInfoAsync()
        {
            User? user = await _db.Users.Include(u => u.UserInfo).ThenInclude(qi => qi!.Quests).FirstOrDefaultAsync(u => u.Name == _userName);
            return user?.UserInfo;
        }

        private async Task<List<Quest>> GetAvailableQuestsAsync(UserInfo userInfo)
        {
            List<int> complitedQuestIds = userInfo.Quests.Select(q => q.Id).ToList();
            List<Quest> availableQuests = await _db.Quests.Include(q => q.Requirements)
                .Where(q =>
                complitedQuestIds.Contains(q.Id) == false &&
                q.Requirements!.Level <= userInfo.Level &&
                q.Requirements!.Quests.Except(complitedQuestIds).Any() == false)
                .Include(q => q.Objectives)
                .ToListAsync();

            string[] types = { "collect", "find", "place", "build" };

            foreach (Quest quest in availableQuests)
            {
                foreach (QuestObjective objective in quest.Objectives)
                {
                    if (objective.Type == "mark")
                    {
                        Item? item = await _db.Items.FirstOrDefaultAsync(i => i.Id == objective.Tool);
                        objective.Tool = item?.ShortName;
                    }
                    else if (types.Contains(objective.Type) && Regex.IsMatch(objective.Target!, @"^\w{24}"))
                    {
                        Item? item = await _db.Items.FirstOrDefaultAsync(i => i.Id == objective.Target);
                        objective.Target = item?.ShortName;
                    }
                }
            }

            return availableQuests;
        }
    }
}
