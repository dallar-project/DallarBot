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

namespace BotWebTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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

            services.AddTwitchBot();

            services.AddAuthentication()
                .AddTwitch(twitchOptions =>
                {
                    twitchOptions.ClientId = SettingsCollection.TwitchAuth.ClientId;
                    twitchOptions.ClientSecret = SettingsCollection.TwitchAuth.ClientSecret;
                    SettingsCollection.TwitchAuth.Scopes.ToList().ForEach(x => twitchOptions.Scope.Add(x));
                });

            services.AddMvc();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
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
                        var addedUsers = Manager.Users.Where(x => x.AddedToChannel == true);
                        foreach (ApplicationUser user in addedUsers)
                        {
                            (o as ITwitchBot).AttemptJoinChannel(user.UserName);
                        }
                    }
                }
            };
        }
    }
}
