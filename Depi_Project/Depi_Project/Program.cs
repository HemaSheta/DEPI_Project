using Depi_Project.Data;
using Depi_Project.Data.Repository.Implementations;
using Depi_Project.Data.Repository.Interfaces;
using Depi_Project.Data.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Depi_Project.Services.Interfaces;
using Depi_Project.Services.Implementations;


namespace Depi_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register Identity (important)
            builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

            // Generic repository (open generic)
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            // Unit of Work
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add MVC
            builder.Services.AddControllersWithViews();

            var app = builder.Build();


            // Service Layer
            builder.Services.AddScoped<IRoomService, RoomService>();
            builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
            builder.Services.AddScoped<IBookingService, BookingService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();


            app.MapControllerRoute(
                name: "admin",
                pattern: "Admin/{controller=RoomType}/{action=Index}/{id?}");



            // Pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Add authentication middleware BEFORE authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();

        }
    }
}
