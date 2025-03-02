using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.Services.Resources;

namespace MrCMS.ContentTemplates.ContentTemplateTokenProviders;

public class ResourceStringTokenProvider(IStringResourceProvider stringResourceProvider) : ContentTemplateTokenProvider
{
    public override string Name => "ResourceString";
    public override string Icon => "fa fa-language";
    public override string HtmlPattern => $"[{Name} key=\"\" default=\"\" /]";
    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        if (!attributes.TryGetValue("key", out var name))
            return string.Empty;

        attributes.TryGetValue("default", out var defaultValue);

        return await stringResourceProvider.GetValue(name, options => options.SetDefaultValue(defaultValue));
    }
}