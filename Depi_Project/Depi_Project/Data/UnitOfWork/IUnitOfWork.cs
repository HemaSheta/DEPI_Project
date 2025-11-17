using Depi_Project.Data.Repository.Interfaces;
using Depi_Project.Models;

namespace Depi_Project.Data.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<UserProfile> UserProfiles { get; }
        IGenericRepository<RoomType> RoomTypes { get; }
        IGenericRepository<Room> Rooms { get; }
        IGenericRepository<Booking> Bookings { get; }

        int Save();
    }
}
