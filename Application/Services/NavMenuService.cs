using Domain.Entities.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{

    public class NavMenuService
    {
        public event Func<Task>? OnMenuChanged;

        private List<MenusList> _menuList = new();

        public List<MenusList> MenuList
        {
            get => _menuList;
            private set => _menuList = value ?? new List<MenusList>(); // ✅ never null
        }

        public void UpdateMenuList(List<MenusList> menuList)
        {
            MenuList = menuList ?? new List<MenusList>(); // ✅ null guard
            _ = NotifyMenuChanged();                      // ✅ fire-and-forget safely
        }

        private async Task NotifyMenuChanged()           // ✅ async Task not async void
        {
            if (OnMenuChanged == null) return;

            // Invoke each subscriber individually so one failure won't affect others
            foreach (var handler in OnMenuChanged.GetInvocationList().Cast<Func<Task>>())
            {
                try
                {
                    await handler.Invoke();              // ✅ each handler awaited safely
                }
                catch (Exception ex)
                {
                    // Log the error but don't crash other subscribers
                    Console.Error.WriteLine($"NavMenuService notification error: {ex.Message}");
                }
            }
        }
    }
}
