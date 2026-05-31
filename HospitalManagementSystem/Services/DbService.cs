using System.Data;
using HospitalManagementSystem.Data;
using HospitalManagementSystem.DTOs;
using HospitalManagementSystem.Exceptions;
using HospitalManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagementSystem.Services;

public class DbService(HospitalContext context) : IDbService
{
    private readonly HospitalContext _context = context;

    public async Task<IEnumerable<GetPatientsDto>> GetPatientsAsync(string? search = null)
    {
        var query = _context.Patients
            .Include(e => e.Admissions).ThenInclude(e => e.Ward)
            .Include(e => e.BedAssignments).ThenInclude(e => e.Bed).ThenInclude(e => e.BedType)
            .Include(e => e.BedAssignments).ThenInclude(e => e.Bed).ThenInclude(e => e.Room).ThenInclude(e => e.Ward)
            .AsQueryable();

        if (search != null)
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, $"%{search}%") ||
                EF.Functions.Like(p.LastName, $"%{search}%"));

        var patients = await query.Select(e => new GetPatientsDto
        {
            Pesel = e.Pesel,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Age = e.Age,
            Sex = e.Sex ? "Male" : "Female",
            Admissions = e.Admissions.Select(a => new AdmissionDto
            {
                Id = a.Id,
                AdmissionDate = a.AdmissionDate,
                DischargeDate = a.DischargeDate,
                Ward = new WardDto
                {
                    Id = a.Ward.Id,
                    Name = a.Ward.Name,
                    Description = a.Ward.Description
                }
            }).ToList(),
            BedAssignments = e.BedAssignments.Select(ba => new BedAssignmentDto
            {
                Id = ba.Id,
                From = ba.From,
                To = ba.To,
                Bed = new BedDto
                {
                    Id = ba.Bed.Id,
                    BedType = new BedTypeDto
                    {
                        Id = ba.Bed.BedType.Id,
                        Name = ba.Bed.BedType.Name,
                        Description = ba.Bed.BedType.Description
                    },
                    Room = new RoomDto
                    {
                        Id = ba.Bed.Room.Id,
                        HasTv = ba.Bed.Room.HasTv,
                        Ward = new WardDto
                        {
                            Id = ba.Bed.Room.Ward.Id,
                            Name = ba.Bed.Room.Ward.Name,
                            Description = ba.Bed.Room.Ward.Description
                        }
                    }
                }
            }).ToList()
        }).ToListAsync();

        return patients;
    }

    public async Task AssignPatientToBedAsync(string pesel, AssignPatientToBedDto assignPatientToBedDto)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Pesel == pesel);
        if (patient == null) throw new NotFoundException($"Patient with PESEL '{pesel}' not found.");

        var bedType = await _context.BedTypes
            .FirstOrDefaultAsync(bt => bt.Name == assignPatientToBedDto.BedType);
        if (bedType == null) throw new NotFoundException($"Bed type '{assignPatientToBedDto.BedType}' not found.");

        var ward = await _context.Wards
            .FirstOrDefaultAsync(w => w.Name == assignPatientToBedDto.Ward);
        if (ward == null) throw new NotFoundException($"Ward '{assignPatientToBedDto.Ward}' not found.");

        var availableBed = await _context.Beds
            .Where(e => e.BedTypeId == bedType.Id)
            .Where(e => e.Room.WardId == ward.Id)
            .Where(e => !e.BedAssignments.Any(f =>
                f.From < (assignPatientToBedDto.To ?? DateTime.MaxValue) && (f.To == null || f.To > assignPatientToBedDto.From)))
            .FirstOrDefaultAsync();
        if (availableBed == null)
            throw new NotFoundException(
                $"No available bed of type '{assignPatientToBedDto.BedType}' in ward '{assignPatientToBedDto.Ward}' for the requested period.");

        await _context.BedAssignments.AddAsync(new BedAssignment
        {
            PatientPesel = pesel,
            BedId = availableBed.Id,
            From = assignPatientToBedDto.From,
            To = assignPatientToBedDto.To
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}