using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskAuthenticationAuthorization.Models;

namespace TaskAuthenticationAuthorization
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
            string connection = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ShoppingContext>(options => options.UseSqlServer(connection));
            services.AddDbContext<AccountsContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("IdentityConnection")));
            services.AddIdentity<ApplicationUser, IdentityRole>(options => options.User.RequireUniqueEmail = true)
                .AddEntityFrameworkStores<AccountsContext>();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Events.OnValidatePrincipal = ValidateLoginAsync;
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("MyDiscount", policy => policy
                    .RequireRole("Buyer")
                    .RequireClaim(BuyerTypes.ClaimName, BuyerTypes.Golden, BuyerTypes.Wholesale));
            });
            services.AddControllersWithViews();
        }

        private static async Task ValidateLoginAsync(CookieValidatePrincipalContext context)
        {
            var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
            var user = await signIn.ValidateSecurityStampAsync(context.Principal);

            // An account edit changes the security stamp and ends existing sessions.
            if (user == null)
            {
                context.RejectPrincipal();
                await signIn.SignOutAsync();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
