using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tracker.Models.Hideout;
using Tracker.Models.Items;
using Tracker.Models.Quests;
using Tracker.Models.Users;

namespace Tracker.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }
        public DbSet<Quest> Quests { get; set; }
        public DbSet<QuestRequirements> QuestRequirments { get; set; }
        public DbSet<QuestObjective> QuestObjectives { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleRequirement> ModuleRequirements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //TestParseItems(modelBuilder);
            //TestParseQuests(modelBuilder);

            User admin = new User("Brian13", "brian13@gmail.com", "123456");
            admin.Id = 1;
            admin.Role = "admin";
            UserInfo userInfo = admin.UserInfo!;
            userInfo.Id = 1;
            userInfo.UserId = admin.Id;
            admin.UserInfo = null;

            modelBuilder.Entity<User>().HasData(admin);
            modelBuilder.Entity<UserInfo>().HasData(userInfo);

            modelBuilder.Entity<UserInfo>()
                .HasMany(ui => ui.Stations)
                .WithMany(s => s.UserInfos)
                .UsingEntity<UserStation>(
                c =>
                {
                    c.HasOne(c => c.UserInfo)
                    .WithMany(ui => ui.UserStations)
                    .HasForeignKey(c => c.UserInfoId);

                    c.HasOne(c => c.Station)
                    .WithMany(s => s.UserStations)
                    .HasForeignKey(c => c.StationId);

                    c.Property(c => c.Level);
                    c.HasKey(c => new { c.UserInfoId, c.StationId });
                });

            modelBuilder.Entity<UserInfo>()
                .HasMany(ui => ui.Items)
                .WithMany(i => i.UserInfos)
                .UsingEntity<UserItem>(
                c =>
                {
                    c.HasOne(c => c.UserInfo)
                    .WithMany(ui => ui.UserItems)
                    .HasForeignKey(c => c.UserInfoId);

                    c.HasOne(c => c.Item)
                    .WithMany(s => s.UserItems)
                    .HasForeignKey(c => c.ItemId);

                    c.Property(c => c.RelateType);
                    c.Property(c => c.RelatedId);
                    c.Property(c => c.Quantity);
                    c.HasKey(c => new { c.UserInfoId, c.ItemId, c.RelateType, c.RelatedId });
                });
        }

        private void TestParseItems(ModelBuilder modelBuilder)
        {
            string testItems = """
                            {
                "5447a9cd4bdc2dbd208b4567": {
                    "id": "5447a9cd4bdc2dbd208b4567",
                    "name": "Colt M4A1 5.56x45 assault rifle",
                    "shortName": "M4A1"
                },
                "5447ac644bdc2d6c208b4567": {
                    "id": "5447ac644bdc2d6c208b4567",
                    "name": "5.56x45mm M855 ammo pack (50 pcs)",
                    "shortName": "M855"
                },
                "5448ba0b4bdc2d02308b456c": {
                    "id": "5448ba0b4bdc2d02308b456c",
                    "name": "Factory emergency exit key",
                    "shortName": "Factory"
                }
                }
                """;

            Dictionary<string, JsonElement> itemJsons = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(testItems)!;
            List<Item> items = new List<Item>();

            foreach (var itemJson in itemJsons)
            {
                Item item = JsonSerializer.Deserialize<Item>(itemJson.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                items.Add(item);
            }

            modelBuilder.Entity<Item>().HasData(items);
        }

        private void TestParseQuests(ModelBuilder modelBuilder)
        {
            string firstQuest = """
                              {
                  "id": 0,
                  "require": {
                    "level": 1,
                    "quests": []
                  },
                  "giver": 0,
                  "title": "Debut",
                  "wiki": "https://escapefromtarkov.fandom.com/wiki/Debut",
                  "objectives": [
                    {
                      "type": "kill",
                      "target": "Scavs",
                      "number": 5,
                      "location": -1,
                      "id": 0
                    },
                    {
                      "type": "collect",
                      "target": "54491c4f4bdc2db1078b4568",
                      "number": 2,
                      "location": -1,
                      "id": 1
                    }
                  ]
                }
                """;

            Quest quest = JsonSerializer.Deserialize<Quest>(firstQuest, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
            QuestRequirements requirments = quest.Requirements!;
            List<QuestObjective> objectives = quest.Objectives;
            requirments.QuestId = quest.Id;
            requirments.Id = 1;
            quest.Objectives = new();
            quest.Requirements = null;
            int objectiveId = 1;

            foreach (QuestObjective objective in objectives)
            {
                objective.QuestId = quest.Id;
                objective.Id = objectiveId++;
            }

            modelBuilder.Entity<Quest>().HasData(quest);
            modelBuilder.Entity<QuestRequirements>().HasData(requirments);
            modelBuilder.Entity<QuestObjective>().HasData(objectives);
        }
    }
}
