using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlusApi.Models;
using PlusApi.Models.User;
using PlusApi.ViewModels.Helper;

namespace PlusApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ISqlService<UserRole> _userRoleRepo;        

        // Constructor
        public UserController(IConfiguration config,
                              AppDbContext context,
                              ISqlService<UserRole> userRoleRepo)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userRoleRepo = userRoleRepo ?? throw new ArgumentNullException(nameof(userRoleRepo));
        }

        //Create new user role
        [HttpPost]
        public async Task<ActionResult> CreateUserRole(UserRole model)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Status = "error", ResponseMsg = "Invalid model data." });
            }

            try
            {
                // Check for duplicate user role asynchronously
                var userRoleExists = await _context.UserRoles
                    .AnyAsync(u => u.RoleName.ToLower() == model.RoleName.ToLower());

                if (userRoleExists)
                {
                    return Conflict(new { Status = "duplicate", ResponseMsg = "This Role Name is already in use." });
                }

                // Start transaction asynchronously
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    model.DateAdded = DateTime.Now;

                    // Insert the new user asynchronously
                    await _userRoleRepo.InsertAsync(model);

                    // Commit transaction asynchronously
                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(CreateUserRole), new { id = model.UserRoleId },
                        new { Status = "success", ResponseMsg = "User Role successfully created." });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while saving the user role." });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An unexpected error occurred." });
            }
        }

        //Select all user roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserRole>>> GetAllRoles()
        {
            try
            {
                var roles = await _userRoleRepo.SelectAllAsync(); // Fetch all roles from the repository
                return Ok(roles);
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while retrieving roles." });
            }
        }

        //Get user role by id
        [HttpGet("{id}")]
        public async Task<ActionResult<UserRole>> GetRoleById(int id)
        {
            var role = await _context.UserRoles.FindAsync(id);
            if (role == null)
                return NotFound(new { message = "Role not found" });

            return role;
        }

        //Delete role by id
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteRole(int id)
        {
            try
            {
                if (id == 1 || id == 2)
                {
                    return Accepted(new { Status = "restricted", ResponseMsg = "Role is restricted." });
                }
                else
                {
                    // Delete the role
                    await _userRoleRepo.DeleteAsync(id);
                    return Ok(new { Status = "success", ResponseMsg = "Role deleted successfully." });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while deleting the role." });
            }
        }

        //Update user role
        [HttpPut("userroles/{id}")]
        public async Task<ActionResult> UpdateUserRole(int id, UserRole model)
        {
            try
            {
                if (id != model.UserRoleId) 
                    return BadRequest(new { Status = "error", ResponseMsg = "Mismatched role ID." });

                var existingRole = await _context.UserRoles.FindAsync(id);
                if (existingRole == null)
                    return NotFound(new { Status = "error", ResponseMsg = "Role not found." });

                // Check for duplicate role name (excluding the current role)
                var duplicateRole = await _context.UserRoles
                    .AnyAsync(opt => opt.RoleName.ToLower() == model.RoleName.ToLower() && opt.UserRoleId != id);
                if (duplicateRole)
                    return Conflict(new { Status = "duplicate", ResponseMsg = "Role name already exists." });

                // Ensure critical roles (ID 1 and 2) are partially editable
                if (id == 1 || id == 2)
                {
                    existingRole.DisplayName = model.DisplayName;
                    existingRole.RoleDesc = model.RoleDesc;
                }
                else
                {
                    existingRole.RoleName = model.RoleName;
                    existingRole.DisplayName = model.DisplayName;
                    existingRole.RoleDesc = model.RoleDesc;
                }

                existingRole.LastUpdatedBy = model.LastUpdatedBy;
                existingRole.LastUpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "success", ResponseMsg = "Role updated successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while updating the role." });
            }
        }

    }
}
