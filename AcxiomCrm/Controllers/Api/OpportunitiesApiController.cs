using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.DTOs;
using AcxiomCrm.ViewModels.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers.Api;

[Route("api/opportunities")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly ApplicationDbContext _context;

    public OpportunitiesApiController(IOpportunityService opportunityService, ApplicationDbContext context)
    {
        _opportunityService = opportunityService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? stage,
        [FromQuery] string? status)
    {
        var vm = await _opportunityService.GetOpportunitiesAsync(search, stage, status);
        var dtos = vm.Opportunities.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            WeightedValue = o.WeightedValue,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            CreatedDate = o.CreatedDate,
            AssignedTo = o.AssignedTo
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityDto>> GetById(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id);
        if (opp == null)
        {
            var exists = await _context.Opportunities.AnyAsync(o => o.OpportunityId == id);
            if (exists)
            {
                return Forbid();
            }
            return NotFound(new { message = $"Opportunity with ID {id} was not found." });
        }

        var dto = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            CustomerName = opp.Customer?.CustomerName,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            WeightedValue = opp.WeightedValue,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo
        };

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> Create([FromBody] OpportunityCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var formModel = new OpportunityFormViewModel
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Open" : dto.Status,
            AssignedTo = dto.AssignedTo
        };

        var (success, error, createdOpp) = await _opportunityService.CreateAsync(formModel);

        if (!success || createdOpp == null)
        {
            return BadRequest(new { message = error ?? "Failed to create opportunity." });
        }

        var resultDto = new OpportunityDto
        {
            OpportunityId = createdOpp.OpportunityId,
            OpportunityName = createdOpp.OpportunityName,
            CustomerId = createdOpp.CustomerId,
            LeadId = createdOpp.LeadId,
            Amount = createdOpp.Amount,
            Stage = createdOpp.Stage,
            Probability = createdOpp.Probability,
            WeightedValue = createdOpp.WeightedValue,
            ExpectedCloseDate = createdOpp.ExpectedCloseDate,
            Status = createdOpp.Status,
            CreatedDate = createdOpp.CreatedDate,
            AssignedTo = createdOpp.AssignedTo
        };

        return CreatedAtAction(nameof(GetById), new { id = resultDto.OpportunityId }, resultDto);
    }
}
