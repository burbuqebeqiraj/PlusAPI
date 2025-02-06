using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace PlusApi.Models.User {
    public class UserRole {
        
		public int UserRoleId { get; set; }
		[StringLength(100)]
		public required string RoleName { get; set; }
		[StringLength(100)]
		public required string DisplayName { get; set; }
		[StringLength(500)]
		public required string RoleDesc { get; set; }
		[DefaultValue(0)]
		public bool IsMigrationData { get; set; }
		public required int AddedBy { get; set; }
		public DateTime DateAdded { get; set; }  = DateTime.Now;
		public DateTime? LastUpdatedDate { get; set; }
		public int? LastUpdatedBy { get; set; }
    }
}