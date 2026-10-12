using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.DTOs;
using AcxiomCrm.ViewModels.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers.Api;

[Route("api/leads")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly ApplicationDbContext _context;

    public LeadsApiController(ILeadService leadService, ApplicationDbContext context)
    {
        _leadService = leadService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? source)
    {
        var leads = await _leadService.GetLeadsAsync(search, status, source);
        var dtos = leads.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeadDto>> GetById(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null)
        {
            var exists = await _context.Leads.AnyAsync(l => l.LeadId == id);
            if (exists)
            {
                return Forbid();
            }
            return NotFound(new { message = $"Lead with ID {id} was not found." });
        }

        return Ok(MapToDto(lead));
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create([FromBody] LeadCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var formModel = new LeadFormViewModel
        {
            LeadCode = dto.LeadCode,
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "New" : dto.Status,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = dto.AssignedTo
        };

        var (success, error, createdLead) = await _leadService.CreateAsync(formModel);

        if (!success || createdLead == null)
        {
            return BadRequest(new { message = error ?? "Failed to create lead." });
        }

        var resultDto = MapToDto(createdLead);
        return CreatedAtAction(nameof(GetById), new { id = resultDto.LeadId }, resultDto);
    }

    private static LeadDto MapToDto(Lead l) => new()
    {
        LeadId = l.LeadId,
        LeadCode = l.LeadCode,
        LeadName = l.LeadName,
        Email = l.Email,
        Phone = l.Phone,
        CompanyName = l.CompanyName,
        Source = l.Source,
        Status = l.Status,
        ExpectedValue = l.ExpectedValue,
        CreatedDate = l.CreatedDate,
        AssignedTo = l.AssignedTo
    };
}
