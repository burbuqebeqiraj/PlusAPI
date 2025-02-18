using System.ComponentModel.DataAnnotations;

namespace PlusApi.Models.Department
{
    public class Departments
    {
        [Key]
        public int DepartmentId { get; set; }
        public required string Name { get; set; }
        [StringLength(100)]
        public string? Description { get; set; }
		public bool IsMigrationData { get; set; }
		public int? AddedBy { get; set; }
		public DateTime DateAdded { get; set; }  = DateTime.Now;
		public DateTime? LastUpdatedDate { get; set; }
		public int? LastUpdatedBy { get; set; }
        
        public bool IsActive { get; set; }	
    }
}