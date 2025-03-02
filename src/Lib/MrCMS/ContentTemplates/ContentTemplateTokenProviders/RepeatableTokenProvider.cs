using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.ContentTemplates.Services;
using MrCMS.Helpers;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class RepeatableTokenProvider(IServiceProvider serviceProvider) : ContentTemplateTokenProvider
{
    public override string Name => "Repeatable";
    public override string Icon => "fa fa-retweet";
    public override string HtmlPattern => $"[{Name} name=\"Repeatable\"]\n[/{Name}]";

    public override string Guide =>
        @"<div class='token-guide mt-3'>
            <h6>Available Variables:</h6>
            <div class='mb-3'>
                Repeatable.Index
            </div>
            <small class='text-muted'>Use these variables in your template with double curly braces, e.g., {{Repeatable.Index}}</small>
            <small class='text-muted d-block mt-2'>Note: Replace 'Repeatable' with the value of the 'name' attribute in your token.</small>
        </div>";

    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        if (!attributes.TryGetValue("name", out var name))
            return string.Empty;

        var result = new StringBuilder();

        // Check if we have array data in the variables
        if (variables.TryGetValue(name, out var arrayData) && arrayData is string arrayJson)
        {
            try
            {
                // Deserialize the JSON array into a list of dictionaries
                var items = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(arrayJson);

                var index = 0;
                foreach (var item in items)
                {
                    // Create a variables dictionary for this item
                    var itemVariables = new Dictionary<string, object>(variables);

                    // Add each field from the dictionary to the variables with the Array prefix
                    foreach (var kvp in item)
                    {
                        itemVariables[$"{kvp.Key}"] = kvp.Value;
                    }
                    
                    // Add the index to the variables
                    itemVariables[$"{name}.Index"] = index++;

                    // Render the inner content with the item's variables
                    var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
                    var renderedContent = await renderer.RenderAsync(htmlHelper, innerContent, itemVariables);
                    result.Append(renderedContent);
                }
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        return result.ToString();
    }

    public override async Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        if (!attributes.TryGetValue("name", out var name))
            return string.Empty;
        
        var originalNamePrefix = $"{NamePrefix}";

        var variables = savedProperties ?? new Dictionary<string, object>();
        var existingItemsHtml = new StringBuilder();
        // Check if we have array data in the variables
        if (variables.TryGetValue(name, out var arrayData) && arrayData is string arrayJson)
        {
            try
            {
                // Deserialize the JSON array into a list of dictionaries
                var items = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(arrayJson);

                var index = 0;
                foreach (var item in items)
                {
                    // Create a variables dictionary for this item
                    var itemVariables = new Dictionary<string, object>(variables);

                    // Add each field from the dictionary to the variables with the Array prefix
                    foreach (var kvp in item)
                    {
                        itemVariables[$"{kvp.Key}"] = kvp.Value;
                    }

                    //Set the name prefix for the item
                    NamePrefix = $"{originalNamePrefix}{name}[{index}].";
                    // Render the inner content with the item's variables
                    var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
                    var renderedContent =
                        await RenderInnerContentAsync(htmlHelper, innerContent, itemVariables, renderer);

                    existingItemsHtml.Append($@"
                    <div class='repeatable-item mb-3 border-bottom pb-3' data-index='{index}'>
                        <div class='repeatable-item-content'>
                            {renderedContent}
                        </div>
                        <button type='button' class='btn btn-danger btn-sm remove-repeatable-item'>
                            <i class='fa fa-trash'></i>
                        </button>
                    </div>");

                    index++;
                }
            }
            catch (JsonException)
            {
                NamePrefix = originalNamePrefix;
                return string.Empty;
            }
        }
        
        var contentTemplateRenderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
        NamePrefix = $"{originalNamePrefix}{name}.template.";
        var template = UpdateNameAttributes(await RenderInnerContentAsync(htmlHelper, innerContent, savedProperties, contentTemplateRenderer), NamePrefix);
        
        var repeatableHtml = $@"
            <div class='repeatable-token' data-repeatable-name='{name}'>
                <div class='repeatable-items-container'>
                   {existingItemsHtml}
                </div>
                <div class='repeatable-input-area'>
                    {template}
                </div>
                <button type='button' class='btn btn-primary btn-sm add-repeatable-item'>
                    <i class='fa fa-plus'></i> Add
                </button>
            </div>";
        
        NamePrefix = originalNamePrefix;
        
        return BuildCardHtml(name.BreakUpString(), repeatableHtml);;
    }
    
    private string UpdateNameAttributes(string htmlContent, string namePrefix)
    {
        // Pattern to match 'name' attributes starting with the prefix
        string pattern = $@"name\s*=\s*(['""]){Regex.Escape(namePrefix)}(.*?)\1";

        // Evaluator to replace 'name' with 'field-name' and remove the prefix
        string result = Regex.Replace(htmlContent, pattern, m => {
            var quote = m.Groups[1].Value;
            var value = m.Groups[2].Value;
            return $"data-field-name={quote}{value}{quote}";
        }, RegexOptions.IgnoreCase);

        return result;
    }
}