using Depi_Project.Models;

namespace Depi_Project.Services.Interfaces
{
    public interface IUserProfileService
    {
        UserProfile GetProfileByIdentityId(string identityUserId);
        void CreateProfile(UserProfile profile);
        void UpdateProfile(UserProfile profile);
    }
}
