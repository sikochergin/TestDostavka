using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TestDostavka.Models.Enums;

namespace TestDostavka.Controllers
{
    [Route("Person")]
    public class PersonController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher<Person> _passwordHasher;

        public PersonController(
            AppDbContext dbContext,
            IPasswordHasher<Person> passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        [HttpGet("Register")]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Request");
            }

            return View();
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            CreatePersonRequest model,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var normalizedEmail = NormalizeEmail(model.Email);

            var emailAlreadyExists = _dbContext.Persons.Any(x => x.Email == normalizedEmail);

            if (emailAlreadyExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Пользователь с такой почтой уже существует.");

                return View(model);
            }

            var person = new Person
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                CreationDateTime = DateTime.UtcNow,
                Role = PersonRole.Customer
            };

            person.PasswordHash =
                _passwordHasher.HashPassword(person, model.Password);

            _dbContext.Persons.Add(person);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Не удалось зарегистрировать пользователя.");

                return View(model);
            }

            await SignInPersonAsync(
                person,
                isPersistent: false);

            return RedirectToAction("Index", "Request");
        }

        [AllowAnonymous]
        [HttpGet("Login")]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("RedirectUrl", "Request");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginPersonRequest model,
            string? returnUrl = null,
            CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var normalizedEmail = NormalizeEmail(model.Email);

            var person = await _dbContext.Persons.FirstOrDefaultAsync(x => x.Email == normalizedEmail);

            if (person is null)
            {
                AddInvalidLoginError();

                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var verificationResult =
                _passwordHasher.VerifyHashedPassword(
                    person,
                    person.PasswordHash,
                    model.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                AddInvalidLoginError();

                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            if (verificationResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                person.PasswordHash =
                    _passwordHasher.HashPassword(person, model.Password);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await SignInPersonAsync(
                person,
                model.RememberMe);

            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("RedirectUrl", "Request");
        }

        [Authorize]
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet("access-denied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInPersonAsync(
            Person person,
            bool isPersistent)
        {
            var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                person.Id.ToString()),

            new(
                ClaimTypes.Name,
                person.Email),

            new(
                ClaimTypes.Email,
                person.Email),

            new(
                ClaimTypes.Role,
                person.Role.ToString())
        };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var authenticationProperties =
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent,

                    ExpiresUtc = isPersistent
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : null,

                    AllowRefresh = true
                };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authenticationProperties);
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private void AddInvalidLoginError()
        {
            ModelState.AddModelError(
                string.Empty,
                "Неверная почта или пароль.");
        }
    }
}