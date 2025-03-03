using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.SampleApp.Api.ServiceLayers;
using Benday.CosmosDb.ServiceLayers;
using Microsoft.AspNetCore.Mvc;

namespace Benday.CosmosDb.SampleApp.WebUi.Controllers;

public class NoteController : Controller
{
    private readonly IOwnedItemService<Note> _NoteService;
    public NoteController(IOwnedItemService<Note> noteService)
    {
        _NoteService = noteService;
    }

    // GET: NoteController
    public async Task<ActionResult> Index()
    {
        // Fetch list of Notes asynchronously
        var notes = await _NoteService.GetAllAsync(
            ApiConstants.DEFAULT_OWNER_ID);
        return View(notes);
    }

    // GET: NoteController/Details/5
    public async Task<ActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id) == true)
        {
            return BadRequest("Id is required.");
        }

        // Fetch note details asynchronously
        var note = await _NoteService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);


        if (note == null)
        {
            return NotFound();
        }

        return View(note);
    }

    // GET: NoteController/Create
    public ActionResult Create()
    {
        return View();
    }

    // POST: NoteController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(Note note)
    {
        try
        {
            // Save note asynchronously
            await _NoteService.SaveAsync(note);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }

    // GET: NoteController/Edit/5
    public async Task<ActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id) == true)
        {
            return BadRequest("Id is required.");
        }

        // Fetch note details asynchronously
        var note = await _NoteService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (note == null)
        {
            return NotFound();
        }

        return View(note);
    }

    // POST: NoteController/Edit/5
    [HttpPost]
    public async Task<ActionResult> Edit(string id, Note note)
    {
        try
        {
            if (string.IsNullOrEmpty(id) == true)
            {
                note.Id = Guid.NewGuid().ToString();
                note.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
            }
            else
            {
                if (string.IsNullOrEmpty(note.Id) == true)
                {
                    note.Id = Guid.NewGuid().ToString();

                    note.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
                }
            }

            try
            {
                await _NoteService.SaveAsync(note);

                return RedirectToAction(nameof(Index));
            }
            catch (OptimisticConcurrencyException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                return View(note);
            }
        }
        catch
        {
            return View();
        }
    }

    // GET: NoteController/Delete/5
    public async Task<ActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id) == true)
        {
            return BadRequest("Id is required.");
        }

        var note = await _NoteService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID,
            id);

        if (note == null)
        {
            return NotFound();
        }

        return View(note);
    }

    // POST: NoteController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(string id, Note note)
    {
        try
        {
            if (string.IsNullOrEmpty(id) == true)
            {
                return BadRequest("Id is required.");
            }

            if (id != note.Id)
            {
                return BadRequest("Id mismatch.");
            }

            var existing = await _NoteService.GetByIdAsync(
                ApiConstants.DEFAULT_OWNER_ID, id);

            if (existing == null)
            {
                return NotFound();
            }

            await _NoteService.DeleteAsync(existing);

            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }
}
