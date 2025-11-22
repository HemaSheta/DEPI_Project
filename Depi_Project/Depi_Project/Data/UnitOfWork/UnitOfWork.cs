using Depi_Project.Data.Repository.Implementations;
using Depi_Project.Data.Repository.Interfaces;
using Depi_Project.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Depi_Project.Data.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        private IGenericRepository<UserProfile> _userProfiles;
        private IGenericRepository<RoomType> _roomTypes;
        private IGenericRepository<Room> _rooms;
        private IGenericRepository<Booking> _bookings;
        public AppDbContext Context => _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }
        public IDbContextTransaction BeginTransaction()
        {
            return _context.Database.BeginTransaction();
        }

        public IGenericRepository<UserProfile> UserProfiles =>
            _userProfiles ??= new GenericRepository<UserProfile>(_context);

        public IGenericRepository<RoomType> RoomTypes =>
            _roomTypes ??= new GenericRepository<RoomType>(_context);

        public IGenericRepository<Room> Rooms =>
            _rooms ??= new GenericRepository<Room>(_context);

        public IGenericRepository<Booking> Bookings =>
            _bookings ??= new GenericRepository<Booking>(_context);

        public int Save()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
