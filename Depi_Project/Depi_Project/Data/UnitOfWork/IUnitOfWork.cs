using Depi_Project.Data.Repository.Interfaces;
using Depi_Project.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Depi_Project.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<UserProfile> UserProfiles { get; }
        IGenericRepository<RoomType> RoomTypes { get; }
        IGenericRepository<Room> Rooms { get; }
        IGenericRepository<Booking> Bookings { get; }
        AppDbContext Context { get; }

        // existing members...
        IDbContextTransaction BeginTransaction();

        int Save();
    }
}
