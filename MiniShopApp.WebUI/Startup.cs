using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniShopApp.Business.Abstract;
using MiniShopApp.Business.Concrete;
using MiniShopApp.Data.Abstract;
using MiniShopApp.Data.Concrete;
using MiniShopApp.Data.Concrete.EfCore;
using MiniShopApp.WebUI.EmailServices;
using MiniShopApp.WebUI.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniShopApp.WebUI
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
            //Sqlite i�in
            services.AddDbContext<ApplicationContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteConnection")));
            services.AddDbContext<MiniShopContext>(options => options.UseSqlite(Configuration.GetConnectionString("SqliteConnection")));
            //Sql server i�in
            //services.AddDbContext<ApplicationContext>(options => options.UseSqlServer(Configuration.GetConnectionString("MsSqlConnection")));
            //services.AddDbContext<MiniShopContext>(options => options.UseSqlServer(Configuration.GetConnectionString("MsSqlConnection")));

            services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<ApplicationContext>().AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(options =>
            {
                //Password
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;

                //Lockout
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                //User
                options.User.RequireUniqueEmail = true;

                //SignIn
                options.SignIn.RequireConfirmedEmail = true;
                

            });
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.LogoutPath = "/account/logout";
                options.AccessDeniedPath = "/account/accessdenied";//yetkin yok sen�n demek i�in yapt�g�m�z url
                options.SlidingExpiration = true; // istek g�nderirse s�f�rla surey� demek
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);//20 dk �stek yapmazsa 20dk sonra kullan�c� giri�inden ��k�l�r.
                options.Cookie = new CookieBuilder()
                {
                    HttpOnly = true,
                    Name = "MiniShopApp.Security.Cookie",
                    SameSite=SameSiteMode.Strict
                };
            });
            //services.AddScoped<IProductRepository, EfCoreProductRepository>();
            //services.AddScoped<ICategoryRepository, EfCoreCategoryRepository>();
            //services.AddScoped<ICardRepository, EfCoreCardRepository>();
            //services.AddScoped<IOrderRepository, EfCoreOrderRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Proje boyunca ICategoryService �a�r�ld���nda, CategoryManager'i kullan.
            services.AddScoped<ICategoryService, CategoryManager>();
            services.AddScoped<IProductService, ProductManager>();
            services.AddScoped<ICardService, CardManager>();
            services.AddScoped<IOrderService, OrderManager>();




            services.AddScoped<IEmailSender, SmtpEmailSender>(i=> new SmtpEmailSender(
                Configuration["EmailSender:Host"],
                Configuration.GetValue<int>("EmailSender:Port"),
                Configuration.GetValue<bool>("EmailSender:EnableSSL"),
                Configuration["EmailSender:UserName"],
                Configuration["EmailSender:Password"]
                ));





            //Projemizin MVC yap�s�nda olmas�n� sa�lar.
            services.AddControllersWithViews();
            services.AddRazorPages().AddViewOptions(options =>
                options.HtmlHelperOptions.ClientValidationEnabled = false
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,UserManager<User> userManager,RoleManager<IdentityRole> roleManager,ICardService cardService)
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
            app.UseAuthentication();
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                name: "orders",
                pattern: "orders",
                defaults: new { controller = "Card", action = "GetOrders" }
                    );
                endpoints.MapControllerRoute(
                name: "checkout",
                pattern: "checkout",
                defaults: new { controller = "Card", action = "CheckOut" }
                    );
                endpoints.MapControllerRoute(
                name: "card",
                pattern: "/card",
                defaults: new { controller = "Card", action = "Index" }
                    );
                endpoints.MapControllerRoute(
                name: "adminusers",
                pattern: "admin/user/list",
                defaults: new { controller = "Admin", action = "UserList" }
                    );
                endpoints.MapControllerRoute(
                name: "adminusercreate",
                pattern: "admin/user/create",
                defaults: new { controller = "Admin", action = "UserCreate" }
                    );
                endpoints.MapControllerRoute(
                name: "adminrolecreate",
                pattern: "admin/role/create",
                defaults: new { controller = "Admin", action = "RoleCreate" }
                    );
                endpoints.MapControllerRoute(
               name: "adminuseredit",
               pattern: "admin/user/{id}",
               defaults: new { controller = "Admin", action = "UserEdit" }
                    );
                endpoints.MapControllerRoute(
                name: "adminroles",
                pattern: "admin/role/list",
                defaults: new { controller = "Admin", action = "RoleList" }
                    );
                endpoints.MapControllerRoute(
               name: "adminroleedit",
               pattern: "admin/role/{id}",
               defaults: new { controller = "Admin", action = "RoleEdit" }
                    );
                endpoints.MapControllerRoute(
                    name: "adminproductcreate",
                    pattern: "admin/products/create",
                    defaults: new { controller = "Admin", action = "ProductCreate" }
                    );
                endpoints.MapControllerRoute(
                    name: "adminproducts",
                    pattern: "admin/products",
                    defaults: new { controller = "Admin", action = "ProductList" }
                    );
                endpoints.MapControllerRoute(
                    name: "search",
                    pattern: "search",
                    defaults: new { controller = "MiniShop", action = "Search" }
                    );
                endpoints.MapControllerRoute(
                   name: "products",
                   pattern: "products/{category?}",
                   defaults: new { controller = "MiniShop", action = "List" }
                   );
                endpoints.MapControllerRoute(
                    name: "adminproductedit",
                    pattern: "admin/products/{id?}",
                    defaults: new { controller = "Admin", action = "ProductEdit" }
                    );
                endpoints.MapControllerRoute(
                    name: "productdetails",
                    pattern: "{url}",
                    defaults: new { controller = "MiniShop", action = "Details" }
                    );

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            //Buraya kullan�c� bilgilerini olu�turacak metodumuz cag�ran kodu yazaca��z.
            SeedIdentity.Seed(userManager, roleManager, cardService, Configuration).Wait();
        }
    }
}