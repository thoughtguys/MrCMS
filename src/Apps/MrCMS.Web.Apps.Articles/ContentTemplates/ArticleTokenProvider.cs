using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;
using MrCMS.ContentTemplates.Services;
using MrCMS.Entities.Documents;
using MrCMS.Helpers;
using MrCMS.Web.Apps.Articles.Pages;
using NHibernate;
using NHibernate.Linq;

namespace MrCMS.Web.Apps.Articles.ContentTemplates;

public class ArticleTokenProvider(IServiceProvider serviceProvider, ISession session)
    : ContentTemplateTokenProvider<MrCMSArticlesApp>
{
    public override string Name => "Article";
    public override string Icon => "fa fa-newspaper-o";

    public override string HtmlPattern => $"[{Name} name=\"Article\"]\n[/{Name}]";

    public override string Guide =>
        @"<div class='token-guide mt-3'>
            <h6>Available Variables:</h6>
            <div class='mb-3'>
                Article.Id, Article.Name, Article.FeatureImage, Article.Abstract, Article.PublishOn, Article.Published, Article.Author, Article.Url, Article.Tags, Article.Index
            </div>
            <small class='text-muted'>Use these variables in your template with double curly braces, e.g., {{Article.Name}}</small>
            <small class='text-muted d-block mt-2'>Note: Replace 'Article' with the value of the 'name' attribute in your token.</small>
        </div>";

    public override async Task<string> RenderAsync(
        string innerContent,
        Dictionary<string, string> attributes,
        Dictionary<string, object> variables,
        IHtmlHelper htmlHelper)
    {
        try
        {
            var name = attributes.GetValueOrDefault("name", "Article");

            var articleListId = TryGetIntVariable(variables, $"{name}.ArticleListId", 0);
            var categoryId = TryGetIntVariable(variables, $"{name}.CategoryId", 0);
            var count = TryGetIntVariable(variables, $"{name}.Count", 5);

            // Build the query
            var query = session.Query<Article>().Where(f => f.Published);

            if (articleListId > 0)
            {
                query = query.Where(f => f.Parent.Id == articleListId);
            }

            if (categoryId > 0)
            {
                query = query.Where(f => f.Tags.Any(t => t.Id == categoryId));
            }

            // Execute query with ordering and limit
            var articles = await query
                .OrderByDescending(f => f.PublishOn)
                .Take(count)
                .ToListAsync()
                .ConfigureAwait(false);

            var result = new StringBuilder();

            // Render each article
            for (var index = 0; index < articles.Count; index++)
            {
                var article = articles[index];
                // Create variables dictionary for this article
                var articleVariables = new Dictionary<string, object>(variables)
                {
                    { $"{name}.Id", article.Id },
                    { $"{name}.Name", article.Name },
                    { $"{name}.FeatureImage", article.FeatureImage },
                    { $"{name}.Abstract", article.Abstract },
                    { $"{name}.PublishOn", article.PublishOn },
                    { $"{name}.Published", article.Published },
                    { $"{name}.Author", article.User?.Name },
                    { $"{name}.Url", $"/{article.UrlSegment}" },
                    { $"{name}.Tags", string.Join(", ", article.Tags.Select(t => t.Name)) },
                    { $"{name}.Index", index }
                };

                // Render the inner content with article variables
                var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
                var renderedContent = await renderer.RenderAsync(htmlHelper, innerContent, articleVariables)
                    .ConfigureAwait(false);
                result.Append(renderedContent);
            }

            return result.ToString();
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
        var name = attributes.GetValueOrDefault("name", "Article");

        var selectedArticleId = savedProperties?.GetValueOrDefault($"{name}.ArticleListId")?.ToString() ?? string.Empty;
        var selectedCategoryId = savedProperties?.GetValueOrDefault($"{name}.CategoryId")?.ToString() ?? string.Empty;
        var countValue = savedProperties?.GetValueOrDefault($"{name}.Count")?.ToString() ?? "5";

        var articleListOptions = await GetArticleListOptionsHtml(selectedArticleId).ConfigureAwait(false);
        var categoryOptions = await GetCategoryOptionsHtml(selectedCategoryId).ConfigureAwait(false);
        var fieldId = GetFieldId(name);
        var fieldName = GetFieldName(name);
        var articleHtml = $@"
            <div class='row'>
                <div class='col-md-6 col-lg-4 col-xl-3'>
                    <div class='form-group'>
                        <label for='{fieldId}_articleListId'>Article List</label>
                        <select class='form-control' 
                                id='{fieldId}_articleListId' 
                                name='{fieldName}.ArticleListId'>
                            <option value=''>Select Article List...</option>
                            {articleListOptions}
                        </select>
                    </div>
                </div>
                
                <div class='col-md-6 col-lg-4 col-xl-3'>
                    <div class='form-group'>
                        <label for='{fieldId}_category'>Category</label>
                        <select class='form-control' 
                                id='{fieldId}_category' 
                                name='{fieldName}.CategoryId'>
                            <option value=''>All Categories</option>
                            {categoryOptions}
                        </select>
                    </div>
                </div>

                <div class='col-md-6 col-lg-4 col-xl-3'>
                    <div class='form-group'>
                        <label for='{fieldId}_count'>Number of Articles</label>
                        <input type='number' 
                               class='form-control' 
                               id='{fieldId}_count' 
                               name='{fieldName}.Count' 
                               value='{HttpUtility.HtmlEncode(countValue)}'
                               min='1' 
                               max='50' />
                    </div>
                </div>
            </div>";

        var renderer = serviceProvider.GetRequiredService<IContentTemplateRenderer>();
        var innerContentHtml = await RenderInnerContentAsync(
                htmlHelper, innerContent, savedProperties, renderer)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(innerContentHtml))
        {
            articleHtml += innerContentHtml;
        }
        
        return BuildCardHtml(name.BreakUpString(), articleHtml);
    }

    private async Task<string> GetArticleListOptionsHtml(string selectedValue)
    {
        var articleLists = await session.Query<ArticleList>()
            .OrderBy(al => al.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        var options = new StringBuilder();
        foreach (var list in articleLists)
        {
            options.AppendFormat(
                "<option value='{0}' {1}>{2}</option>",
                HttpUtility.HtmlEncode(list.Id),
                selectedValue == list.Id.ToString() ? "selected" : "",
                HttpUtility.HtmlEncode(list.Name));
        }

        return options.ToString();
    }

    private async Task<string> GetCategoryOptionsHtml(string selectedValue)
    {
        var categories = await session.Query<Tag>()
            .OrderBy(t => t.Name)
            .ToListAsync()
            .ConfigureAwait(false);

        var options = new StringBuilder();
        foreach (var category in categories)
        {
            options.AppendFormat(
                "<option value='{0}' {1}>{2}</option>",
                HttpUtility.HtmlEncode(category.Id),
                selectedValue == category.Id.ToString() ? "selected" : "",
                HttpUtility.HtmlEncode(category.Name));
        }

        return options.ToString();
    }

    private int TryGetIntVariable(Dictionary<string, object> variables, string key, int defaultValue)
    {
        return variables.TryGetValue(key, out var value) && int.TryParse(value?.ToString(), out var result)
            ? result
            : defaultValue;
    }
}