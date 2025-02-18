using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlusApi.Models;
using PlusApi.Models.Department;

namespace PlusApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class DepartmentController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ISqlService<Departments> _department;      
        public DepartmentController(IConfiguration config,
                                    AppDbContext context,
                                    ISqlService<Departments> department)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _department = department ?? throw new ArgumentNullException(nameof(department));
        }

        [HttpPost]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> CreateDepartment(Departments department)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(new { Status = "error", ResponseMsg = "Invalid model data."});
            }

            try
            {
                var departmentExists = await _context.Departments
                    .AnyAsync(d => d.Name.ToLower() == department.Name.ToLower());
                
                if (departmentExists)
                {
                    return Conflict(new { Status = "duplicate", ResponseMsg = "This Department Name is already in use." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    department.DateAdded = DateTime.Now;
                    department.IsActive = true;

                    await _department.InsertAsync(department);

                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(CreateDepartment), new { id = department.DepartmentId },
                        new { Status = "success", ResponseMsg = "Department successfully created." });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while saving the department." });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An unexpected error occurred." });
            }
        }

        [HttpGet]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<Departments>>> GetDepartmentList()
        {
            try
            {
                var departmentList = await (from d in _context.Departments
                                    join r in _context.Departments 
                                    on d.DepartmentId equals r.DepartmentId
                                    select new
                                    {
                                        DepartmentID = d.DepartmentId,
                                        UserRoleId = d.Name,
                                        FullName = d.Description,
                                    }).ToListAsync();

                return Ok(departmentList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<Departments>> GetSingleUser(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                    return NotFound(new { message = "Department not found" });

                return department;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", Message = ex.Message });
            }
        }

        [HttpPut]
         // [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateDepartment(Departments model)
        {
            try
            {
                var objDepartment = await _context.Departments.SingleOrDefaultAsync(d => d.DepartmentId == model.DepartmentId);

                if (objDepartment == null)
                {
                    return NotFound(new { Status = "error", ResponseMsg = "Department not found." });
                }

                objDepartment.DepartmentId = model.DepartmentId;
                objDepartment.Name = model.Name;
                objDepartment.Description = model.Description;
                objDepartment.LastUpdatedBy = model.LastUpdatedBy;
                objDepartment.LastUpdatedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "success", ResponseMsg = "Department updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while updating the department " + ex.Message });
            }
        }

        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSingleDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);
                if (department == null)
                {
                    return NotFound(new { Status = "error", ResponseMsg = "Department not found" });
                }

                if (department.IsMigrationData)
                {
                    return Conflict(new { Status = "error", ResponseMsg = "Not allowed to delete this user." });
                }

                await _department.DeleteAsync(id);

                return Ok(new { Status = "success", ResponseMsg = "Department successfully deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = ex.Message });
            }
        }

    }
}