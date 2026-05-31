using HospitalManagementSystem.DTOs;

namespace HospitalManagementSystem.Services;

public interface IDbService
{
    Task<IEnumerable<GetPatientsDto>> GetPatientsAsync(string? search = null);
    Task AssignPatientToBedAsync(string pesel, AssignPatientToBedDto assignPatientToBedDto);
}