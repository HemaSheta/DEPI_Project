using Depi_Project.Models;
using Depi_Project.Data.UnitOfWork;
using Depi_Project.Services.Interfaces;

namespace Depi_Project.Services.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserProfileService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public UserProfile? GetProfileByIdentityId(string identityUserId)
        {
            return _unitOfWork.UserProfiles
                .GetAll()
                .FirstOrDefault(p => p.IdentityUserId == identityUserId);
        }


        public void CreateProfile(UserProfile profile)
        {
            _unitOfWork.UserProfiles.Add(profile);
            _unitOfWork.Save();
        }

        public void UpdateProfile(UserProfile profile)
        {
            _unitOfWork.UserProfiles.Update(profile);
            _unitOfWork.Save();
        }
    }
}
