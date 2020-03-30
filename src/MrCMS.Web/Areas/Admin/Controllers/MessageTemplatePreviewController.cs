﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MrCMS.Web.Areas.Admin.Services;
using MrCMS.Website.Controllers;

namespace MrCMS.Web.Areas.Admin.Controllers
{
    public class MessageTemplatePreviewController : MrCMSAdminController
    {
        private readonly IMessageTemplatePreviewService _messageTemplatePreviewService;

        public MessageTemplatePreviewController(IMessageTemplatePreviewService messageTemplatePreviewService)
        {
            _messageTemplatePreviewService = messageTemplatePreviewService;
        }

        public async Task<ViewResult> Get(string type)
        {
            return View(await _messageTemplatePreviewService.GetTemplate(type));
        }

        public async Task<PartialViewResult> Preview(string type, int id)
        {
            return PartialView(await _messageTemplatePreviewService.GetPreview(type, id));
        }
    }
}