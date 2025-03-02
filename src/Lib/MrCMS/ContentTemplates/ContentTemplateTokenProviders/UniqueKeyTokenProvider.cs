using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.ContentTemplates.Services;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class UniqueKeyTokenProvider(IServiceProvider serviceProvider) : ContentTemplateTokenProvider
{
    public override string Name => "Key";
    public override string Icon => "fa fa-key";
    public override string HtmlPattern => $"[{Name} name=\"Key\"]\n[/{Name}]";
    
    public override string Guide =>
        @"<div class='token-guide mt-3'>
            <h6>Available Variables:</h6>
            <div class='mb-3'>
                Key
            </div>
            <small class='text-muted'>Use these variables in your template with double curly braces, e.g., {{Key}}</small>
            <small class='text-muted d-block mt-2'>Note: Replace 'Key' with the value of the 'name' attribute in your token.</small>
        </div>";
    
    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        if (!attributes.TryGetValue("name", out var name))
            return string.Empty;

        // Create variables dictionary with unique key
        var tokenVariables = new Dictionary<string, object>(variables)
        {
            { name, GenerateUniqueKey() }
        };

        // Render the inner content with the key variable
        var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
        return await renderer.RenderAsync(htmlHelper, innerContent, tokenVariables);
    }
    
    public override async Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
        var innerContentHtml = await RenderInnerContentAsync(
                htmlHelper, innerContent, savedProperties, renderer)
            .ConfigureAwait(false);

        return innerContentHtml;
    }

    private string GenerateUniqueKey()
    {
        // Generate a unique key using Guid and make it URL-friendly
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "")
            .Substring(0, 22); // Trim to 22 characters for cleaner keys
    }
}