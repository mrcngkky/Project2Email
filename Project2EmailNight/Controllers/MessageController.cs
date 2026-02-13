using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project2EmailNight.Context;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;

namespace Project2EmailNight.Controllers
{
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MessageController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. GELEN KUTUSU (PAGINATION + FİLTRELEME EKLENDİ)
        [HttpGet]
        public async Task<IActionResult> Inbox(string search, int page = 1, string filter = "all")
        {
            int pageSize = 50; // Sayfa başına 50 mesaj

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var query = _context.Messages
                .Include(x => x.Attachments)
                .Where(x => x.ReceiverEmail == user.Email && !x.IsDraft)
                .AsQueryable();

            // Filtreleme
            switch (filter)
            {
                case "unread":
                    query = query.Where(x => !x.IsStatus);
                    break;
                case "starred":
                    query = query.Where(x => x.IsStarred);
                    break;
                case "snoozed":
                    query = query.Where(x => x.SnoozeUntil.HasValue && x.SnoozeUntil > DateTime.Now);
                    break;
                case "important":
                    query = query.Where(x => x.IsImportant);
                    break;
                case "drafts":
                    query = _context.Messages
                        .Include(x => x.Attachments)
                        .Where(x => x.SenderEmail == user.Email && x.IsDraft)
                        .AsQueryable();
                    break;
            }

            // Arama
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Subject.Contains(search) ||
                                        x.MessageDetail.Contains(search) ||
                                        x.SenderEmail.Contains(search));
            }

            // Toplam sayfa sayısı
            int totalMessages = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalMessages / (double)pageSize);

            // Pagination
            var messages = await query
                .OrderByDescending(x => x.SendDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ViewBag'e bilgileri ekle
            ViewBag.SearchTerm = search;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalMessages = totalMessages;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentFilter = filter;
            ViewBag.UnreadCount = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email && !x.IsStatus && !x.IsDraft)
                .CountAsync();

            return View(messages);
        }

        // 2. GİDEN KUTUSU (ARAMA ÖZELLİĞİ EKLENDİ)
        [HttpGet]
        public async Task<IActionResult> Sendbox(string search)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            var query = _context.Messages
                .Include(x => x.Attachments)
                .Where(x => x.SenderEmail == user.Email && !x.IsDraft);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.Subject.Contains(search) ||
                                         x.MessageDetail.Contains(search) ||
                                         x.ReceiverEmail.Contains(search));
            }

            var values = await query.OrderByDescending(x => x.SendDate).ToListAsync();

            ViewBag.SearchTerm = search;
            ViewBag.CurrentFilter = "sent";

            return View("Inbox", values);
        }

        // 3. YENİ MESAJ GÖNDERME (DOSYA DESTEĞİ EKLENDİ)
        [HttpPost]
        public async Task<IActionResult> SendMessage(string ReceiverEmail, string Subject, string MessageDetail, List<IFormFile> Attachments, int? DraftId)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                Message message;

                if (DraftId.HasValue)
                {
                    // Taslaktan gönder
                    message = await _context.Messages
                        .Include(m => m.Attachments)
                        .FirstOrDefaultAsync(x => x.MessageId == DraftId.Value);

                    if (message == null) return RedirectToAction("Inbox");

                    message.IsDraft = false;
                    message.SendDate = DateTime.Now;
                    message.ReceiverEmail = ReceiverEmail;
                    message.Subject = Subject;
                    message.MessageDetail = MessageDetail;
                }
                else
                {
                    // Yeni mesaj
                    message = new Message
                    {
                        SenderEmail = user.Email,
                        ReceiverEmail = ReceiverEmail,
                        Subject = Subject,
                        MessageDetail = MessageDetail,
                        SendDate = DateTime.Now,
                        IsDraft = false,
                        IsStatus = false,
                        IsStarred = false,
                        Category = DetectCategory(Subject)
                    };
                    _context.Messages.Add(message);
                }

                // Dosyaları kaydet
                if (Attachments != null && Attachments.Count > 0)
                {
                    foreach (var file in Attachments)
                    {
                        if (file.Length > 0 && file.Length <= 10 * 1024 * 1024) // Max 10MB
                        {
                            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                            var uploadPath = Path.Combine("wwwroot/uploads/attachments", user.Id);
                            Directory.CreateDirectory(uploadPath);

                            var filePath = Path.Combine(uploadPath, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            message.Attachments.Add(new MessageAttachment
                            {
                                FileName = Path.GetFileName(file.FileName),
                                FilePath = $"/uploads/attachments/{user.Id}/{fileName}",
                                FileSize = file.Length,
                                ContentType = file.ContentType,
                                UploadedDate = DateTime.Now
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Sendbox");
            }
            catch
            {
                TempData["Error"] = "Mesaj gönderilemedi.";
                return RedirectToAction("Inbox");
            }
        }

        // 4. TASLAK KAYDETME (YENİ - AJAX)
        [HttpPost]
        public async Task<IActionResult> SaveDraft(string ReceiverEmail, string Subject, string MessageDetail, int? DraftId, List<IFormFile> Attachments)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(User.Identity.Name);
                Message draft;

                if (DraftId.HasValue)
                {
                    // Mevcut taslağı güncelle
                    draft = await _context.Messages
                        .Include(m => m.Attachments)
                        .FirstOrDefaultAsync(x => x.MessageId == DraftId.Value);

                    if (draft == null) return Json(new { success = false });

                    draft.ReceiverEmail = ReceiverEmail;
                    draft.Subject = Subject;
                    draft.MessageDetail = MessageDetail;
                }
                else
                {
                    // Yeni taslak oluştur
                    draft = new Message
                    {
                        SenderEmail = user.Email,
                        ReceiverEmail = ReceiverEmail,
                        Subject = Subject,
                        MessageDetail = MessageDetail,
                        SendDate = DateTime.Now,
                        IsDraft = true,
                        IsStatus = false
                    };
                    _context.Messages.Add(draft);
                }

                // Dosyaları kaydet
                if (Attachments != null && Attachments.Count > 0)
                {
                    foreach (var file in Attachments)
                    {
                        if (file.Length > 0)
                        {
                            var fileName = Path.GetFileName(file.FileName);
                            var uploadPath = Path.Combine("wwwroot/uploads/attachments", user.Id);
                            Directory.CreateDirectory(uploadPath);

                            var filePath = Path.Combine(uploadPath, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            draft.Attachments.Add(new MessageAttachment
                            {
                                FileName = fileName,
                                FilePath = $"/uploads/attachments/{user.Id}/{fileName}",
                                FileSize = file.Length,
                                ContentType = file.ContentType,
                                UploadedDate = DateTime.Now
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, draftId = draft.MessageId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // 5. MESAJ DETAYI (DOSYA DESTEĞİ EKLENDİ)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var message = await _context.Messages
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            // Mesajı okundu yap
            if (message != null && message.ReceiverEmail == user.Email && message.IsStatus == false)
            {
                message.IsStatus = true;
                await _context.SaveChangesAsync();
            }
            return View(message);
        }

        // 6. YILDIZLAMA (AJAX)
        [HttpPost]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message != null)
            {
                message.IsStarred = !message.IsStarred;
                await _context.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }

        // 7. OKUNDU İŞARETLE (YENİ - AJAX)
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null) return Json(new { success = false });

                message.IsStatus = true;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 8. OKUNMADI İŞARETLE (YENİ - AJAX)
        [HttpPost]
        public async Task<IActionResult> MarkAsUnread(int messageId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null) return Json(new { success = false });

                message.IsStatus = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 9. ERTELEME (YENİ - AJAX)
        [HttpPost]
        public async Task<IActionResult> SnoozeMessage(int messageId, int hours)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null) return Json(new { success = false });

                message.SnoozeUntil = DateTime.Now.AddHours(hours);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        // 10. OKUNMAMIŞ SAYISI (YENİ - AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var count = await _context.Messages
                .Where(x => x.ReceiverEmail == user.Email && !x.IsStatus && !x.IsDraft)
                .CountAsync();

            return Json(new { count = count });
        }

        // 11. TEKLİ SİLME
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.MessageId == id);

            if (message != null)
            {
                // Dosyaları sil
                foreach (var attachment in message.Attachments)
                {
                    var filePath = Path.Combine("wwwroot", attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Inbox");
        }

        // 12. TOPLU SİLME (AJAX)
        [HttpPost]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest();

            var messagesToDelete = await _context.Messages
                .Include(m => m.Attachments)
                .Where(x => ids.Contains(x.MessageId))
                .ToListAsync();

            // Dosyaları sil
            foreach (var message in messagesToDelete)
            {
                foreach (var attachment in message.Attachments)
                {
                    var filePath = Path.Combine("wwwroot", attachment.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            _context.Messages.RemoveRange(messagesToDelete);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // YARDIMCI METOD: Kategori Algılama
        private string DetectCategory(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return "Genel";

            var lowerSubject = subject.ToLower();
            if (lowerSubject.Contains("iş") || lowerSubject.Contains("toplantı") || lowerSubject.Contains("proje"))
                return "İş";
            if (lowerSubject.Contains("acil") || lowerSubject.Contains("urgent"))
                return "Acil";
            if (lowerSubject.Contains("eğitim") || lowerSubject.Contains("kurs") || lowerSubject.Contains("ders"))
                return "Eğitim";
            if (lowerSubject.Contains("sosyal") || lowerSubject.Contains("etkinlik"))
                return "Sosyal";
            return "Genel";
        }
    }
}


