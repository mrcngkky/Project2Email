using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Project2EmailNight.Dtos;
using Project2EmailNight.Entities;

namespace Project2EmailNight.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public RegisterController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult CreateUser() // DİKKAT: Dosya adın CreateUser olduğu için burası da CreateUser olmalı
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(UserRegisterDto userRegisterDto)
        {
            if (ModelState.IsValid)
            {
                Random rnd = new Random();
                int code = rnd.Next(100000, 1000000);

                AppUser appUser = new AppUser()
                {
                    Name = userRegisterDto.Name,
                    Surname = userRegisterDto.Surname,
                    UserName = userRegisterDto.Username,
                    Email = userRegisterDto.Email,

                    
                    ConfirmCode = code.ToString()
                };

                var result = await _userManager.CreateAsync(appUser, userRegisterDto.Password);

                if (result.Succeeded)
                {
                    MimeMessage mimeMessage = new MimeMessage();
                    MailboxAddress mailboxAddressFrom = new MailboxAddress("Admin Kayıt", "cn.15.mr@gmail.com");
                    mimeMessage.From.Add(mailboxAddressFrom);

                    MailboxAddress mailboxAddressTo = new MailboxAddress("User", appUser.Email);
                    mimeMessage.To.Add(mailboxAddressTo);

                    mimeMessage.Subject = "Syndash Kayıt Doğrulama Kodu";

                    var bodyBuilder = new BodyBuilder();
                    bodyBuilder.TextBody = "Kayıt işlemini tamamlamak için doğrulama kodunuz: " + code;
                    mimeMessage.Body = bodyBuilder.ToMessageBody();

                    SmtpClient client = new SmtpClient();
                    client.Connect("smtp.gmail.com", 587, false);
                    client.Authenticate("cn.15.mr@gmail.com", "knbp ipay tzvb zbrz");
                    client.Send(mimeMessage);
                    client.Disconnect(true);

                    
                    TempData["Mail"] = userRegisterDto.Email;

                    
                    return RedirectToAction("Index", "ConfirmEmail");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError("", item.Description);
                    }
                }
            }
            return View(userRegisterDto);
        }
    }
}