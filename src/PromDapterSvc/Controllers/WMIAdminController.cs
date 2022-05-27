using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PromDapterSvc.Controllers
{
    public class WMIAdminController : Controller
    {
        // GET: WMIAdminController
        [HttpGet]
        [Route("WMIAdmin")]
        //[Route("{filter}")]
        public ActionResult Index(string filter = null)
        {
            return View();
        }

        // GET: WMIAdminController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: WMIAdminController/Create
        [HttpGet]
        [Route("WMIAdmin/Create")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: WMIAdminController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: WMIAdminController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: WMIAdminController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: WMIAdminController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: WMIAdminController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
