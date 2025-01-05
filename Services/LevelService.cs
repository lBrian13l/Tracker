using Microsoft.EntityFrameworkCore;
using Tracker.Models;
using Tracker.Models.Users;

namespace Tracker.Services
{
    public class LevelService
    {
        private const int _maxLevel = 75;

        private string? _userName;
        private ApplicationDbContext _db;
        private User? _user;

        public LevelService(IHttpContextAccessor contextAccessor, ApplicationDbContext db)
        {
            _db = db;
            _userName = contextAccessor.HttpContext?.User.Identity?.Name;
        }

        public async Task<int?> GetLevelAsync()
        {
            User? user = await GetUserAsync();
            return user?.UserInfo?.Level;
        }

        public async Task Increase() => await ChangeLevel(1);

        public async Task Decrease() => await ChangeLevel(-1);

        private async Task ChangeLevel(int value)
        {
            User? user = await GetUserAsync();

            if (user?.UserInfo != null)
            {
                UserInfo userInfo = user.UserInfo;
                userInfo.Level += value;
                userInfo.Level = Math.Clamp(userInfo.Level, 1, _maxLevel);

                await _db.SaveChangesAsync();
            }
        }

        private async Task<User?> GetUserAsync()
        {
            if (_user == null)
                _user = await _db.Users.Include(u => u.UserInfo).FirstOrDefaultAsync(u => u.Name == _userName);

            return _user;
        }
    }
}
