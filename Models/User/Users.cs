using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PlusApi.Models.User{
    public class Users{
        [Key]
        public int UserId {get; set;}
        [ForeignKey("UserRole")]
		public required int UserRoleId { get; set; }
		[StringLength(100)]
		public required string FullName { get; set; }
		[StringLength(100)]
		public string? Mobile { get; set; }
		[StringLength(100)]
		public required string Email { get; set; }
		[StringLength(100)]
		public string? Password { get; set; }
		public string? Address {get; set;}
        public bool IsActive { get; set; }	
		[DefaultValue(0)] 
		public bool IsMigrationData { get; set; }
		public int? AddedBy { get; set; }	
		public DateTime DateAdded { get; set; } = DateTime.Now;
		public DateTime? LastUpdatedDate { get; set; }
		public int? LastUpdatedBy { get; set; }
		public int FailedLoginAttempts { get; set; }
		public bool IsLockedOut { get; set; }
		public DateTime? LockoutEndTime { get; set; }
    }
}