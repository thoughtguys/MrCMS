using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MrCMS.ContentTemplates.Services;

public interface IContentTemplateRenderer
{
    Task<string> RenderAsync(IHtmlHelper htmlHelper, string template,
        Dictionary<string, object> variables = null);

    Task<string> RenderAdminAsync(IHtmlHelper htmlHelper, string template, string namePrefix = null, Dictionary<string, object> savedProperties = null);
}