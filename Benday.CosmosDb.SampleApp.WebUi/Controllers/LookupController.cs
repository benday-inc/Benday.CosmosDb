using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.ServiceLayers;
using Benday.CosmosDb.ServiceLayers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Benday.CosmosDb.SampleApp.WebUi.Controllers;

public class LookupController : Controller
{
    private readonly ILookupValueService _LookupValueService;

    public LookupController(ILookupValueService lookupValueService)
    {
        _LookupValueService = lookupValueService;
    }

    private static readonly List<SelectListItem> CategoryOptions = new()
    {
        new SelectListItem("Status", "Status"),
        new SelectListItem("Priority", "Priority"),
        new SelectListItem("Type", "Type"),
        new SelectListItem("Department", "Department"),
        new SelectListItem("Region", "Region")
    };

    private void PopulateCategoryList(string? selectedValue = null)
    {
        ViewBag.CategoryList = new SelectList(CategoryOptions, "Value", "Text", selectedValue);
    }

    // GET: LookupController
    public async Task<ActionResult> Index()
    {
        var lookupValues = await _LookupValueService.GetAllAsync(
            ApiConstants.DEFAULT_OWNER_ID);
        return View(lookupValues);
    }

    // GET: LookupController/Details/5
    public async Task<ActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Id is required.");
        }

        var lookupValue = await _LookupValueService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (lookupValue == null)
        {
            return NotFound();
        }

        return View(lookupValue);
    }

    // GET: LookupController/Create
    public ActionResult Create()
    {
        PopulateCategoryList();
        return View();
    }

    // POST: LookupController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(LookupValue lookupValue)
    {
        try
        {
            await _LookupValueService.SaveAsync(lookupValue);
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            PopulateCategoryList(lookupValue.Category);
            return View(lookupValue);
        }
    }

    // GET: LookupController/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Id is required.");
        }

        var lookupValue = await _LookupValueService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (lookupValue == null)
        {
            return NotFound();
        }

        PopulateCategoryList(lookupValue.Category);
        return View(lookupValue);
    }

    // POST: LookupController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string id, LookupValue lookupValue)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                lookupValue.Id = Guid.NewGuid().ToString();
                lookupValue.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
            }
            else
            {
                if (string.IsNullOrEmpty(lookupValue.Id))
                {
                    lookupValue.Id = Guid.NewGuid().ToString();
                    lookupValue.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
                }
            }

            try
            {
                await _LookupValueService.SaveAsync(lookupValue);
                return RedirectToAction(nameof(Index));
            }
            catch (OptimisticConcurrencyException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                PopulateCategoryList(lookupValue.Category);
                return View(lookupValue);
            }
        }
        catch
        {
            PopulateCategoryList(lookupValue.Category);
            return View(lookupValue);
        }
    }

    // GET: LookupController/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Id is required.");
        }

        var lookupValue = await _LookupValueService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (lookupValue == null)
        {
            return NotFound();
        }

        return View(lookupValue);
    }

    // POST: LookupController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(string id, LookupValue lookupValue)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Id is required.");
            }

            if (id != lookupValue.Id)
            {
                return BadRequest("Id mismatch.");
            }

            var existing = await _LookupValueService.GetByIdAsync(
                ApiConstants.DEFAULT_OWNER_ID, id);

            if (existing == null)
            {
                return NotFound();
            }

            await _LookupValueService.DeleteAsync(existing);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View(lookupValue);
        }
    }
}
