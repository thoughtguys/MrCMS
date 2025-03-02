using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.ContentTemplates.Services;
using Newtonsoft.Json;

namespace MrCMS.ContentTemplates.Helper;

public static class ContentTemplateHtmlHelperExtensions
{
    public static async Task<IHtmlContent> RenderContentTemplate<T>(this IHtmlHelper<T> helper,
        Expression<Func<T, string>> templateMethod, Expression<Func<T, string>> propertiesMethod)
    {
        var model = helper.ViewData.Model;
        if (model == null)
            return HtmlString.Empty;

        var template = templateMethod?.Compile().Invoke(model);
        var properties = propertiesMethod?.Compile().Invoke(model);
        
        Dictionary<string, object> variables = null;
        if (!string.IsNullOrEmpty(properties))
        {
            try
            {
                variables = JsonConvert.DeserializeObject<Dictionary<string, object>>(properties);
            }
            catch
            {
                variables = new Dictionary<string, object>();
            }
        }

        return new HtmlString(await helper.ViewContext.HttpContext.RequestServices
            .GetRequiredService<IContentTemplateRenderer>()
            .RenderAsync(helper, template, variables));
    }


    public static async Task<IHtmlContent> RenderAdminProperties<T>(this IHtmlHelper<T> helper,
        Expression<Func<T, string>> templateMethod, Expression<Func<T, string>> propertiesMethod)
    {
        var model = helper.ViewData.Model;
        if (model == null)
            return HtmlString.Empty;

        var template = templateMethod?.Compile().Invoke(model);
        var properties = propertiesMethod?.Compile().Invoke(model);
        
        Dictionary<string, object> savedProperties = null;
        if (!string.IsNullOrEmpty(properties))
        {
            try
            {
                savedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(properties);
            }
            catch (JsonException ex)
            {
                // Handle JSON deserialization exception
                savedProperties = new Dictionary<string, object>();
                Console.WriteLine($"Deserialization failed: {ex.Message}");
            }
        }

        var renderResult = await helper.ViewContext.HttpContext.RequestServices
            .GetRequiredService<IContentTemplateRenderer>()
            .RenderAdminAsync(helper, template, string.Empty ,savedProperties);
        return new HtmlString("<div data-content-template-container>" + renderResult + "</div>");
    }
}