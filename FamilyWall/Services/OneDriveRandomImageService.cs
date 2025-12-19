using Azure.Core;
using FamilyWall.Models;
using Google.Apis.Calendar.v3.Data;
using ImageMagick;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Matching;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FamilyWall.Services;

public sealed class OneDriveRandomImageService(
    IWebHostEnvironment env,
    IHttpClientFactory httpClientFactory, 
    IConfiguration configuration,
    ILogger<OneDriveRandomImageService> logger)
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private string AccessToken { get; set; } = "";

    public async Task SyncPhotoDetails(string id)
    {
        var ct = CancellationToken.None;

        HttpClient hc = httpClientFactory.CreateClient("microsoft-graph");

        string url = $"me/drive/items/{id}?$select=id,name,webUrl,createdDateTime,file,photo,fileSystemInfo,image,location,photo";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

        var resp = await hc.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        var page = await JsonSerializer.DeserializeAsync<OneDriveItem>(stream, _json, ct)
                   ?? new OneDriveItem();

        var photosFolder = Path.Combine(env.ContentRootPath, "photos");
        Directory.CreateDirectory(photosFolder);

        var fileName = $"{id}.json";
        var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(photosFolder, safeFileName);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, page, jsonOptions, ct);
        await fileStream.FlushAsync(ct);
    }

    public async Task<IActionResult> OnGetSyncPhoto(string id)
    {
        var photosFolder = Path.Combine(env.ContentRootPath, "photos");
        Directory.CreateDirectory(photosFolder);

        var fileName = $"{id}.jpg";
        var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(photosFolder, safeFileName);

        if (!File.Exists(filePath))
        {

            var ct = CancellationToken.None;

            HttpClient hc = httpClientFactory.CreateClient("microsoft-graph");

            string url = $"me/drive/items/{id}/content";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            const int maxWidth = 3840;
            const int maxHeight = 2160;

            var resp = await hc.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            await using var contentStream = await resp.Content.ReadAsStreamAsync(ct);

            // Buffer content so we can attempt multiple strategies (ImageSharp, external conversion)
            await using var originalBuffer = new MemoryStream();
            await contentStream.CopyToAsync(originalBuffer, ct);
            originalBuffer.Seek(0, SeekOrigin.Begin);

            // Load image with ImageSharp, with HEIC fallback via external converter (ImageMagick 'magick' CLI)
            try
            {
                Image<Rgba32>? image = null;
                Stream? imageStreamToDispose = null;

                try
                {
                    // Try ImageSharp first (will throw UnknownImageFormatException for unsupported formats like HEIC)
                    originalBuffer.Seek(0, SeekOrigin.Begin);
                    image = await Image.LoadAsync<Rgba32>(originalBuffer, ct);
                }
                catch (UnknownImageFormatException)
                {
                    // If content-type or signature indicates HEIC, attempt external conversion using 'magick'
                    originalBuffer.Seek(0, SeekOrigin.Begin);
                    var looksLikeHeic = contentType.Contains("heic", StringComparison.OrdinalIgnoreCase)
                                        || contentType.Contains("heif", StringComparison.OrdinalIgnoreCase)
                                        || IsHeicSignature(originalBuffer);

                    if (looksLikeHeic)
                    {
                        try
                        {
                            var convertedPath = await ConvertHeicToJpegWithMagickAsync(originalBuffer, ct);
                            if (convertedPath is not null && File.Exists(convertedPath))
                            {
                                imageStreamToDispose = File.OpenRead(convertedPath);
                                image = await Image.LoadAsync<Rgba32>(imageStreamToDispose, ct);

                                // Delete the temporary converted file after loading
                                try { imageStreamToDispose.Dispose(); File.Delete(convertedPath); imageStreamToDispose = null; } catch { /* best effort */ }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "HEIC conversion failed for {Id}", id);
                        }
                    }

                    // If still null, rethrow original exception to be handled by outer catch
                    if (image is null)
                        throw;
                }

                if (image is null)
                    return new NotFoundResult();

                using (image)
                {
                    var width = image.Width;
                    var height = image.Height;

                    bool needsResize = width > maxWidth || height > maxHeight;

                    if (needsResize)
                    {
                        // Compute scale factor preserving aspect ratio
                        var scale = Math.Min((double)maxWidth / width, (double)maxHeight / height);
                        var newWidth = Math.Max(1, (int)Math.Round(width * scale));
                        var newHeight = Math.Max(1, (int)Math.Round(height * scale));

                        image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                    }

                    // Save as JPEG with reasonable quality
                    var encoder = new JpegEncoder { Quality = 90 };

                    // Overwrite if exists
                    await image.SaveAsJpegAsync(filePath, encoder, ct);

                    await SyncPhotoDetails(id);

                    return new OkResult();
                }
            }
            catch (UnknownImageFormatException)
            {
                logger.LogWarning("Skipped saving item {Id} - unknown image format", id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process image {Id}", id);
            }

            return new NotFoundResult();
        }

        return new ConflictResult();
    }

    private static bool IsHeicSignature(Stream s)
    {
        // Peek into the first 12-16 bytes to look for ISO BMFF ftyp + heic/heix/mif1 indicators.
        try
        {
            s.Seek(0, SeekOrigin.Begin);
            Span<byte> buffer = stackalloc byte[16];
            int read = s.Read(buffer);
            s.Seek(0, SeekOrigin.Begin);

            if (read < 12) return false;

            // Look for 'ftyp' at bytes 4..7
            if (buffer[4] == (byte)'f' && buffer[5] == (byte)'t' && buffer[6] == (byte)'y' && buffer[7] == (byte)'p')
            {
                // brand begins at 8
                var brand = System.Text.Encoding.ASCII.GetString(buffer.Slice(8, Math.Min(8, read - 8)).ToArray());
                if (brand.Contains("heic", StringComparison.OrdinalIgnoreCase) ||
                    brand.Contains("heix", StringComparison.OrdinalIgnoreCase) ||
                    brand.Contains("mif1", StringComparison.OrdinalIgnoreCase) ||
                    brand.Contains("msf1", StringComparison.OrdinalIgnoreCase) ||
                    brand.Contains("hevc", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            try { s.Seek(0, SeekOrigin.Begin); } catch { }
            return false;
        }
    }

    private async Task<string?> ConvertHeicToJpegWithMagickAsync(Stream heicStream, CancellationToken ct)
    {
        return null;
    }

    public async Task<List<OneDriveItem>> SyncPhotos(string accessToken, CancellationToken ct = default)
    {
        AccessToken = accessToken;
        var url = $"me/drive/items/{configuration["WallSettings:Microsoft:OneDriveAlbumId"]}/children?$select=id,name,webUrl,createdDateTime,file,photo,fileSystemInfo,image,location";
        HttpClient hc = httpClientFactory.CreateClient("microsoft-graph");

        var allPhotos = new List<OneDriveItem>();

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

            var resp = await hc.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            // var xx = await resp.Content.ReadAsStringAsync(ct);

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var page = await JsonSerializer.DeserializeAsync<OneDriveChildrenResponse>(stream, _json, ct)
                       ?? new OneDriveChildrenResponse();

            foreach (var item in page.Value)
            {
                // Only files that look like images
                var mime = item.File?.MimeType ?? "";
                var isImage = mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                              || (item.Name?.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".heic", StringComparison.OrdinalIgnoreCase) ?? false);

                if (!isImage) continue;

                allPhotos.Add(item);

                // Prefer EXIF date taken (photo.takenDateTime)
                var dt =
                    item.Photo?.TakenDateTime
                    ?? item.FileSystemInfo?.CreatedDateTime
                    ?? item.CreatedDateTime;

                if (dt is null) continue;
            }

            // Paging (Graph uses @odata.nextLink; mapped here as OdataNextLink)
            url = page.OdataNextLink;
        }

        return allPhotos;
    }

    public async Task<OneDriveItem?> PickRandomImageForMonthDayAsync(
        string accessToken,
        string folderPath,   // e.g. "Pictures/OnThisDay"
        int month,
        int day,
        CancellationToken ct = default)
    {
        var url = $"me/drive/items/{configuration["WallSettings:Microsoft:OneDriveAlbumId"]}/children?$select=id,name,webUrl,createdDateTime,file,photo,fileSystemInfo";

        var candidates = new List<OneDriveItem>();
        var all = new List<OneDriveItem>();

        logger.LogInformation($"Pulling {url}");

        var http = httpClientFactory.CreateClient("microsoft-graph");

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var resp = await http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            // var xx = await resp.Content.ReadAsStringAsync(ct);

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var page = await JsonSerializer.DeserializeAsync<OneDriveChildrenResponse>(stream, _json, ct)
                       ?? new OneDriveChildrenResponse();

            foreach (var item in page.Value)
            {
                // Only files that look like images
                var mime = item.File?.MimeType ?? "";
                var isImage = mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                              || (item.Name?.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ?? false)
                              || (item.Name?.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ?? false);

                if (!isImage) continue;

                all.Add(item);
                
                // Prefer EXIF date taken (photo.takenDateTime)
                var dt =
                    item.Photo?.TakenDateTime
                    ?? item.FileSystemInfo?.CreatedDateTime
                    ?? item.CreatedDateTime;

                if (dt is null) continue;

                if (dt.Value.Month == month && dt.Value.Day == day)
                    candidates.Add(item);
            }

            // Paging (Graph uses @odata.nextLink; mapped here as OdataNextLink)
            url = page.OdataNextLink;
        }

        if (candidates.Count == 0)
        {
            return all[Random.Shared.Next(all.Count)];
        }

        var idx = Random.Shared.Next(candidates.Count);
        return candidates[idx];
    }
}