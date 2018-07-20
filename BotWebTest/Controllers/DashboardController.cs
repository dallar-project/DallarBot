using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BotWebTest.Models;
using Dallar.Bots;
using Dallar;
using BotWebTest.Extensions;
using System.Linq;

namespace BotWebTest.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IDallarSettingsCollection _settingsCollection;
        private readonly ILogger _logger;
        private readonly ITwitchBot _twitchBot;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IDallarSettingsCollection settingsCollection,
            ILogger<DashboardController> logger,
            ITwitchBot twitchBot)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _settingsCollection = settingsCollection;
            _logger = logger;
            _twitchBot = twitchBot;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (_signInManager.IsSignedIn(User))
            {
                ApplicationUser user = await _userManager.GetUserAsync(HttpContext.User);

                if (_twitchBot.DaemonClient.GetWalletAddressFromAccount(user.NameIdentifier, true, out string depositAddress))
                {
                    decimal balance = _twitchBot.DaemonClient.GetRawAccountBalance(user.NameIdentifier);
                    decimal pendingBalance = _twitchBot.DaemonClient.GetUnconfirmedAccountBalance(user.NameIdentifier);

                    ViewData["Balance"] = balance;
                    ViewData["PendingBalance"] = pendingBalance;

                    decimal txfee = _settingsCollection.Dallar.Txfee;
                    decimal Amount = balance - txfee;
                    ViewData["Spendable"] = Amount;

                    ViewData["DepositAddress"] = depositAddress;

                    DallarWithdrawResultModel WithdrawInfo = TempData.Get<DallarWithdrawResultModel>("Withdraw");

                    if (WithdrawInfo != null)
                    {
                        if (WithdrawInfo.bSuccess)
                        {
                            ViewData["WithdrawSuccess"] = $"You have successfully withdrawn {WithdrawInfo.Amount} DAL to address {WithdrawInfo.DallarAddress}!";
                        }
                        else
                        {
                            ViewData["WithdrawFailed"] = $"Failed to withdraw Dallar. Error: {WithdrawInfo.ErrorMessage}";
                        }
                    }
                }
                else
                {
                    // @TODO: Wallet error?
                }

                ViewData["AddedToChannel"] = user.AddedToChannel;                

                return View();
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        public void Ass()
        {
            _userManager.Users.Where(x => x.AddedToChannel == true);
        }

        [HttpPost]
        public async Task<IActionResult> SetTwitchBotActive(bool bActive)
        {
            // Not signed in?
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            ApplicationUser user = await _userManager.GetUserAsync(HttpContext.User);
            user.AddedToChannel = bActive;
            await _userManager.UpdateAsync(user);

            if (bActive)
            {
                _twitchBot.AttemptJoinChannel(user.UserName);
            }
            else
            {
                _twitchBot.AttemptLeaveChannel(user.UserName);
            }

            return RedirectToAction(nameof(DashboardController.Index));
        }

        [HttpPost]
        public async Task<IActionResult> WithdrawDallar(DallarWithdrawModel DallarWithdrawData)
        {
            // Not signed in?
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            ApplicationUser user = await _userManager.GetUserAsync(HttpContext.User);

            // Invalid address?
            if (!_twitchBot.DaemonClient.IsAddressValid(DallarWithdrawData.DallarAddress))
            {
                TempData.Put("Withdraw", new DallarWithdrawResultModel()
                {
                    bSuccess = false,
                    ErrorMessage = "You've tried to withdraw to an invalid Dallar address."
                });

                return RedirectToAction(nameof(DashboardController.Index));
            }

            // Invalid amount?
            if (DallarWithdrawData.Amount <= 0)
            {
                TempData.Put("Withdraw", new DallarWithdrawResultModel()
                {
                    bSuccess = false,
                    ErrorMessage = "You've tried to withdraw an invalid amount."
                });

                return RedirectToAction(nameof(DashboardController.Index));
            }

            // Can't afford transaction?
            if (!_twitchBot.DaemonClient.CanAccountAffordTransaction(user.NameIdentifier, DallarWithdrawData.Amount, _settingsCollection.Dallar.Txfee))
            {
                // @TODO: Getting wallet failed?
                TempData.Put("Withdraw", new DallarWithdrawResultModel()
                {
                    bSuccess = false,
                    ErrorMessage = "You can not afford to withdraw the requested amount."
                });

                return RedirectToAction(nameof(DashboardController.Index));
            }

            // Amount should be guaranteed a good value to withdraw
            // Fetch user's wallet
            if (_twitchBot.DaemonClient.GetWalletAddressFromAccount(user.NameIdentifier, true, out string Wallet))
            {
                if (_twitchBot.DaemonClient.SendMinusFees(user.NameIdentifier, DallarWithdrawData.DallarAddress, DallarWithdrawData.Amount, _settingsCollection.Dallar.Txfee, _settingsCollection.Dallar.FeeAccount))
                {
                    // Successfully withdrew
                    TempData.Put("Withdraw", new DallarWithdrawResultModel()
                    {
                        bSuccess = true,
                        DallarAddress = DallarWithdrawData.DallarAddress,
                        Amount = DallarWithdrawData.Amount
                    });

                    _logger.LogInformation($"{user.UserName} ({user.NameIdentifier}): Successfully withdrew {DallarWithdrawData.Amount} from wallet ({Wallet}).");
                    return RedirectToAction(nameof(DashboardController.Index));
                }
                else
                {
                    // Daemon send failed?
                    TempData.Put("Withdraw", new DallarWithdrawResultModel()
                    {
                        bSuccess = false,
                        ErrorMessage = "The Dallar Wallet Service was unable to withdraw your requested amount. Maybe you can no longer afford it?"
                    });

                    return RedirectToAction(nameof(DashboardController.Index));
                }
            }
            else
            {
                // @TODO: Getting wallet failed?
                TempData.Put("Withdraw", new DallarWithdrawResultModel()
                {
                    bSuccess = false,
                    ErrorMessage = "The Dallar Wallet Service was unable to retrieve your wallet. Please contact support in our Discord."
                });

                return RedirectToAction(nameof(DashboardController.Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Dashboard", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToLocal("/");
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToLocal("/");
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToLocal("/");
            }
            else
            {
                // If the user does not have an account, create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;

                var user = new ApplicationUser {
                    Email = info.Principal.FindFirstValue(ClaimTypes.Email),
                    UserName = info.Principal.FindFirstValue(ClaimTypes.Name),
                    NameIdentifier = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                };

                var userResult = await _userManager.CreateAsync(user);
                if (userResult.Succeeded)
                {
                    userResult = await _userManager.AddLoginAsync(user, info);
                    if (userResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(userResult);
            }

            return RedirectToLocal("/");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
