using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.Helpers;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class SelectTokenProvider : ContentTemplateTokenProvider
{
    public override string Name => "Select";
    public override string Icon => "fa fa-list";
    public override string HtmlPattern => $"[{Name} name=\"Select\" items=\"item1:Item 1,item2:Item 2\" defaultValue=\"item1\" /]";
    public override string ResponsiveClass => "col-md-6 col-lg-4 col-xl-3";

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
        var selectedValue = defaultValue;

        // If we have saved data, use it
        if (savedProperties?.TryGetValue(name, out var savedValue) == true)
        {
            selectedValue = savedValue?.ToString() ?? string.Empty;
        }

        var items = attributes.GetValueOrDefault("items", string.Empty)
            .Split(',')
            .Select(item => 
            {
                var parts = item.Trim().Split(':');
                return new { Value = parts[0], Text = parts.Length > 1 ? parts[1] : parts[0] };
            });

        var options = string.Join("\n",
            items.Select(item => 
                $"<option value='{HttpUtility.HtmlEncode(item.Value)}' {(item.Value == selectedValue ? "selected" : "")}>{HttpUtility.HtmlEncode(item.Text)}</option>"));

        var fieldId = GetFieldId(name);
        var fieldName = GetFieldName(name);
        
        return Task.FromResult($@"
            <div class='form-group'>
                <label for='{fieldId}'>{name.BreakUpString()}</label>
                <select class='form-control' 
                        id='{fieldId}' 
                        name='{fieldName}'
                        data-val='true'
                        data-val-required='The {name.BreakUpString()} field is required.'>
                    {options}
                </select>
                <span class='text-danger field-validation-valid' 
                      data-valmsg-for='{fieldId}' 
                      data-valmsg-replace='true'></span>
            </div>");
    }
}