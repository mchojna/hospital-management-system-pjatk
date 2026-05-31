using HospitalManagementSystem.DTOs;
using HospitalManagementSystem.Exceptions;
using HospitalManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace HospitalManagementSystem.Controller;

[Route("api/patients")]
[ApiController]
public class HospitalController(IDbService service) : ControllerBase
{
    private readonly IDbService _service = service;

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        try
        {
            var result = await _service.GetPatientsAsync(search);
            return Ok(result);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [Route("{pesel}/bedassignments")]
    [HttpPost]
    public async Task<IActionResult> AssignPatientToBed([FromRoute] string pesel, [FromBody] AssignPatientToBedDto assignPatientToBedDto)
    {
        try
        {
            await _service.AssignPatientToBedAsync(pesel, assignPatientToBedDto);
            return Created();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}