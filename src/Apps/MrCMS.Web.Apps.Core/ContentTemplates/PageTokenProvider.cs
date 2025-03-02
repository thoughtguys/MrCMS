using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.ContentTemplates.Services;
using MrCMS.Entities.Documents.Web;
using MrCMS.Web.Apps.Core.Pages;
using NHibernate;

namespace MrCMS.Web.Apps.Core.ContentTemplates;

public class PageTokenProvider(IServiceProvider serviceProvider, ISession session)
    : ContentTemplateTokenProvider<MrCMSCoreApp>
{
    public override string Name => "Page";
    public override string Icon => "fa fa-file-code-o";
    public override string HtmlPattern => $"[{Name} name=\"Page\"]\n[/{Name}]";
    public override string ResponsiveClass => "col-md-6 col-lg-4 col-xl-3";

    public override string Guide =>
        @"<div class='token-guide mt-3'>
            <h6>Available Variables:</h6>
            <div class='mb-3'>
                Page.Id, Page.Name, Page.FeatureImage, Page.PublishOn, Page.Published, Page.Url, Page.Tags
            </div>
            <small class='text-muted'>Use these variables in your template with double curly braces, e.g., {{Page.Name}}</small>
            <small class='text-muted d-block mt-2'>Note: Replace 'Page' with the value of the 'name' attribute in your token.</small>
        </div>";

    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        try
        {
            var name = attributes.GetValueOrDefault("name", "Page");

            var pageId = TryGetIntVariable(variables, $"{name}.PageId", 0);

            var page = await session.GetAsync<Webpage>(pageId);

            if (page == null)
                return string.Empty;
            
            var pageVariables = new Dictionary<string, object>(variables)
            {
                { $"{name}.Id", page.Id },
                { $"{name}.Name", page.Name },
                { $"{name}.FeatureImage", page is TextPage textPage ? textPage.FeatureImage : "" },
                { $"{name}.PublishOn", page.PublishOn },
                { $"{name}.Published", page.Published },
                { $"{name}.Url", $"/{page.UrlSegment}" },
                { $"{name}.Tags", string.Join(", ", page.Tags.Select(t => t.Name)) },
            };

            // Render the inner content with page variables
            var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
            var renderedContent = await renderer.RenderAsync(htmlHelper, innerContent, pageVariables)
                .ConfigureAwait(false);
            
            return renderedContent;
        }
        catch
        {
            // For now, return an empty string
            return string.Empty;
        }
    }

    public override async Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        var name = attributes.GetValueOrDefault("name", "Page");

        var selectedPageId = savedProperties?.GetValueOrDefault($"{name}.PageId")?.ToString() ?? string.Empty;
        Webpage selectedPage = null;
        if (!string.IsNullOrWhiteSpace(selectedPageId))
        {
            selectedPage = await session.GetAsync<Webpage>(int.Parse(selectedPageId));
        }
        
        var fieldId = GetFieldId(name);
        var fieldName = GetFieldName(name);
        var pageHtml = $@"
            <div class='form-group'>
                <label for='{fieldId}_category'>Page</label>
                <select class='form-control' data-webpage-url-selector
                        id='{fieldId}_pageId' 
                        name='{fieldName}.PageId'>
                    {(selectedPage != null ? $"<option value='{selectedPage.Id}' selected>{selectedPage.Name}</option>" : "")}
                </select>
            </div>";

        var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
        var innerContentHtml = await RenderInnerContentAsync(
                htmlHelper, innerContent, savedProperties, renderer)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(innerContentHtml))
        {
            pageHtml += innerContentHtml;
        }
        
        return pageHtml;
    }

    private int TryGetIntVariable(Dictionary<string, object> variables, string key, int defaultValue)
    {
        return variables.TryGetValue(key, out var value) && int.TryParse(value?.ToString(), out var result)
            ? result
            : defaultValue;
    }
}