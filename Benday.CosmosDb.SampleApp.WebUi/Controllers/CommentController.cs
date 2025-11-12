using Benday.CosmosDb.Repositories;
using Benday.CosmosDb.SampleApp.Api;
using Benday.CosmosDb.SampleApp.Api.DomainModels;
using Benday.CosmosDb.ServiceLayers;
using Microsoft.AspNetCore.Mvc;

namespace Benday.CosmosDb.SampleApp.WebUi.Controllers;

public class CommentController : Controller
{
    private readonly IParentedItemService<Comment> _CommentService;

    public CommentController(IParentedItemService<Comment> commentService)
    {
        _CommentService = commentService;
    }

    // GET: CommentController?noteId={noteId}
    public async Task<ActionResult> Index(string? noteId)
    {
        if (string.IsNullOrEmpty(noteId))
        {
            return BadRequest("Note ID is required.");
        }

        // Fetch comments for this note using GetAllByParentIdAsync
        var comments = await _CommentService.GetAllByParentIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, noteId, "Note");

        // Pass noteId to view so we can add new comments
        ViewData["NoteId"] = noteId;

        return View(comments);
    }

    // GET: CommentController/Details/5?noteId={noteId}
    public async Task<ActionResult> Details(string id, string? noteId)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Id is required.");
        }

        var comment = await _CommentService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (comment == null)
        {
            return NotFound();
        }

        ViewData["NoteId"] = noteId ?? comment.ParentId;

        return View(comment);
    }

    // GET: CommentController/Create?noteId={noteId}
    public ActionResult Create(string? noteId)
    {
        if (string.IsNullOrEmpty(noteId))
        {
            return BadRequest("Note ID is required.");
        }

        ViewData["NoteId"] = noteId;
        return View();
    }

    // POST: CommentController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(Comment comment, string? noteId)
    {
        try
        {
            if (string.IsNullOrEmpty(noteId))
            {
                return BadRequest("Note ID is required.");
            }

            // Set parent information
            comment.ParentId = noteId;
            comment.ParentDiscriminator = "Note";
            comment.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
            comment.Id = Guid.NewGuid().ToString();
            comment.CreatedDate = DateTime.UtcNow;

            await _CommentService.SaveAsync(comment);

            return RedirectToAction(nameof(Index), new { noteId });
        }
        catch
        {
            ViewData["NoteId"] = noteId;
            return View();
        }
    }

    // GET: CommentController/Edit/5?noteId={noteId}
    public async Task<ActionResult> Edit(string id, string? noteId)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Id is required.");
        }

        var comment = await _CommentService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (comment == null)
        {
            return NotFound();
        }

        ViewData["NoteId"] = noteId ?? comment.ParentId;

        return View(comment);
    }

    // POST: CommentController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string id, Comment comment, string? noteId)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Id is required.");
            }

            if (id != comment.Id)
            {
                return BadRequest("Id mismatch.");
            }

            // Ensure parent information is preserved
            if (string.IsNullOrEmpty(comment.ParentId) && !string.IsNullOrEmpty(noteId))
            {
                comment.ParentId = noteId;
            }

            if (string.IsNullOrEmpty(comment.ParentDiscriminator))
            {
                comment.ParentDiscriminator = "Note";
            }

            if (string.IsNullOrEmpty(comment.OwnerId))
            {
                comment.OwnerId = ApiConstants.DEFAULT_OWNER_ID;
            }

            try
            {
                await _CommentService.SaveAsync(comment);

                return RedirectToAction(nameof(Index), new { noteId = comment.ParentId });
            }
            catch (OptimisticConcurrencyException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewData["NoteId"] = noteId ?? comment.ParentId;
                return View(comment);
            }
        }
        catch
        {
            ViewData["NoteId"] = noteId ?? comment.ParentId;
            return View();
        }
    }

    // GET: CommentController/Delete/5?noteId={noteId}
    public async Task<ActionResult> Delete(string id, string? noteId)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Id is required.");
        }

        var comment = await _CommentService.GetByIdAsync(
            ApiConstants.DEFAULT_OWNER_ID, id);

        if (comment == null)
        {
            return NotFound();
        }

        ViewData["NoteId"] = noteId ?? comment.ParentId;

        return View(comment);
    }

    // POST: CommentController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Delete(string id, Comment comment, string? noteId)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Id is required.");
            }

            if (id != comment.Id)
            {
                return BadRequest("Id mismatch.");
            }

            var existing = await _CommentService.GetByIdAsync(
                ApiConstants.DEFAULT_OWNER_ID, id);

            if (existing == null)
            {
                return NotFound();
            }

            await _CommentService.DeleteAsync(existing);

            var parentId = noteId ?? existing.ParentId;
            return RedirectToAction(nameof(Index), new { noteId = parentId });
        }
        catch
        {
            ViewData["NoteId"] = noteId ?? comment.ParentId;
            return View();
        }
    }
}
