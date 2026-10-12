using AcxiomCrm.Data;
using AcxiomCrm.Models;
using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Customers;
using AcxiomCrm.ViewModels.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCrm.Controllers.Api;

[Route("api/customers")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ApplicationDbContext _context;

    public CustomersApiController(
        ICustomerService customerService,
        ApplicationDbContext context)
    {
        _customerService = customerService;
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] string? search, [FromQuery] string? status)
    {
        var customers = await _customerService.GetCustomersAsync(search, status);
        var dtos = customers.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
        {
            var exists = await _context.Customers.AnyAsync(c => c.CustomerId == id);
            if (exists)
            {
                return Forbid();
            }
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }

        return Ok(MapToDto(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CustomerCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var formModel = new CustomerFormViewModel
        {
            CustomerCode = dto.CustomerCode,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "Active" : dto.Status
        };

        var (success, error, createdCustomer) = await _customerService.CreateAsync(formModel);

        if (!success || createdCustomer == null)
        {
            if (error != null && (error.Contains("already exists", StringComparison.OrdinalIgnoreCase) || error.Contains("unique", StringComparison.OrdinalIgnoreCase)))
            {
                return Conflict(new { message = error });
            }
            return BadRequest(new { message = error ?? "Failed to create customer." });
        }

        var resultDto = MapToDto(createdCustomer);
        return CreatedAtAction(nameof(GetById), new { id = resultDto.CustomerId }, resultDto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] CustomerUpdateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (existing == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }

        if (!await _customerService.CanUserAccessCustomerAsync(id))
        {
            return Forbid();
        }

        var formModel = new CustomerFormViewModel
        {
            CustomerId = id,
            CustomerCode = existing.CustomerCode,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status
        };

        var (success, error) = await _customerService.UpdateAsync(formModel);

        if (!success)
        {
            if (error != null && error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = error });
            }
            return BadRequest(new { message = error ?? "Failed to update customer." });
        }

        var updated = await _customerService.GetByIdAsync(id);
        return Ok(MapToDto(updated!));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
        if (existing == null)
        {
            return NotFound(new { message = $"Customer with ID {id} was not found." });
        }

        if (!await _customerService.CanUserAccessCustomerAsync(id))
        {
            return Forbid();
        }

        var (success, error) = await _customerService.SoftDeleteAsync(id);
        if (!success)
        {
            return BadRequest(new { message = error ?? "Failed to deactivate customer." });
        }

        return Ok(new { message = "Customer deactivated successfully." });
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        CustomerId = c.CustomerId,
        CustomerCode = c.CustomerCode,
        CustomerName = c.CustomerName,
        Email = c.Email,
        Phone = c.Phone,
        CompanyName = c.CompanyName,
        Address = c.Address,
        City = c.City,
        State = c.State,
        Status = c.Status,
        CreatedDate = c.CreatedDate,
        CreatedBy = c.CreatedBy
    };
}
