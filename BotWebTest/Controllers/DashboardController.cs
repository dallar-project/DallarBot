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
                DallarAccount DallarAccount = user.DallarAccount();

                if (_twitchBot.DaemonClient.GetWalletAddressFromAccount(true, ref DallarAccount))
                {
                    ViewData["ShowDiscordWallet"] = false;
                    ViewData["ShowTwitchWallet"] = false;
                    ViewData["IsDiscordLinked"] = false;
                    ViewData["IsTwitchLinked"] = false;
                    ViewData["HasDiscordDallar"] = false;
                    ViewData["HasTwitchDallar"] = false;

                    if (!string.IsNullOrEmpty(user.DiscordAccountId))
                    {
                        ViewData["ShowDiscordWallet"] = true;
                        ViewData["IsDiscordLinked"] = true;

                        decimal balance = _twitchBot.DaemonClient.GetRawAccountBalance(DallarAccount);
                        decimal pendingBalance = _twitchBot.DaemonClient.GetUnconfirmedAccountBalance(DallarAccount);

                        ViewData["DiscordBalance"] = balance;
                        ViewData["DiscordPendingBalance"] = pendingBalance;

                        ViewData["HasDiscordDallar"] = (balance > 0);

                        decimal txfee = _settingsCollection.Dallar.Txfee;
                        decimal Amount = balance - txfee;
                        ViewData["DiscordSpendable"] = Amount;

                        ViewData["DiscordDepositAddress"] = DallarAccount.KnownAddress;

                        ViewData["WithdrawBalance"] = balance;
                        ViewData["WithdrawSpendable"] = Amount;
                    }

                    if (!string.IsNullOrEmpty(user.TwitchAccountId))
                    {
                        ViewData["ShowTwitchWallet"] = ((bool)ViewData["ShowDiscordWallet"] == false);
                        ViewData["IsTwitchLinked"] = true;

                        decimal balance = _twitchBot.DaemonClient.GetRawAccountBalance(DallarAccount);
                        decimal pendingBalance = _twitchBot.DaemonClient.GetUnconfirmedAccountBalance(DallarAccount);

                        ViewData["TwitchBalance"] = balance;
                        ViewData["TwitchPendingBalance"] = pendingBalance;

                        ViewData["HasTwitchDallar"] = (balance > 0);

                        decimal txfee = _settingsCollection.Dallar.Txfee;
                        decimal Amount = balance - txfee;
                        ViewData["TwitchSpendable"] = Amount;

                        ViewData["TwitchDepositAddress"] = DallarAccount.KnownAddress;

                        if((bool)ViewData["ShowDiscordWallet"] == false)
                        {
                            ViewData["WithdrawBalance"] = balance;
                            ViewData["WithdrawSpendable"] = Amount;
                        }
                    }

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

                ViewData["AddedToChannel"] = user.AddedToTwitchChannel;                

                return View();
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
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
            if (string.IsNullOrEmpty(user.TwitchChannel))
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }

            user.AddedToTwitchChannel = bActive;
            await _userManager.UpdateAsync(user);

            if (bActive)
            {
                _twitchBot.AttemptJoinChannel(user.TwitchChannel);
            }
            else
            {
                _twitchBot.AttemptLeaveChannel(user.TwitchChannel);
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
            DallarAccount DallarAccount = user.DallarAccount();

            DallarAccount WithdrawAccount = new DallarAccount();

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

            WithdrawAccount.KnownAddress = DallarWithdrawData.DallarAddress;

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
            if (!_twitchBot.DaemonClient.CanAccountAffordTransaction(DallarAccount, DallarWithdrawData.Amount))
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
            if (_twitchBot.DaemonClient.GetWalletAddressFromAccount(true, ref DallarAccount))
            {
                if (_twitchBot.DaemonClient.SendMinusFees(DallarAccount, WithdrawAccount, DallarWithdrawData.Amount, true))
                {
                    // Successfully withdrew
                    TempData.Put("Withdraw", new DallarWithdrawResultModel()
                    {
                        bSuccess = true,
                        DallarAddress = DallarWithdrawData.DallarAddress,
                        Amount = DallarWithdrawData.Amount
                    });

                    _logger.LogInformation($"{user.UserName} ({DallarAccount.UniqueAccountName}): Successfully withdrew {DallarWithdrawData.Amount} from wallet ({DallarAccount}) to {WithdrawAccount.KnownAddress}.");
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
                // Attempt to link two accounts together
                if (_signInManager.IsSignedIn(User) && User.Identity != info.Principal)
                {
                    ApplicationUser user = await _userManager.GetUserAsync(User);

                    if (info.LoginProvider == "Twitch")
                    {
                        // Already have a linked twitch account, do nothing?
                        // @TODO: Error handling
                        if (!string.IsNullOrEmpty(user.TwitchChannel))
                        {
                            _logger.LogInformation("User already has Twitch info but tried merging Twitch account?");
                            return RedirectToLocal("/");
                        }
                        else
                        {
                            ApplicationUser oldUser = _userManager.Users.First(x => x.TwitchAccountId == info.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
                            if (oldUser != null)
                            {
                                await _userManager.RemoveLoginAsync(oldUser, info.LoginProvider, info.ProviderKey);
                                await _userManager.DeleteAsync(oldUser);
                            }

                            // Merge new twitch data into this account
                            user.TwitchAccountId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                            user.TwitchChannel = info.Principal.FindFirstValue(ClaimTypes.Name);
                            var userResult = await _userManager.UpdateAsync(user);
                            if (userResult.Succeeded)
                            {
                                userResult = await _userManager.AddLoginAsync(user, info);
                                if (userResult.Succeeded)
                                {
                                    decimal bal = _twitchBot.DaemonClient.GetRawAccountBalance(user.DallarAccount("Twitch"));
                                    if (bal - _twitchBot.DaemonClient.txFee > 0)
                                    {
                                        _twitchBot.DaemonClient.SendMinusFees(user.DallarAccount("Twitch"), user.DallarAccount("Discord"), bal - _twitchBot.DaemonClient.txFee, true);
                                    }

                                    await _signInManager.SignInAsync(user, isPersistent: false);
                                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                                    return RedirectToAction(nameof(DashboardController.Index));
                                }
                            }
                        }
                    }
                    else if (info.LoginProvider == "Discord")
                    {
                        // Already have a linked twitch account, do nothing?
                        // @TODO: Error handling
                        if (!string.IsNullOrEmpty(user.DiscordUserName))
                        {
                            _logger.LogInformation("User already has Discord info but tried merging Discord account?");
                            return RedirectToLocal("/");
                        }
                        else
                        {
                            ApplicationUser oldUser = _userManager.Users.First(x => x.DiscordAccountId == info.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
                            if (oldUser != null)
                            {
                                await _userManager.RemoveLoginAsync(oldUser, info.LoginProvider, info.ProviderKey);
                                await _userManager.DeleteAsync(oldUser);
                            }

                            // Merge new discord data into this account
                            user.DiscordAccountId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                            user.DiscordUserName = info.Principal.FindFirstValue(ClaimTypes.Name);
                            var userResult = await _userManager.UpdateAsync(user);
                            if (userResult.Succeeded)
                            {
                                userResult = await _userManager.AddLoginAsync(user, info);
                                if (userResult.Succeeded)
                                {
                                    decimal bal = _twitchBot.DaemonClient.GetRawAccountBalance(user.DallarAccount("Twitch"));
                                    if (bal - _twitchBot.DaemonClient.txFee > 0)
                                    {
                                        _twitchBot.DaemonClient.SendMinusFees(user.DallarAccount("Twitch"), user.DallarAccount("Discord"), bal - _twitchBot.DaemonClient.txFee, true);
                                    }

                                    await _signInManager.SignInAsync(user, isPersistent: false);
                                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                                    return RedirectToAction(nameof(DashboardController.Index));
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                    return RedirectToLocal(returnUrl);
                }

                
            }
            if (result.IsLockedOut)
            {
                return RedirectToLocal("/");
            }
            else
            {
                // User is not logged in, so lets register a user
                if (!_signInManager.IsSignedIn(User))
                {

                    ViewData["ReturnUrl"] = returnUrl;
                    ViewData["LoginProvider"] = info.LoginProvider;

                    var user = new ApplicationUser
                    {
                        UserName = info.Principal.FindFirstValue(ClaimTypes.Name)
                    };

                    if (info.ProviderDisplayName == "Twitch")
                    {
                        user.TwitchAccountId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        user.TwitchChannel = info.Principal.FindFirstValue(ClaimTypes.Name);
                    }
                    else if (info.ProviderDisplayName == "Discord")
                    {
                        user.DiscordAccountId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        user.DiscordUserName = info.Principal.FindFirstValue(ClaimTypes.Name);
                    }
                    else
                    {
                        return RedirectToLocal("/");
                    }

                    var userResult = await _userManager.CreateAsync(user);
                    if (userResult.Succeeded)
                    {
                        userResult = await _userManager.AddLoginAsync(user, info);
                        if (userResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                            return RedirectToAction(nameof(DashboardController.Index));
                        }
                    }
                    AddErrors(userResult);
                }
                else // User is logged in, must be linking a new account that has no need to merge
                {
                    ApplicationUser user = await _userManager.GetUserAsync(HttpContext.User);

                    if (info.ProviderDisplayName == "Twitch")
                    {
                        user.TwitchAccountId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        user.TwitchChannel = info.Principal.FindFirstValue(ClaimTypes.Name);
                    }
                    else if (info.ProviderDisplayName == "Discord")
                    {
                        user.DiscordAccountId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        user.DiscordUserName = info.Principal.FindFirstValue(ClaimTypes.Name);
                    }
                    else
                    {
                        return RedirectToLocal("/");
                    }

                    var userResult = await _userManager.UpdateAsync(user);
                    if (userResult.Succeeded)
                    {
                        userResult = await _userManager.AddLoginAsync(user, info);
                        if (userResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                            return RedirectToAction(nameof(DashboardController.Index));
                        }
                    }
                    AddErrors(userResult);
                }
                
                
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
