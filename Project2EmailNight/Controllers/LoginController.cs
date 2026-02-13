using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;
using MimeKit;
using MailKit.Net.Smtp;

namespace Project2EmailNight.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UserLogin(UserLoginDto userLoginDto)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(userLoginDto.Username);

                if (user != null)
                {
                    // EĞER MAİL ONAYLI DEĞİLSE -> YENİ KOD ÜRET, DB'YE YAZ VE MAİL AT
                    if (user.EmailConfirmed == false)
                    {
                        // 1. Yeni 6 Haneli Kod Üret
                        Random random = new Random();
                        int code = random.Next(100000, 999999);

                        // 2. Kodu Veritabanına (AppUser -> ConfirmCode) Kaydet
                        user.ConfirmCode = code.ToString();
                        await _userManager.UpdateAsync(user);

                        // 3. Mail Gönderme İşlemi
                        MimeMessage mimeMessage = new MimeMessage();
                        MailboxAddress mailboxAddressFrom = new MailboxAddress("Admin", "cn.15.mr@gmail.com"); // GÜNCELLE
                        MailboxAddress mailboxAddressTo = new MailboxAddress("User", user.Email);

                        mimeMessage.From.Add(mailboxAddressFrom);
                        mimeMessage.To.Add(mailboxAddressTo);

                        var bodyBuilder = new BodyBuilder();
                        bodyBuilder.HtmlBody = $"<h2>Giriş Doğrulama Kodunuz: {code}</h2>"; // Sadece kodu gönderiyoruz

                        mimeMessage.Body = bodyBuilder.ToMessageBody();
                        mimeMessage.Subject = "Project2EmailNight - Onay Kodu";

                        using (var client = new SmtpClient())
                        {
                            client.Connect("smtp.gmail.com", 587, false);
                            client.Authenticate("cn.15.mr@gmail.com", "knbp ipay tzvb zbrz"); // GÜNCELLE
                            client.Send(mimeMessage);
                            client.Disconnect(true);
                        }

                        // 4. Doğrulama Ekranına Yönlendir
                        TempData["Mail"] = user.Email;
                        return RedirectToAction("Index", "ConfirmEmail");
                    }

                    // Mail onaylıysa normal giriş yap
                    var result = await _signInManager.PasswordSignInAsync(userLoginDto.Username, userLoginDto.Password, true, false);

                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        ViewBag.Error = "Kullanıcı adınız veya şifreniz hatalı!";
                        return View(userLoginDto);
                    }
                }
                else
                {
                    ViewBag.Error = "Böyle bir kullanıcı bulunamadı!";
                    return View(userLoginDto);
                }
            }

            return View(userLoginDto);
        }
    }
}