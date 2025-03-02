using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.Helpers;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class TextAreaTokenProvider: ContentTemplateTokenProvider
{
    public override string Name => "TextArea";
    public override string Icon => "fa fa-file-text-o";
    public override string HtmlPattern => $"[{Name} name=\"TextArea\" defaultValue=\"\" /]";
    
    public override Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        if (!attributes.TryGetValue("name", out var name))
            return Task.FromResult(string.Empty);

        // Check if we have a value in the variables
        if (variables.TryGetValue(name, out var value))
            return Task.FromResult(value?.ToString() ?? string.Empty);

        // Fallback to default value
        return Task.FromResult(attributes.GetValueOrDefault("defaultValue", string.Empty));
    }

    public override Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        if (!attributes.TryGetValue("name", out var name))
            return Task.FromResult(string.Empty);

        var defaultValue = attributes.GetValueOrDefault("defaultValue", string.Empty);
        var value = defaultValue;

        // Use saved data if available
        if (savedProperties?.TryGetValue(name, out var savedValue) == true)
        {
            value = savedValue?.ToString() ?? string.Empty;
        }
        
        var fieldId = GetFieldId(name);
        var fieldName = GetFieldName(name);

        // Use a textarea instead of an input, and add the 'enable-editor' class
        return Task.FromResult($@"
        <div class='form-group'>
            <label for='{fieldId}'>{name.BreakUpString()}</label>
            <textarea 
                   class='form-control enable-editor' 
                   id='{fieldId}' 
                   name='{fieldName}' 
                   data-val='true'
                   data-val-required='The {name.BreakUpString()} field is required.'>{HttpUtility.HtmlEncode(value)}</textarea>
            <span class='text-danger field-validation-valid' 
                  data-valmsg-for='{fieldId}' 
                  data-valmsg-replace='true'></span>
        </div>");
    }
}