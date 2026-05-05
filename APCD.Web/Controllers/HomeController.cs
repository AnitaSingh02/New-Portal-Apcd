using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using APCD.Web.Models;

namespace APCD.Web.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _env;

    public HomeController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Index()
    {
        var sliderDirPath = Path.Combine(_env.WebRootPath, "images", "SliderImages");
        var images = new List<string>();
        
        if (Directory.Exists(sliderDirPath))
        {
            images = Directory.GetFiles(sliderDirPath)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .Select(f => "/images/SliderImages/" + Path.GetFileName(f))
                .OrderBy(f => f) // Ensure consistent order
                .ToList();
        }
        
        ViewBag.SliderImages = images;
        return View();
    }

    public IActionResult AboutScheme()
    {
        return View();
    }

    public IActionResult AboutNPC()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
