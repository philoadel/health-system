using UserAccountAPI.Models;

namespace UserAccountAPI.Repositories.Interfaces
{
    public interface IDeptRepository
    {
        Task<List<Department>> GetAllDepartments();
        Task<Department?> GetDepartmentById(int id);
        Task<Department> AddDepartment(Department department);
        Task<Department?> UpdateDepartment(int id, Department department);
        Task<bool> DeleteDepartment(int id);
        Task<List<Department>> FilterDepartments(string? name);
    }
}
