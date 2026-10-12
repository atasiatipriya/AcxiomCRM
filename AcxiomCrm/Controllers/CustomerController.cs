using AcxiomCrm.Services;
using AcxiomCrm.ViewModels.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCrm.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var customers = await _customerService.GetCustomersAsync(search, status);
        var viewModel = new CustomerListViewModel
        {
            SearchTerm = search,
            StatusFilter = status,
            Customers = customers
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or access denied.";
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _customerService.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to create customer.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
        {
            TempData["ErrorMessage"] = "Customer not found or access denied.";
            return RedirectToAction(nameof(Index));
        }

        var model = new CustomerFormViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _customerService.UpdateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to update customer.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _customerService.SoftDeleteAsync(id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Failed to deactivate customer.";
        }
        else
        {
            TempData["SuccessMessage"] = "Customer has been deactivated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
