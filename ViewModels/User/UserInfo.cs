using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PlusApi.ViewModels.User
{
    public class UserInfo{
        public int UserId { get; set; }	

        public int UserRoleId { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }
        
        public string? RoleName { get; set; }

        public string? DisplayName { get; set; }

		public string? FullName { get; set; }

		public string? Mobile { get; set; }

        public string? Address{get;set;}

        public int? AddedBy { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public int? LastUpdatedBy { get; set; }
    }
}