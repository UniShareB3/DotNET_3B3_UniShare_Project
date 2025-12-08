using Backend.Features.Items.Enums;
using Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationContext context)
        {
            // Asigură-te că baza de date este creată
            context.Database.Migrate();

            // Verificăm dacă există deja iteme
            if (context.Items.Any()) return;

            // Creăm un user test pentru ownership
            var owner = context.Users.FirstOrDefault();
            if (owner == null)
            {
                owner = new User
                {
                    Email = "test.user@example.com",
                    UserName = "testuser",
                    FirstName = "Test",
                    LastName = "User",
                    EmailConfirmed = true,
                };
                context.Users.Add(owner);
                context.SaveChanges();
            }

            // Lista de iteme
            var items = new List<Item>
            {
                new Item { Name = "Laptop", Description = "High-end gaming laptop", Category = ItemCategory.Electronics, Condition = ItemCondition.Excellent, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=1" },
                new Item { Name = "Textbook", Description = "Math textbook", Category = ItemCategory.Books, Condition = ItemCondition.Good, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=2" },
                new Item { Name = "Backpack", Description = "Durable backpack", Category = ItemCategory.Accessories, Condition = ItemCondition.Excellent, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=3" },
                new Item { Name = "Smartphone", Description = "Latest model smartphone", Category = ItemCategory.Electronics, Condition = ItemCondition.Excellent, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=4" },
                new Item { Name = "Desk Chair", Description = "Comfortable office chair", Category = ItemCategory.Others, Condition = ItemCondition.Good, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=5" },
                new Item { Name = "Monitor", Description = "27-inch 4K monitor", Category = ItemCategory.Electronics, Condition = ItemCondition.Excellent, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=6" },
                new Item { Name = "Coffee Mug", Description = "Ceramic mug", Category = ItemCategory.Kitchen, Condition = ItemCondition.Good, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=7" },
                new Item { Name = "Headphones", Description = "Noise-cancelling headphones", Category = ItemCategory.Electronics, Condition = ItemCondition.Excellent, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=8" },
                new Item { Name = "Notebook", Description = "College-ruled notebook", Category = ItemCategory.Others, Condition = ItemCondition.Good, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=9" },
                new Item { Name = "Lamp", Description = "Desk lamp", Category = ItemCategory.Others, Condition = ItemCondition.Excellent, OwnerId = owner.Id, ImageUrl = "https://picsum.photos/200?random=10" }
            };

            context.Items.AddRange(items);
            context.SaveChanges();
        }
    }
}