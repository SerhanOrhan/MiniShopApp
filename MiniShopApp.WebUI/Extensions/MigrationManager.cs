using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniShopApp.Data.Concrete.EfCore;
using MiniShopApp.WebUI.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiniShopApp.WebUI.Extensions
{
    public static class MigrationManager
    {//Oto Margiration  komutları
        public static IHost MigrateDatabase(this IHost host)//bu hosttan genisletilsin demek IHost tipi
        {
            using (var scope= host.Services.CreateScope())
            {
                using (var applicationContex= scope.ServiceProvider.GetRequiredService<ApplicationContext>())
                {
                    try
                    {
                        applicationContex.Database.Migrate();
                    }
                    catch (Exception)
                    {

                        throw;
                    };
                }
                using (var miniShopContex = scope.ServiceProvider.GetRequiredService<MiniShopContext>())
                {
                    try
                    {
                        miniShopContex.Database.Migrate();
                    }
                    catch (Exception)
                    {

                        throw;
                    };
                }
            }
            return host;
        }
    }
}
