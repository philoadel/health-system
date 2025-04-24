using UserAccountAPI.Models;
using UserAccountAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAccountAPI.Repositories.Interfaces;

namespace UserAccountAPI.Controllers
{
    [Route("api/[controller]")]  // Changed from api/v2/[controller] for consistency
    [ApiController]
    [Authorize]  // Added basic authorization
    public class DepartmentController : ControllerBase
    {
        private readonly IDeptRepository _repo;
        private readonly IDoctorRepository _doctorRepository;

        public DepartmentController(IDeptRepository repo, IDoctorRepository doctorRepository)
        {
            _repo = repo;
            _doctorRepository = doctorRepository;
        }

        [HttpGet]  // Changed from AllDepartments for REST convention
        public async Task<IActionResult> GetAllDepartments()
        {
            var departments = await _repo.GetAllDepartments();
            return Ok(departments);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDepartment(int id)
        {
            var department = await _repo.GetDepartmentById(id);
            return department == null ? NotFound("Department not found") : Ok(department);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]  // Only admins can update departments
        public async Task<IActionResult> UpdateDepartment(int id, [FromBody] Department department)
        {
            var updatedDept = await _repo.UpdateDepartment(id, department);
            return updatedDept == null ? NotFound() : Ok(updatedDept);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]  // Only admins can delete departments
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var result = await _repo.DeleteDepartment(id);
            return result ? NoContent() : NotFound();
        }

        [HttpPost]  // Changed from AddDepartment for REST convention
        [Authorize(Roles = "Admin")]  // Only admins can add departments
        public async Task<IActionResult> AddDepartment([FromBody] Department department)
        {
            var newDept = await _repo.AddDepartment(department);
            return CreatedAtAction(nameof(GetDepartment), new { id = newDept.Id }, newDept);
        }

        [HttpGet("filter")]  // Changed from Filtering for consistency
        public async Task<IActionResult> FilterDepartments([FromQuery] string? name)
        {
            var filtered = await _repo.FilterDepartments(name);
            return Ok(filtered);
        }

        // Added new endpoint to get doctors by department
        [HttpGet("{id:int}/doctors")]
        public async Task<IActionResult> GetDoctorsByDepartment(int id)
        {
            // First check if department exists
            var department = await _repo.GetDepartmentById(id);
            if (department == null)
                return NotFound("Department not found");

            var doctors = await _doctorRepository.GetDoctorsByDepartment(id);
            return Ok(doctors);
        }
    }
}