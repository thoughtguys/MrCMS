using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.ContentTemplates.Services;
using NCalc;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class ConditionTokenProvider(IServiceProvider serviceProvider) : ContentTemplateTokenProvider
{
    public override string Name => "Condition";
    public override string Icon => "fa fa-terminal";
    public override string HtmlPattern => $"[{Name} if=\"\"][/{Name}]";

    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        if (!attributes.TryGetValue("if", out var condition))
            return string.Empty;

        try
        {
            // Create a new dictionary to hold the modified variables
            var modifiedVariables = new Dictionary<string, object>();

            // Iterate over the original variables
            foreach (var kvp in variables)
            {
                if (!condition.Contains(kvp.Key))
                {
                    // If the variable is not used in the condition, skip it
                    continue;
                }
                
                // Replace dots in the variable name with underscores
                var modifiedKey = kvp.Key.Replace('.', '_');
                modifiedVariables[modifiedKey] = kvp.Value;

                // Replace occurrences of the original variable name in the condition
                condition = condition.Replace(kvp.Key, modifiedKey);
            }

            // Create the expression with the modified condition and variables
            var exp = new Expression(condition)
            {
                Parameters = modifiedVariables
            };

            if (exp.Evaluate() is true)
            {
                var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
                return await renderer.RenderAsync(htmlHelper, innerContent, variables);
            }
        }
        catch
        {
            // If expression evaluation fails, don't render the content
        }

        return string.Empty;
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
}