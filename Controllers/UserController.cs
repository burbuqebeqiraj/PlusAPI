using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlusApi.Models;
using PlusApi.Models.User;
using PlusApi.ViewModels.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PlusApi.ViewModels.Helper;
using Microsoft.AspNetCore.Authorization;

namespace PlusApi.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _context;
        private readonly ISqlService<UserRole> _userRoleRepo;        
        private readonly ISqlService<Users> _userRepo;
        private readonly IPasswordHasher<Users> _passwordHasher;

        // Constructor
        public UserController(IConfiguration config,
                              AppDbContext context,
                              ISqlService<Users> userRepo,
                              ISqlService<UserRole> userRoleRepo,
                              IPasswordHasher<Users> passwordHasher)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userRoleRepo = userRoleRepo ?? throw new ArgumentNullException(nameof(userRoleRepo));
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        }

        //Create new user role
        [HttpPost]
        // [Authorize(Roles = "Admin")] 
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
        // [Authorize(Roles = "Admin")] 
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
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<UserRole>> GetRoleById(int id)
        {
            try
            {
                var role = await _context.UserRoles.FindAsync(id);
                if (role == null)
                    return NotFound(new { message = "Role not found" });

                return role;
            }
            catch(Exception)
            {
               return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while retrieving roles." }); 
            }
        }

        //Delete role by id
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")] 
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
        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> UpdateUserRole(int id, UserRole model)
        {
            try
            {
                if (id != model.UserRoleId) 
                    return BadRequest(new { Status = "error", ResponseMsg = "Mismatched role ID." });

                var existingRole = await _context.UserRoles.FindAsync(id);
                if (existingRole == null)
                    return NotFound(new { Status = "error", ResponseMsg = "Role not found." });

                // Check for duplicate role name
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

        // Create User
        [HttpPost]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult> CreateUser(Users model)
        {
            // Validate the model state
            if (!ModelState.IsValid)
            {
                return BadRequest(new { Status = "error", ResponseMsg = "Invalid model data." });
            }

            try
            {
                // Check for duplicate email asynchronously
                var userExists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());

                if (userExists)
                {
                    return Conflict(new { Status = "duplicate", ResponseMsg = "This email is already in use." });
                }

                // Check if the password is provided and validate it
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    if (model.Password.Length < 8 || 
                        !model.Password.Any(char.IsLetter) ||
                        !model.Password.Any(char.IsDigit) ||
                        !model.Password.Any(ch => !char.IsLetterOrDigit(ch)))
                    {
                        return BadRequest(new { Status = "error", ResponseMsg = "Password must be at least 8 characters long and contain at least one letter, one number, and one special character." });
                    }

                    model.Password = _passwordHasher.HashPassword(model, model.Password);
                }
                else
                {
                    return BadRequest(new { Status = "error", ResponseMsg = "Password must define" });   
                }

                // Start transaction asynchronously
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    model.DateAdded = DateTime.Now;
                    model.IsActive = true;

                    // Insert the new user asynchronously
                    await _userRepo.InsertAsync(model);

                    // Commit transaction asynchronously
                    await transaction.CommitAsync();

                    return CreatedAtAction(nameof(CreateUser), new { id = model.UserId },
                        new { Status = "success", ResponseMsg = "User successfully created." });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while saving the user." });
                }
            }
            catch (Exception)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An unexpected error occurred." });
            }
        }
        
        //Get list of users
        [HttpGet]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<IEnumerable<Users>>> GetUserList()
        {
            try
            {
                var userList = await (from u in _context.Users
                                    join r in _context.UserRoles 
                                    on u.UserRoleId equals r.UserRoleId
                                    select new
                                    {
                                        UserId = u.UserId,
                                        UserRoleId = u.UserRoleId,
                                        FullName = u.FullName,
                                        RoleName = r.RoleName,
                                        DisplayName = r.DisplayName,
                                        Mobile = u.Mobile,
                                        Email = u.Email,
                                        Address = u.Address,
                                        IsActive = u.IsActive
                                    }).ToListAsync();

                return Ok(userList);
            }
            catch (Exception ex)
            {
                // Detailed error response
                return StatusCode(500, new { Status = "error", Message = ex.Message });
            }
        }

        //Get user by id
        [HttpGet("{id}")]
        // [Authorize(Roles = "Admin")] 
        public async Task<ActionResult<Users>> GetSingleUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                return user;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", Message = ex.Message });
            }
        }

        //Update User
        [HttpPut]
         // [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateUser(Users model)
        {
            try
            {
                var objUser = await _context.Users.SingleOrDefaultAsync(u => u.UserId == model.UserId);

                if (objUser == null)
                {
                    return NotFound(new { Status = "error", ResponseMsg = "User not found." });
                }

                objUser.UserRoleId = model.UserRoleId;
                objUser.FullName = model.FullName;
                objUser.Mobile = model.Mobile;
                objUser.Email = model.Email;
                objUser.Address = model.Address;

                // Update password if a new password is provided
                if (!string.IsNullOrEmpty(model.Password))
                {
                    objUser.Password = _passwordHasher.HashPassword(objUser, model.Password);
                }

                objUser.LastUpdatedBy = model.LastUpdatedBy;
                objUser.LastUpdatedDate = DateTime.Now;
                objUser.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "success", ResponseMsg = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while updating the user: " + ex.Message });
            }
        }

        //Delete Single User
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSingleUser(int id)
        {
            try
            {
                // Retrieve the user asynchronously by id
                var user = await _context.Users.FirstOrDefaultAsync(q => q.UserId == id);
                if (user == null)
                {
                    return NotFound(new { Status = "error", ResponseMsg = "User not found" });
                }
                // If the user is marked as migration data, deletion is restricted
                if (user.IsMigrationData)
                {
                    return Conflict(new { Status = "error", ResponseMsg = "Not allowed to delete this user." });
                }

                // Delete the user asynchronously using the repository
                await _userRepo.DeleteAsync(id);

                return Ok(new { Status = "success", ResponseMsg = "User successfully deleted." });
            }
            catch (Exception ex)
            {
                // Return a 500 error with a generic message if an exception occurs
                return StatusCode(500, new { Status = "error", ResponseMsg = ex.Message });
            }
        }

        [HttpPut]
        // [Authorize(Roles = "Admin", "User")]
        public async Task<ActionResult> ChangeUserPassword(UserInfo model)
        {
            try
            {
                // Retrieve the user from the database using the UserId from the model
                var objUser = await _context.Users.SingleOrDefaultAsync(u => u.UserId == model.UserId);
                if (objUser == null)
                {
                    return NotFound(new { Status = "error", ResponseMsg = "User not found." });
                }

                // Validate the password
                if (string.IsNullOrWhiteSpace(model.Password) ||
                    model.Password.Length < 8 ||
                    !model.Password.Any(char.IsLetter) ||
                    !model.Password.Any(char.IsDigit) ||
                    !model.Password.Any(ch => !char.IsLetterOrDigit(ch)))
                {
                    return BadRequest(new { Status = "error", ResponseMsg = "Password must be at least 8 characters long and contain at least one letter, one number, and one special character." });
                }

                // Hash the new password before saving (use the correct model type objUser)
                objUser.Password = _passwordHasher.HashPassword(objUser, model.Password);

                // Save the changes to the database
                await _context.SaveChangesAsync();

                return Ok(new { Status = "success", ResponseMsg = "Password updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred while updating the password.", Details = ex.Message });
            }
        }

        ///<summary>
        ///Get Log in Detail
        ///</summary>
        [AllowAnonymous]
        [HttpGet("{email}/{password}")]
        public async Task<ActionResult> GetLoginInfo(string email, string password)
        {
            try
            {
                var user = await (from u in _context.Users
                                join r in _context.UserRoles on u.UserRoleId equals r.UserRoleId
                                where u.IsActive && u.Email == email
                                select new
                                {
                                    u.UserId,
                                    r.UserRoleId,
                                    r.RoleName,
                                    r.DisplayName,
                                    u.FullName,
                                    u.Mobile,
                                    u.Email,
                                    u.Address,
                                    u.AddedBy
                                }).FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { Status = "error", ResponseMsg = "User not found." });
                }

                // Verify password
                var userEntity = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (userEntity == null || _passwordHasher.VerifyHashedPassword(userEntity, userEntity.Password, password) != PasswordVerificationResult.Success)
                {
                    return Unauthorized(new { Status = "error", ResponseMsg = "Invalid credentials." });
                }

                // Map user data
                var userInfo = new UserInfo
                {
                    UserId = user.UserId,
                    UserRoleId = user.UserRoleId,
                    RoleName = user.RoleName,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    FullName = user.FullName,
                    Mobile = user.Mobile,
                    Address = user.Address,
                    AddedBy = user.AddedBy
                };

                // Generate JWT token
                var token = GenerateJwtToken(userInfo);

                return Ok(new LogInResponse { Token = token, Obj = userInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = "error", ResponseMsg = "An error occurred.", Details = ex.Message });
            }
        }

        string GenerateJwtToken(UserInfo userInfo)
        {
            var securityKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:SecretKey"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.UserId.ToString()),
                new Claim("fullName", userInfo.FullName.ToString()),
                new Claim("role", userInfo.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(360),
                signingCredentials: credentials
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
