using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BotWebTest.Data;
using BotWebTest.Models;
using Dallar;
using Dallar.Bots;
using Dallar.Services;
using Dallar.Exchange;
using Microsoft.AspNetCore.HttpOverrides;

namespace BotWebTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public class DallarAccountOverrider : IDallarAccountOverrider
        {
            protected IApplicationBuilder _app;
            public DallarAccountOverrider(IApplicationBuilder app)
            {
                _app = app;
            }

            public bool OverrideDallarAccountIfNeeded(ref DallarAccount Account)
            {
                DallarAccount tempAccount = Account;

                using (var scope = _app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var Manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                    //@TODO: Not hardcode these prefixes
                    if (Account.AccountPrefix == "twitch_")
                    {
                        ApplicationUser accountUser = Manager.Users.First(x => x.TwitchAccountId == tempAccount.AccountId);
                        if (accountUser != null)
                        {
                            Account = accountUser.DallarAccount();
                            return true;
                        }
                    }
                    else if (Account.AccountPrefix == "")
                    {
                        ApplicationUser accountUser = Manager.Users.First(x => x.DiscordAccountId == tempAccount.AccountId);
                        if (accountUser != null)
                        {
                            Account = accountUser.DallarAccount();
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source=TwitchBot.db"));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            //.AddDefaultTokenProviders();

            IDallarSettingsCollection SettingsCollection = DallarSettingsCollection.FromConfig();
            services.AddSingleton<IDallarSettingsCollection>(SettingsCollection);

            services.AddSingleton<IDallarClientService, DallarClientService>();
            services.AddSingleton<IFunServiceCollection, FunServiceCollection>();
            services.AddSingleton<IDallarPriceProviderService, DigitalPriceExchangeService>();

            services.AddTwitchBot();

            services.AddAuthentication()
                .AddTwitch(twitchOptions =>
                {
                    twitchOptions.ClientId = SettingsCollection.TwitchAuth.ClientId;
                    twitchOptions.ClientSecret = SettingsCollection.TwitchAuth.ClientSecret;
                    SettingsCollection.TwitchAuth.Scopes.ToList().ForEach(x => twitchOptions.Scope.Add(x));
                });

            services.AddAuthentication().AddDiscord(discordOptions =>
            {
                discordOptions.ClientId = SettingsCollection.Discord.ClientId;
                discordOptions.ClientSecret = SettingsCollection.Discord.ClientSecret;
                discordOptions.Scope.Add("guilds");
            });

            services.AddMvc();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            // Spin up the twitch bot and join all relevant channels
            ITwitchBot TwitchBot = app.ApplicationServices.GetService<ITwitchBot>();
            TwitchBot.OnConnectionStatusChanged += (o, s) =>
            {
                if (s.bConnected)
                {
                    using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                    {
                        var Manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var addedUsers = Manager.Users.Where(x => x.AddedToTwitchChannel == true);
                        foreach (ApplicationUser user in addedUsers)
                        {
                            (o as ITwitchBot).AttemptJoinChannel(user.TwitchChannel);
                        }
                    }
                }
            };
            TwitchBot.SetAccountOverrider(new DallarAccountOverrider(app));
        }
    }
}
