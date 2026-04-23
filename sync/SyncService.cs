using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using LangSwap.translation.model;
using LangSwap.windows;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LangSwap.sync;

// ----------------------------
// Sync Service
//
// @author Jo44
// @version 1.7 (23/04/2026)
// @since 01/01/2026
// ----------------------------
public sealed class SyncService : IDisposable
{
    // Log
    private readonly string Class = $"[{nameof(SyncService)}]";

    // Services
    private static IClientState ClientState => Plugin.ClientState;
    private static ICondition Condition => Plugin.Condition;
    private static IFramework Framework => Plugin.Framework;
    private static IObjectTable ObjectTable => Plugin.ObjectTable;
    private static IPluginLog Log => Plugin.Log;

    // Core component
    private readonly Configuration config;

    // HTTP client
    private readonly HttpClient httpClient = new();

    // Upload state
    private readonly HashSet<string> uploadedTranslations = new(StringComparer.Ordinal);
    private DateTime nextUploadAttempt = DateTime.MaxValue;
    private bool disposed = false;
    private bool isUploadRunning = false;
    private bool isInDuty = false;

    // ----------------------------
    // Constructor
    // ----------------------------
    public SyncService(Configuration config)
    {
        // Initialize core component
        this.config = config;

        // Retrieve remote obfuscated translations
        RetrieveRemoteObfuscatedTranslations();

        // Initialize bound by duty state
        isInDuty = CheckBoundByDuty();

        // Initialize next upload attempt
        nextUploadAttempt = DateTime.MaxValue;

        // Register framework update
        Framework.Update += OnFrameworkUpdate;
    }

    // ----------------------------
    // Retrieve remote obfuscated translations
    // ----------------------------
    private void RetrieveRemoteObfuscatedTranslations()
    {
        try
        {
            bool loaded = GetRemoteObfuscatedTranslations().GetAwaiter().GetResult();
            if (!loaded) Log.Warning($"{Class} - Remote obfuscated translations were not retrieved");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to retrieve remote obfuscated translations");
        }
    }

    // ----------------------------
    // Get remote obfuscated translations
    // ----------------------------
    private async Task<bool> GetRemoteObfuscatedTranslations()
    {
        try
        {
            // Check URL
            if (string.IsNullOrWhiteSpace(config.DownloadURL))
            {
                Log.Error($"{Class} - Remote obfuscated translations URL is empty");
                return false;
            }

            // Download remote CSV
            string csv = await httpClient.GetStringAsync(config.DownloadURL).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(csv))
            {
                Log.Warning($"{Class} - Remote obfuscated translations CSV is empty");
                return false;
            }

            // Import CSV content into a temporary list
            List<ObfuscatedTranslation> importedTranslations = [];
            if (!AdvancedWindow.ImportObfuscatedTranslationsCSV(csv, importedTranslations, out string status))
            {
                Log.Error($"{Class} - Failed to import remote obfuscated translations CSV : {status}");
                return false;
            }

            // Replace current remote translations and persist
            config.RemoteObfuscatedTranslations.Clear();
            config.RemoteObfuscatedTranslations.AddRange(importedTranslations);
            config.Save();

            // Log
            LogAllObfuscatedTranslations();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Failed to get remote obfuscated translations");
            return false;
        }
    }

    // ----------------------------
    // Log all obfuscated translations
    // ----------------------------
    private void LogAllObfuscatedTranslations()
    {
        if (config.RemoteObfuscatedTranslations.Count == 0)
        {
            Log.Information($"{Class} - No remote obfuscated translations loaded");
            return;
        }
        Log.Information($"{Class} - Remote obfuscated translations loaded : {config.RemoteObfuscatedTranslations.Count}");
        foreach (ObfuscatedTranslation translation in config.RemoteObfuscatedTranslations)
        {
            Log.Debug($"  - ActionID: {translation.ActionID}, LanguageID: {translation.LanguageID}, ObfuscatedName: {translation.ObfuscatedName}, DeobfuscatedName: {translation.DeobfuscatedName}");
        }
    }

    // ----------------------------
    // On framework update
    // ----------------------------
    private void OnFrameworkUpdate(IFramework framework)
    {
        // Check disposed, disabled upload or running upload
        if (disposed || !config.AutoUploadData || isUploadRunning) return;

        // Detect duty exit
        bool isBoundByDuty = CheckBoundByDuty();
        bool dutyExitTriggered = isInDuty && !isBoundByDuty;
        isInDuty = isBoundByDuty;

        // Upload attempt 10 sec after duty exit
        if (dutyExitTriggered)
        {
            nextUploadAttempt = DateTime.UtcNow.AddSeconds(10);
            return;
        }

        // Check upload trigger
        bool uploadTriggered = DateTime.UtcNow >= nextUploadAttempt;
        if (!uploadTriggered) return;

        // Run upload task
        isUploadRunning = true;
        _ = RunUploadTask();
    }

    // ----------------------------
    // Check bound by duty state
    // ----------------------------
    private static bool CheckBoundByDuty()
    {
        // Check if player is currently bound by duty
        return Condition[ConditionFlag.BoundByDuty] || Condition[ConditionFlag.BoundByDuty56];
    }

    // ----------------------------
    // Run upload task
    // ----------------------------
    private async Task RunUploadTask()
    {
        try
        {
            Log.Information($"{Class} - Try to upload unknown scanned obfuscated translations");

            // Resolve player name
            if (!TryGetPlayerName(out string playerName))
            {
                // Retry 10 sec later if player name is unavailable
                nextUploadAttempt = DateTime.UtcNow.AddSeconds(10); 
                Log.Warning($"{Class} - Upload delayed by 10 seconds because player name is unavailable");
                return;
            }

            // Get upload candidates
            List<ObfuscatedTranslation> candidates = GetUploadCandidates();
            if (candidates.Count == 0)
            {
                // No unknown data to upload
                nextUploadAttempt = DateTime.MaxValue;
                Log.Information($"{Class} - No unknown scanned obfuscated translations to upload. Aborded.");
                return;
            }

            Log.Information($"{Class} - New unknown scanned obfuscated translations count : {candidates.Count}");

            // Send candidates
            bool sent = await SendCandidatesAsync(playerName, candidates).ConfigureAwait(false);
            if (sent)
            {
                // Mark candidates as sent (in memory)
                MarkAsSent(candidates);
            }

            // Wait for next duty exit
            nextUploadAttempt = DateTime.MaxValue;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{Class} - Upload obfuscated translations failed");
            nextUploadAttempt = DateTime.MaxValue;
        }
        finally
        {
            isUploadRunning = false;
        }
    }

    // ----------------------------
    // Try get player name
    // ----------------------------
    private static bool TryGetPlayerName(out string playerName)
    {
        // Initialize player name
        playerName = string.Empty;

        // Check login state
        if (!ClientState.IsLoggedIn) return false;

        // Get player
        IPlayerCharacter? player = ObjectTable.LocalPlayer;
        if (player == null) return false;

        // Resolve player name
        playerName = $"[{player.HomeWorld.Value.Name}] {player.Name.TextValue}";
        return !string.IsNullOrWhiteSpace(playerName);
    }

    // ----------------------------
    // Get upload candidates
    // ----------------------------
    private List<ObfuscatedTranslation> GetUploadCandidates()
    {
        // Build known keys
        HashSet<string> knownKeys = new(StringComparer.Ordinal);
        AddKeys(knownKeys, config.RemoteObfuscatedTranslations);

        // Add already sent keys
        foreach (string sentKey in uploadedTranslations)
        {
            knownKeys.Add(sentKey);
        }

        // Build candidates
        List<ObfuscatedTranslation> candidates = [];
        HashSet<string> candidateKeys = new(StringComparer.Ordinal);

        // Iterate scanned obfuscated translations
        foreach (ObfuscatedTranslation scannedTranslation in config.ScannedObfuscatedTranslations)
        {
            // Check translation
            if (!IsValidTranslation(scannedTranslation)) continue;

            // Build key
            string key = BuildKey(scannedTranslation);

            // Skip known or duplicated entries
            if (knownKeys.Contains(key) || !candidateKeys.Add(key)) continue;

            // Add candidate
            candidates.Add(new ObfuscatedTranslation
            {
                ActionID = scannedTranslation.ActionID,
                ObfuscatedName = scannedTranslation.ObfuscatedName,
                LanguageID = scannedTranslation.LanguageID,
                DeobfuscatedName = scannedTranslation.DeobfuscatedName
            });
        }

        // Return upload candidates
        return candidates;
    }

    // ----------------------------
    // Send candidates
    // ----------------------------
    private async Task<bool> SendCandidatesAsync(string playerName, List<ObfuscatedTranslation> candidates)
    {
        // Check upload URL
        if (string.IsNullOrWhiteSpace(config.UploadURL))
        {
            Log.Error($"{Class} - Upload skipped because upload URL is empty");
            return false;
        }

        // Check API key
        if (string.IsNullOrWhiteSpace(config.APIKey))
        {
            Log.Error($"{Class} - Upload skipped because API key is empty");
            return false;
        }

        // Build upload translations expected by webservice
        List<UploadTranslation> uploadTranslations = [];

        // Iterate through candidates
        foreach (ObfuscatedTranslation candidate in candidates)
        {
            // Add upload translation
            uploadTranslations.Add(new UploadTranslation
            {
                ActionId = candidate.ActionID,
                ObfuscatedName = candidate.ObfuscatedName,
                DeobfuscatedName = candidate.DeobfuscatedName,
                LanguageId = candidate.LanguageID,
                CharacterName = playerName
            });
        }

        // Build request with API key header
        using HttpRequestMessage request = new(HttpMethod.Post, config.UploadURL)
        {
            Content = JsonContent.Create(uploadTranslations)
        };
        request.Headers.TryAddWithoutValidation("X-API-Key", config.APIKey);

        // Send request
        using HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"{Class} - Upload failed -> HTTP {(int)response.StatusCode}");
            return false;
        }

        // Log
        Log.Information($"{Class} - {uploadTranslations.Count} scanned obfuscated translations successfully uploaded");
        return true;
    }

    // ----------------------------
    // Mark candidates as sent
    // ----------------------------
    private void MarkAsSent(List<ObfuscatedTranslation> sentTranslations)
    {
        // Check input
        if (sentTranslations == null || sentTranslations.Count == 0) return;

        // Add sent keys in memory (only for current session)
        foreach (ObfuscatedTranslation translation in sentTranslations)
        {
            if (!IsValidTranslation(translation)) continue;
            this.uploadedTranslations.Add(BuildKey(translation));
        }
    }

    // ----------------------------
    // Add keys
    // ----------------------------
    private void AddKeys(HashSet<string> target, List<ObfuscatedTranslation> source)
    {
        // Check target and source
        if (target == null || source == null || source.Count == 0) return;

        // Add valid keys
        foreach (ObfuscatedTranslation translation in source)
        {
            if (!IsValidTranslation(translation)) continue;
            target.Add(BuildKey(translation));
        }
    }

    // ----------------------------
    // Build key
    // ----------------------------
    private static string BuildKey(ObfuscatedTranslation translation)
    {
        return $"{translation.ActionID}|{translation.ObfuscatedName}|{translation.LanguageID}";
    }

    // ----------------------------
    // Check translation validity
    // ----------------------------
    private bool IsValidTranslation(ObfuscatedTranslation translation)
    {
        return translation != null
            && translation.ActionID > 0 && translation.ActionID < config.MaxValidActionID
            && !string.IsNullOrWhiteSpace(translation.ObfuscatedName)
            && translation.LanguageID >= 0 && translation.LanguageID < 4
            && !string.IsNullOrWhiteSpace(translation.DeobfuscatedName);
    }

    // ----------------------------
    // Dispose
    // ----------------------------
    public void Dispose()
    {
        // Check already disposed
        if (disposed) return;

        // Set disposed state
        disposed = true;

        // Unregister framework update
        Framework.Update -= OnFrameworkUpdate;

        // Dispose HTTP client
        httpClient.Dispose();

        // Finalize
        GC.SuppressFinalize(this);
    }

}