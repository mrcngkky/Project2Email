using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project2EmailNight.Context;
using Project2EmailNight.Entities;

namespace Project2EmailNight.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            // İstatistikler
            var totalMessages = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email || x.SenderEmail == user.Email)
                .CountAsync();

            var inboxCount = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email && !x.IsDraft)
                .CountAsync();

            var sentCount = await _context.Messages
                .Where(x => x.SenderEmail == user.Email && !x.IsDraft)
                .CountAsync();

            var unreadCount = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email && !x.IsStatus && !x.IsDraft)
                .CountAsync();

            var draftCount = await _context.Messages
                .Where(x => x.SenderEmail == user.Email && x.IsDraft)
                .CountAsync();

            var starredCount = await _context.Messages
                .Where(x => (x.ReceiverEmail == user.Email || x.SenderEmail == user.Email) && x.IsStarred)
                .CountAsync();

            // Kategoriye göre sayılar
            var categoryStats = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email || x.SenderEmail == user.Email)
                .GroupBy(x => x.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            // Son aktiviteler (son 5 mesaj)
            var recentMessages = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email)
                .OrderByDescending(x => x.SendDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalMessages = totalMessages;
            ViewBag.InboxCount = inboxCount;
            ViewBag.SentCount = sentCount;
            ViewBag.UnreadCount = unreadCount;
            ViewBag.DraftCount = draftCount;
            ViewBag.StarredCount = starredCount;
            ViewBag.CategoryStats = categoryStats;
            ViewBag.RecentMessages = recentMessages;

            return View();
        }
    }
}