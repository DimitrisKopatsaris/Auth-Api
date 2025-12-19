using AuthApi.DTOs;
using AuthApi.Models;
using System.Threading.Tasks;

namespace AuthApi.Services
{
    public interface IUserService
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User> RegisterUserAsync(RegisterDto dto);
        Task<string?> LoginUserAsync(LoginDto dto);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<bool> DeleteUserAsync(Guid id);
        Task<bool> PromoteUserAsync(Guid id);
        Task<bool> DemoteUserAsync(Guid id);
    }
}
