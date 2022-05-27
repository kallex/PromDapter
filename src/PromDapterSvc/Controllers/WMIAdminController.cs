using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
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
            var namespaces = GetWmiNamespaces("root");

            List<(string namespaceName, string className)> fullClassNames =
                new List<(string namespaceName, string className)>();
            foreach (String namespaceName in namespaces)
            {
                var classNames = GetClassNamesWithinWmiNamespace(namespaceName);
                foreach (var className in classNames)
                {
                    Debug.WriteLine($"{namespaceName}:{className}");
                    fullClassNames.Add((namespaceName, className));
                }

            }
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

        private static List<String> GetWmiNamespaces(string root)
        {
            List<String> namespaces = new List<string>();
            try
            {
                ManagementClass nsClass = new ManagementClass(new ManagementScope(root), new ManagementPath("__namespace"), null);
                foreach (ManagementObject ns in nsClass.GetInstances())
                {
                    string namespaceName = root + "\\" + ns["Name"].ToString();
                    namespaces.Add(namespaceName);
                    namespaces.AddRange(GetWmiNamespaces(namespaceName));
                }
            }
            catch (ManagementException me)
            {
                Debug.WriteLine(me.Message);
            }

            return namespaces.OrderBy(s => s).ToList();
        }

        private static List<String> GetClassNamesWithinWmiNamespace(string wmiNamespaceName)
        {
            List<String> classes = new List<string>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
            (new ManagementScope(wmiNamespaceName),
                new WqlObjectQuery("SELECT * FROM meta_class"));
            List<string> classNames = new List<string>();
            ManagementObjectCollection objectCollection = searcher.Get();
            foreach (ManagementClass wmiClass in objectCollection)
            {
                string stringified = wmiClass.ToString();
                string[] parts = stringified.Split(new char[] { ':' });
                classes.Add(parts[1]);
            }
            return classes.OrderBy(s => s).ToList();
        }
    }
}

