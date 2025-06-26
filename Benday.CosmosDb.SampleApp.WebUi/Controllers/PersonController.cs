using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.ServiceLayers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Benday.CosmosDb.SampleApp.WebUi.Controllers;

public class PersonController : Controller
{
    private readonly IPersonService _PersonService;
    private readonly ILogger<PersonController> _Logger;
    public PersonController(
        ILogger<PersonController> logger,
        IPersonService personService)
    {
        _Logger = logger;
        _PersonService = personService;
    }

    // GET: PersonController
    public async Task<ActionResult> Index()
    {
        // Fetch list of persons asynchronously
        var persons = await _PersonService.GetAllAsync(
            ApiConstants.DEFAULT_OWNER_ID);
        return View(persons);
    }

    // GET: PersonController/Details/5
    public async Task<ActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id) == true)
        {
            return BadRequest("Id is required.");
        }

        // Fetch person details asynchronously
        var person = await _PersonService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);


        if (person == null)
        {
            return NotFound();
        }

        return View(person);
    }

    // GET: PersonController/Create
    public ActionResult Create()
    {
        var person = new Person
        {
            Id = Guid.NewGuid().ToString(),
            OwnerId = ApiConstants.DEFAULT_OWNER_ID
        };

        var ticks = DateTime.UtcNow.Ticks;
        person.FirstName = $"FirstName-{ticks}";
        person.LastName = $"LastName-{ticks}";
        person.EmailAddress = $"EmailAddress-{ticks}@example.com";
        person.Timestamp = DateTime.UtcNow;

        return View(person);
    }

    // POST: PersonController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(Person person)
    {
        try
        {
            person.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
            if (string.IsNullOrEmpty(person.Id) == true)
            {
                person.Id = Guid.NewGuid().ToString();
            }

            person.Timestamp = DateTime.UtcNow;

            var json = System.Text.Json.JsonSerializer.Serialize(person);

            _Logger.LogInformation(json);

            // Save person asynchronously
            await _PersonService.SaveAsync(person);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            return View(person);
        }
    }

    // GET: PersonController/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id) == true)
        {
            return BadRequest("Id is required.");
        }

        // Fetch person details asynchronously
        var person = await _PersonService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (person == null)
        {
            return NotFound();
        }

        return View(person);
    }

    // POST: PersonController/Edit/5
    [HttpPost]
    public async Task<ActionResult> Edit(string id, Person person)
    {
        try
        {
            if (string.IsNullOrEmpty(id) == true)
            {
                person.Id = Guid.NewGuid().ToString();
                person.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
            }
            else
            {
                if (string.IsNullOrEmpty(person.Id) == true)
                {
                    person.Id = Guid.NewGuid().ToString();

                    person.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
                }
            }

            try
            {
                await _PersonService.SaveAsync(person);

                return RedirectToAction(nameof(Index));
            }
            catch (OptimisticConcurrencyException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                return View(person);
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            return View();
        }
    }

    // GET: PersonController/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id) == true)
        {
            return BadRequest("Id is required.");
        }

        var person = await _PersonService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID,
            id);

        if (person == null)
        {
            return NotFound();
        }

        return View(person);
    }

    // POST: PersonController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(string id, Person person)
    {
        try
        {
            if (string.IsNullOrEmpty(id) == true)
            {
                return BadRequest("Id is required.");
            }

            if (id != person.Id)
            {
                return BadRequest("Id mismatch.");
            }

            var existing = await _PersonService.GetByIdAsync(
                ApiConstants.DEFAULT_OWNER_ID, id);

            if (existing == null)
            {
                return NotFound();
            }

            await _PersonService.DeleteAsync(existing);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }
}
