using Fitvalle_25.Models;
using Fitvalle_25.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Fitvalle_25.Controllers
{
    public class HomeController : Controller
    {
        private readonly FirebaseDbService _dbService;

        public HomeController(FirebaseDbService dbService)
        {
            _dbService = dbService;
        }

		public IActionResult Index()
		{
			var firebaseResponse = HttpContext.Session.GetString("FirebaseResponse");

			if (string.IsNullOrEmpty(firebaseResponse))
			{
				ViewBag.Message = "No hay sesión iniciada.";
			}
			else
			{
				ViewBag.Message = firebaseResponse;
			}

			return View();

		}
	}
}
