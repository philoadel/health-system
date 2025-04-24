// DeptRepository.cs
using UserAccountAPI.Models;
using Microsoft.EntityFrameworkCore;
using UserAccountAPI.Data;
using UserAccountAPI.Repositories.Interfaces;

namespace MedicalAPI_1.Repositories
{
    public class DeptRepository : IDeptRepository
    {
        private readonly ApplicationDbContext _context;

        public DeptRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Department>> GetAllDepartments()
        {
            return await _context.Departments.ToListAsync();
        }

        public async Task<Department?> GetDepartmentById(int id)
        {
            return await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Department> AddDepartment(Department department)
        {
            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<Department?> UpdateDepartment(int id, Department department)
        {
            var existingDept = await GetDepartmentById(id);
            if (existingDept == null) return null;

            existingDept.Name = department.Name;
            existingDept.Description = department.Description;

            await _context.SaveChangesAsync();
            return existingDept;
        }

        public async Task<bool> DeleteDepartment(int id)
        {
            var department = await GetDepartmentById(id);
            if (department == null) return false;

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Department>> FilterDepartments(string? name)
        {
            var query = _context.Departments.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(d => d.Name.Contains(name));
            }

            return await query.ToListAsync();
        }
    }
}