using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;

namespace Project2EmailNight.Controllers
{
    public class ConfirmEmailController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ConfirmEmailController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // RegisterController'dan gelen maili TempData ile yakalıyoruz
            var value = TempData["Mail"];

            // Bu maili DTO içine koyup View'e (Ekrana) gönderiyoruz
            // Böylece input'un içinde otomatik yazılı gelecek.
            var model = new ConfirmUserDto();
            if (value != null)
            {
                model.Mail = value.ToString();
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ConfirmUserDto confirmUserDto)
        {
            // Kullanıcıyı mail adresinden buluyoruz
            var user = await _userManager.FindByEmailAsync(confirmUserDto.Mail);

            // Veritabanındaki kod ile ekrana girilen kod AYNI MI?
            if (user.ConfirmCode == confirmUserDto.ConfirmCode)
            {
                // Kod doğruysa, SQL'deki EmailConfirmed sütununu "True" (1) yap
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                // Ve mutlu son: Giriş Yap sayfasına gönder
                return RedirectToAction("UserLogin", "Login");
            }

            // Hata varsa (Kod yanlışsa) sayfada kal
            return View(confirmUserDto);
        }
    }
}