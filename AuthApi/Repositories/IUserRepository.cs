using AuthApi.Models;
using System.Threading.Tasks;

namespace AuthApi.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task AddUserAsync(User user);
        Task SaveChangesAsync();
        void Delete(User user);
        Task<User?> GetByIdAsync(Guid id);
        Task UpdateUserAsync(User user);

    }
}
