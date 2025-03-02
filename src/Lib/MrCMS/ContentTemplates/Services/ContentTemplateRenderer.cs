using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using MrCMS.ContentTemplates.ContentTemplateTokenProviders.Base;

namespace MrCMS.ContentTemplates.Services;

public partial class ContentTemplateRenderer : IContentTemplateRenderer
{
    private readonly Dictionary<string, ContentTemplateTokenProvider> _tokenProviders;

    private static readonly Regex TokenRegex = new(
        @"\[(?<closing>/)?(?<name>\w+)(?<attributes>(?:\s+\w+\s*=\s*(?:""[^""]*""|'[^']*'))*)\s*(?<selfClosing>/)?\]",
        RegexOptions.Compiled);

    private static readonly Regex VariableRegex = new(
        @"\{\{(?<expression>[^}]+)\}\}",
        RegexOptions.Compiled);

    public ContentTemplateRenderer(
        IEnumerable<ContentTemplateTokenProvider> tokenProviders)
    {
        _tokenProviders = tokenProviders.ToDictionary(x => x.Name, x => x);
    }

    public async Task<string> RenderAsync(IHtmlHelper htmlHelper, string template,
        Dictionary<string, object> variables = null)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        variables ??= new Dictionary<string, object>();

        var result = await ProcessTokensAsync(htmlHelper, template, variables);
        return SubstituteVariables(result, variables);
    }

    public async Task<string> RenderAdminAsync(IHtmlHelper htmlHelper, string template, string namePrefix = null,
        Dictionary<string, object> savedProperties = null)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        var result = await ProcessAdminTokensAsync(htmlHelper, template, namePrefix, savedProperties);

        return result;
    }

    private async Task<string> ProcessAdminTokensAsync(
        IHtmlHelper htmlHelper,
        string template,
        string namePrefix,
        Dictionary<string, object> savedProperties)
    {
        var result = new StringBuilder();
        var index = 0;

        while (index < template.Length)
        {
            var match = TokenRegex.Match(template, index);
            if (match.Success && match.Index == index)
            {
                var tokenName = match.Groups["name"].Value;
                var closing = match.Groups["closing"].Success;
                var selfClosing = match.Groups["selfClosing"].Success;

                if (!_tokenProviders.TryGetValue(tokenName, out var provider))
                {
                    // Skip unrecognized tokens in admin render
                    index += match.Length;
                    continue;
                }

                if (closing)
                {
                    // Unexpected closing tag at top level, skip
                    index += match.Length;
                    continue;
                }
                
                // Set the name prefix for nested tokens
                var originalNamePrefix = $"{provider.NamePrefix}";
                provider.NamePrefix = namePrefix + provider.NamePrefix;

                var attributes = ParseAttributes(match.Groups["attributes"].Value);

                if (selfClosing)
                {
                    // Self-closing token
                    var adminHtml = await provider.RenderAdminAsync(
                        string.Empty,
                        attributes,
                        htmlHelper,
                        savedProperties);

                    var wrappedAdminHtml = $"<div class=\"{provider.ResponsiveClass}\" >{adminHtml}</div>";
                    result.Append(wrappedAdminHtml);
                    index += match.Length;
                }
                else
                {
                    // Non-self-closing token
                    var openingTagEnd = index + match.Length;
                    var closingTagIndex = FindClosingTag(template, tokenName, openingTagEnd);

                    if (closingTagIndex >= 0)
                    {
                        // Extract inner content without processing nested tokens
                        var innerContentStart = openingTagEnd;
                        var innerContentLength = closingTagIndex - openingTagEnd;
                        var innerContent = template.Substring(innerContentStart, innerContentLength);

                        // Pass inner content as-is to the provider
                        var adminHtml = await provider.RenderAdminAsync(
                            innerContent,
                            attributes,
                            htmlHelper,
                            savedProperties);

                        if (!string.IsNullOrEmpty(adminHtml))
                        {
                            var wrappedAdminHtml = $"<div class=\"{provider.ResponsiveClass}\" >{adminHtml}</div>";
                            result.Append(wrappedAdminHtml);
                        }

                        // Move index past the closing tag
                        index = closingTagIndex + $"[/{tokenName}]".Length;
                    }
                    else
                    {
                        // No matching closing tag, skip this token
                        index += match.Length;
                    }
                }
                
                provider.NamePrefix = originalNamePrefix;
            }
            else
            {
                // No token at current index, skip to next character
                index++;
            }
        }

        var wrappedResult = $"<div class=\"row\">{result}</div>";
        return wrappedResult;
    }


    private async Task<string> ProcessTokensAsync(
        IHtmlHelper htmlHelper,
        string template,
        Dictionary<string, object> variables)
    {
        var result = new StringBuilder();
        var index = 0;

        while (index < template.Length)
        {
            var match = TokenRegex.Match(template, index);
            if (match.Success && match.Index == index)
            {
                var tokenName = match.Groups["name"].Value;
                var closing = match.Groups["closing"].Success;
                var selfClosing = match.Groups["selfClosing"].Success;

                if (!_tokenProviders.TryGetValue(tokenName, out var provider))
                {
                    // Unrecognized token, treat as text
                    result.Append(template.Substring(index, match.Length));
                    index += match.Length;
                    continue;
                }

                if (closing)
                {
                    // Unexpected closing tag at top level, treat as text
                    result.Append(template.Substring(index, match.Length));
                    index += match.Length;
                    continue;
                }

                var attributes = ParseAttributes(match.Groups["attributes"].Value);

                if (selfClosing)
                {
                    // Self-closing token
                    var rendered = await provider.RenderAsync(
                        string.Empty,
                        attributes,
                        variables,
                        htmlHelper);
                    result.Append(rendered);
                    index += match.Length;
                }
                else
                {
                    // Non-self-closing token
                    var openingTagEnd = index + match.Length;
                    var closingTagIndex = FindClosingTag(template, tokenName, openingTagEnd);

                    if (closingTagIndex >= 0)
                    {
                        // Extract inner content without processing nested tokens
                        var innerContentStart = openingTagEnd;
                        var innerContentLength = closingTagIndex - openingTagEnd;
                        var innerContent = template.Substring(innerContentStart, innerContentLength);

                        // Pass inner content as-is to the provider
                        var rendered = await provider.RenderAsync(
                            innerContent,
                            attributes,
                            variables,
                            htmlHelper);
                        result.Append(rendered);

                        // Move index past the closing tag
                        index = closingTagIndex + $"[/{tokenName}]".Length;
                    }
                    else
                    {
                        // No matching closing tag, treat opening tag as text
                        result.Append(template.Substring(index, match.Length));
                        index += match.Length;
                    }
                }
            }
            else
            {
                // No token at current index, add character to result
                result.Append(template[index]);
                index++;
            }
        }

        return result.ToString();
    }

    private int FindClosingTag(string template, string tokenName, int startIndex)
    {
        var nestingLevel = 1;
        var index = startIndex;
        while (index < template.Length && nestingLevel > 0)
        {
            var match = TokenRegex.Match(template, index);
            if (match.Success)
            {
                if (match.Index > index)
                {
                    // Skip any text between tokens
                    index = match.Index;
                }

                var currentTokenName = match.Groups["name"].Value;
                var closing = match.Groups["closing"].Success;
                var selfClosing = match.Groups["selfClosing"].Success;

                if (currentTokenName.Equals(tokenName, StringComparison.OrdinalIgnoreCase))
                {
                    if (!closing && !selfClosing)
                    {
                        // Found an opening tag of the same name
                        nestingLevel++;
                    }
                    else if (closing)
                    {
                        // Found a closing tag of the same name
                        nestingLevel--;
                        if (nestingLevel == 0)
                        {
                            // Match found
                            return match.Index;
                        }
                    }
                    // Self-closing tags do not affect nesting level
                }

                // Move index to after this match
                index = match.Index + match.Length;
            }
            else
            {
                // No more tokens found
                break;
            }
        }

        // No matching closing tag found
        return -1;
    }

    private static Dictionary<string, string> ParseAttributes(string attributesText)
    {
        var attributes = new Dictionary<string, string>();
        var matches = AttributeRegex().Matches(attributesText);

        foreach (Match match in matches)
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;
            attributes[key] = value;
        }

        return attributes;
    }

    private string SubstituteVariables(string content, Dictionary<string, object> variables)
    {
        return VariableRegex.Replace(content, match =>
        {
            var variableName = match.Groups["expression"].Value.Trim();
            if (TryGetVariableValue(variableName, variables, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }

            // Important: Return empty string for undefined variables instead of original placeholder
            return string.Empty;
        });
    }

    private static bool TryGetVariableValue(string variableName, Dictionary<string, object> variables, out object value)
    {
        if (variables.TryGetValue(variableName, out var variableValue))
        {
            value = variableValue;
            return true;
        }

        value = null;
        return false;
    }

    [GeneratedRegex(@"\s*(?<key>\w+)\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')"
    )]
    private static partial Regex AttributeRegex();
}