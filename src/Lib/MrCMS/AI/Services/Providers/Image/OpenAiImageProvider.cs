﻿using System;
using System.Threading;
using System.Threading.Tasks;
using MrCMS.AI.Models;
using MrCMS.AI.Services.Core;
using MrCMS.AI.Settings;
using OpenAI.Images;

namespace MrCMS.AI.Services.Providers.Image;

public class OpenAiImageProvider : IAiImageProvider
{
    private readonly OpenAiSettings _settings;

    public OpenAiImageProvider(OpenAiSettings settings)
    {
        _settings = settings;
    }

    public async Task<AiImageResponse> GenerateImageAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Initialize the OpenAI API client using the API key from your settings.
        var client = new ImageClient(_settings.ImageModel, _settings.ApiKey);
        

        // Call the API using the client's image generation method.
        var result = await client.GenerateImagesAsync(prompt, 1, new()
        {
            Quality = _settings.ImageQuality,
            Size = _settings.ImageSize,
            Style = GeneratedImageStyle.Natural,
            ResponseFormat = GeneratedImageFormat.Uri,
        }, cancellationToken);
        
        // Extract the URL from the result.
        if ( result.Value.Count == 0)
        {
            throw new Exception("No image URL returned by the API.");
        }

        return new AiImageResponse
        {
            Url = result.Value[0].ImageUri
        };
    }
}