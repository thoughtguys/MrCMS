using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.Entities.Documents.Web;
using MrCMS.Helpers;
using NHibernate;
using NHibernate.Linq;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class FormRenderTokenProvider(ISession session) : ContentTemplateTokenProvider
{
    public override string Name => "Form";
    public override string Icon => "fa fa-wpforms";
    public override string HtmlPattern => $"[{Name} name=\"Form\" /]";
    public override string ResponsiveClass => "col-md-6 col-lg-4 col-xl-3";

    public override Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        var name = attributes.GetValueOrDefault("name", "Form");
        
        if (variables.TryGetValue($"{name}", out var value) && int.TryParse(value?.ToString(), out var formId))
        {
            return Task.FromResult(htmlHelper.ParseShortcodes($"[form id=\"{formId}\"]").ToString());

        }

        return Task.FromResult(string.Empty);
    }
    
    public override async Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        if (!attributes.TryGetValue("name", out var name))
            return string.Empty;
        
        string selectedValue = null;

        // If we have saved data, use it
        if (savedProperties?.TryGetValue(name, out var savedValue) == true)
        {
            selectedValue = savedValue?.ToString() ?? string.Empty;
        }

        var formOptions = await session.Query<Form>().Select(f => new { f.Id, f.Name }).ToListAsync();

        var options = string.Join("\n",
            formOptions.Select(item => 
                $"<option value='{HttpUtility.HtmlEncode(item.Id)}' {(item.Id.ToString() == selectedValue ? "selected" : "")}>{HttpUtility.HtmlEncode(item.Name)}</option>"));
        var fieldId = GetFieldId(name);
        var fieldName = GetFieldName(name);
        return $@"
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
            </div>";
    }
}