using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.Apps;
using MrCMS.ContentTemplates.Services;
using MrCMS.Helpers;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;

public abstract class ContentTemplateTokenProvider
{
    public virtual string Name => GetType().Name.Replace("TemplateTokenProvider", "");
    public virtual string Icon => "fa fa-cog";
    public virtual string HtmlPattern => $"[{Name} name=\"{Name}1\"]";
    public virtual string DisplayName => Name.BreakUpString();
    public virtual string Guide => string.Empty;
    public virtual string ResponsiveClass => "col-12";
    public string NamePrefix { get; set; } = string.Empty;

    public string Category
    {
        get
        {
            var type = GetType();
            if (type.BaseType?.IsGenericType == true &&
                type.BaseType.GetGenericTypeDefinition() == typeof(ContentTemplateTokenProvider<>))
            {
                var appType = type.BaseType.GetGenericArguments().FirstOrDefault();
                if (appType != null)
                {
                    try
                    {
                        // Create instance using Activator
                        if (Activator.CreateInstance(appType) is StandardMrCMSApp app)
                        {
                            return app.Name;
                        }
                    }
                    catch
                    {
                        // If instance creation fails, fallback to General
                        return "General";
                    }
                }
            }

            return "General";
        }
    }


    /// <summary>
    /// Renders the content in the view.
    /// </summary>
    public abstract Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper);

    /// <summary>
    /// Renders the content in the admin view (if needed).
    /// </summary>
    public virtual Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        // By default, do nothing for admin rendering
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Renders the inner content of the token, if any, for the admin interface.
    /// </summary>
    /// <param name="htmlHelper">An instance of IHtmlHelper.</param>
    /// <param name="innerContent">The inner content to render.</param>
    /// <param name="savedProperties">A dictionary of saved properties.</param>
    /// <param name="renderer">An instance of IContentTemplateRenderer.</param>
    /// <returns>A task representing the asynchronous operation, containing the rendered inner content HTML.</returns>
    protected async Task<string> RenderInnerContentAsync(
        IHtmlHelper htmlHelper,
        string innerContent,
        Dictionary<string, object> savedProperties,
        IContentTemplateRenderer renderer)
    {
        if (string.IsNullOrWhiteSpace(innerContent))
        {
            return string.Empty;
        }

        var innerContentHtml = await renderer
            .RenderAdminAsync(htmlHelper, innerContent, NamePrefix, savedProperties)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(innerContentHtml))
        {
            return string.Empty;
        }

        return $@"
            <div class='token-inner-content'>
                {innerContentHtml}
            </div>";
    }

    protected string GetFieldName(string name)
    {
        return $"{NamePrefix}{name}";
    }

    protected string GetFieldId(string name)
    {
        return Regex.Replace(GetFieldName(name), @"[\[\]\.]", "_").Replace("__", "_");
    }

    protected string BuildCardHtml(string header, string content)
    {
        return $@"
        <div class=""card"">
            {(string.IsNullOrWhiteSpace(header) ? string.Empty : $@"<div class=""card-header"">{header}</div>")}
            <div class=""card-body"">
                {content}
            </div>
        </div>";
    }
}

public abstract class ContentTemplateTokenProvider<T> : ContentTemplateTokenProvider where T : StandardMrCMSApp
{
}