using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MrCMS.Models;
using MrCMS.Services;
using MrCMS.Web.Admin.ACL;
using MrCMS.Web.Admin.ModelBinders;
using MrCMS.Web.Admin.Models;
using MrCMS.Web.Admin.Services;
using MrCMS.Web.Admin.Infrastructure.BaseControllers;
using MrCMS.Web.Admin.Infrastructure.Helpers;
using MrCMS.Website;

namespace MrCMS.Web.Admin.Controllers
{
    public class MediaCategoryController : MrCMSAdminController
    {
        private readonly IMediaCategoryAdminService _mediaCategoryAdminService;
        private readonly IFileAdminService _fileAdminService;
        private readonly ICurrentSiteLocator _currentSiteLocator;

        public MediaCategoryController(IMediaCategoryAdminService mediaCategoryAdminService,
            IFileAdminService fileAdminService, ICurrentSiteLocator currentSiteLocator)
        {
            _mediaCategoryAdminService = mediaCategoryAdminService;
            _fileAdminService = fileAdminService;
            _currentSiteLocator = currentSiteLocator;
        }

        [HttpGet, ActionName("Add")]
        public async Task<ViewResult> Add_Get(int? id)
        {
            //Build list 
            var model = _mediaCategoryAdminService.GetNewCategoryModel(id);
            ViewData["parent"] = await _mediaCategoryAdminService.GetCategory(id);

            return View(model);
        }

        [HttpPost]
        public async Task<RedirectToActionResult> Add(AddMediaCategoryModel model)
        {
            var canAdd = await _mediaCategoryAdminService.CanAdd(model);
            if (!canAdd.Success)
            {
                TempData.AddErrorMessage(canAdd.ErrorMessage);
                return RedirectToAction("Add", new { id = model.ParentId });
            }

            var doc = await _mediaCategoryAdminService.Add(model);
            TempData.AddSuccessMessage($"{doc.Name} successfully added");                                    
            return RedirectToAction("Show", new { id = (int?)doc.Id });
        }

        [HttpGet, ActionName("Edit")]
        public async Task<ViewResult> Edit_Get(int id)
        {
            return View(await _mediaCategoryAdminService.GetEditModel(id));
        }

        [HttpPost]
        public async Task<RedirectToActionResult> Edit(UpdateMediaCategoryModel model)
        {
            var category = await _mediaCategoryAdminService.Update(model);
            TempData.AddSuccessMessage($"{category.Name} successfully saved");
            return RedirectToAction("Show", new { id = category.Id });
        }

        [HttpGet, ActionName("Delete")]
        public async Task<ActionResult> Delete_Get(int id)
        {
            return PartialView(await _mediaCategoryAdminService.GetEditModel(id));
        }

        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            var category = await _mediaCategoryAdminService.Delete(id);
            TempData.AddInfoMessage($"{category.Name} deleted");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Sort(int id)
        {
            List<SortItem> sortItems = await _mediaCategoryAdminService.GetSortItems(id);

            return View(sortItems);
        }

        [HttpPost]
        public async Task<ActionResult> Sort(int id, List<SortItem> items)
        {
            await _mediaCategoryAdminService.SetOrders(items);
            return RedirectToAction("Sort", new { id });
        }

        public async Task<ActionResult> Show(MediaCategorySearchModel searchModel)
        {
            if (searchModel == null || searchModel.Id == null)
                return RedirectToAction("Index");
            ViewData["category"] = await _fileAdminService.GetCategory(searchModel);

            return View(searchModel);
        }

        public ViewResult Index(MediaCategorySearchModel searchModel)
        {
            return View(searchModel);
        }

        [HttpGet]
        public async Task<ActionResult> ShowFilesSimple(int id)
        {
            var category = await _mediaCategoryAdminService.Get(id);
            return PartialView(category);
        }


        [HttpGet]
        public async Task<ActionResult> SortFiles(int id)
        {
            ViewData["categoryId"] = id;
            var category = await _mediaCategoryAdminService.Get(id);
            IList<ImageSortItem> sortItems = await _fileAdminService.GetFilesToSort(category);

            return View(sortItems);
        }

        [HttpPost]
        public async Task<ActionResult> SortFiles(int? id, List<SortItem> items)
        {
            await _fileAdminService.SetOrders(items);
            return RedirectToAction("SortFiles", new { id });
        }

        /// <summary>
        ///     Finds out if the URL entered is valid.
        /// </summary>
        /// <param name="path">The URL Segment entered</param>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult> ValidateUrlIsAllowed(string path, int? id)
        {
            return !await _mediaCategoryAdminService.UrlIsValidForMediaCategory(_currentSiteLocator.GetCurrentSite().Id,
                path, id)
                ? Json("Please choose a different Path as this one is already used.")
                : Json(true);
        }

        [Acl(typeof(MediaToolsACL), MediaToolsACL.Cut)]
        public async Task<JsonResult> MoveFilesAndFolders(
            [ModelBinder(typeof(MoveFilesModelBinder))]
            MoveFilesAndFoldersModel model)
        {
            await _fileAdminService.MoveFiles(model.Files, model.Folder);
            string message = await _fileAdminService.MoveFolders(model.Folders, model.Folder);
            return Json(new FormActionResult { success = true, message = message });
        }

        [Acl(typeof(MediaToolsACL), MediaToolsACL.Delete)]
        public async Task<JsonResult> DeleteFilesAndFolders(
            [ModelBinder(typeof(DeleteFilesModelBinder))]
            DeleteFilesAndFoldersModel model)
        {
            await _fileAdminService.DeleteFilesHard(model.Files);
            await _fileAdminService.DeleteFoldersHard(model.Folders);

            return Json(new FormActionResult { success = true, message = "" });
        }

        public ActionResult Directory(MediaCategorySearchModel searchModel)
        {
            return PartialView(searchModel);
        }
    }
}