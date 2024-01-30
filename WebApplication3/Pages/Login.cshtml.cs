using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Security.Claims;
using WebApplication3.ViewModels;

namespace WebApplication3.Pages
{
    public class reCaptchaResponse
    {
        public bool Success { get; set; }
        public string[] ErrorCodes { get; set; }
    }

    public class LoginModel : PageModel
    {

        [BindProperty]
        public Login LModel { get; set; }

        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        public LoginModel(SignInManager<ApplicationUser> signInManager, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.signInManager = signInManager;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {

                // Verify reCAPTCHA
                var userResponse = HttpContext.Request.Form["g-recaptcha-response"];
                var isCaptchaValid = await GetreCaptchaResponse(userResponse);



                var identityResult = await signInManager.PasswordSignInAsync(LModel.Email, LModel.Password, LModel.RememberMe, false);
                if (identityResult.Succeeded)
                {

                    //Create the security context
                    var claims = new List<Claim> {
                        new Claim(ClaimTypes.Name, "c@c.com"),
                        new Claim(ClaimTypes.Email, "c@c.com"),
                        new Claim("Department", "HR")
                    };
                    var i = new ClaimsIdentity(claims, "MyCookieAuth");
                    ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(i);
                    await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);

                    return RedirectToPage("Index");
                }
                ModelState.AddModelError("", "Username or Password incorrect");
            }
            return Page();
        }

        private async Task<bool> GetreCaptchaResponse(string userResponse)
        {
            var reCaptchaSecretKey = configuration["reCaptcha:SecretKey"];

            if (reCaptchaSecretKey != null && userResponse != null)
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"secret", reCaptchaSecretKey },
                    {"response", userResponse }
                });

                using (var httpClient = httpClientFactory.CreateClient())
                {
                    var response = await httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<reCaptchaResponse>();
                        return result.Success;
                    }
                }
            }

            return false;
        }

    }
}
