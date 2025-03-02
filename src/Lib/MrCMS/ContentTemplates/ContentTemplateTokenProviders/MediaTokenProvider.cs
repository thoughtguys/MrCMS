using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.Helpers;
namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class MediaTokenProvider : ContentTemplateTokenProvider
{
    public override string Name => "Media";
    public override string Icon => "fa fa-picture-o";
    public override string HtmlPattern => $"[{Name} name=\"Media\" width=\"\" height=\"\" class=\"img-fluid\"/]";

    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        if (!attributes.TryGetValue("name", out var name))
            return string.Empty;
        
        string mediaUrl;
        if (variables.TryGetValue(name, out var mediaUrlValue) && mediaUrlValue is string url && !string.IsNullOrWhiteSpace(url))
            mediaUrl = url;
        else
            return string.Empty;


        attributes.TryGetValue("width", out var widthValue);
        attributes.TryGetValue("height", out var heightValue);
        attributes.TryGetValue("class", out var classValue);
        
        var width = TryGetIntVariable(widthValue);
        var height = TryGetIntVariable(heightValue);
        
        var size = default(Size);
        if (width is > 0)
            size = new Size { Width = width.Value };

        if (height is > 0)
            size.Height = height.Value;

    
        var htmlContent = await htmlHelper.RenderImage(mediaUrl, size, attributes: new { @class = classValue }, enableLazyLoading: false);
        await using var writer = new StringWriter();
        htmlContent.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
        return writer.ToString();
    }

    public override Task<string> RenderAdminAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        IHtmlHelper htmlHelper,
        Dictionary<string, object> savedProperties = null)
    {
        if (!attributes.TryGetValue("name", out var name))
            return Task.FromResult(string.Empty);

        var value = string.Empty;

        // Use saved data if available
        if (savedProperties?.TryGetValue(name, out var savedValue) == true)
        {
            value = savedValue?.ToString() ?? string.Empty;
        }
        
        var fieldId = GetFieldId(name);
        var fieldName = GetFieldName(name);

        return Task.FromResult($@"
        <div class='form-group'>
            <label class='form-label d-block' for='{fieldId}'>{name.BreakUpString()}</label>
            <input type='text' 
                   class='form-control' 
                   id='{fieldId}' 
                   name='{fieldName}' 
                   value='{HttpUtility.HtmlEncode(value)}'
                   data-type='media-selector'/>
        </div>");
    }
    
    private int? TryGetIntVariable(string value)
    {
        return int.TryParse(value, out var result)
            ? result
            : null;
    }
}