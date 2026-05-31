using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.DTOs;

public class AssignPatientToBedDto
{
    [Required] public DateTime From { get; set; }
    public DateTime? To { get; set; }
    [Required] public string BedType { get; set; } = string.Empty;
    [Required] public string Ward { get; set; } = string.Empty;
}