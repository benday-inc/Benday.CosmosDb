using Benday.CosmosDb.SampleApp.WebUi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Benday.CosmosDb.SampleApp.WebUi.Controllers;

[AllowAnonymous]
public class StartupController : Controller
{
    private readonly ServiceStatusChecker _statusChecker;

    public StartupController(ServiceStatusChecker statusChecker)
    {
        _statusChecker = statusChecker;
    }

    public IActionResult Index()
    {
        if (_statusChecker.HasPassed)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Check()
    {
        await _statusChecker.CheckAsync();

        return Json(new
        {
            passed = _statusChecker.HasPassed,
            cosmosDb = new
            {
                reachable = _statusChecker.CosmosDbReachable,
                error = _statusChecker.CosmosDbError
            },
            azurite = new
            {
                reachable = _statusChecker.AzuriteReachable,
                error = _statusChecker.AzuriteError
            }
        });
    }
}
