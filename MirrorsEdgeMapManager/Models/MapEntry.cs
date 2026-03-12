using System.Text.Json.Serialization;

namespace MirrorsEdgeMapManager.Models;

public class MapEntry
{
    [JsonPropertyName("FriendlyName")]
    public string? FriendlyName { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("MapFileName")]
    public string? MapFileName { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("image")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("configurl")]
    public string? ConfigUrl { get; set; }

    [JsonPropertyName("tdgameuiscene")]
    public string? TdGameUiScene { get; set; }

    [JsonPropertyName("zipsize")]
    public string? ZipSize { get; set; }

    [JsonPropertyName("speedrunid")]
    public string? SpeedrunId { get; set; }

    [JsonPropertyName("packname")]
    public string? PackName { get; set; }

    [JsonPropertyName("packdescription")]
    public string? PackDescription { get; set; }

    [JsonPropertyName("packedmaps")]
    public List<PackedMapEntry>? PackedMaps { get; set; }

    [JsonPropertyName("StretchNameId")]
    public string? StretchNameId { get; set; }

    [JsonPropertyName("QualifyingTime")]
    public string? QualifyingTime { get; set; }

    [JsonPropertyName("Rating1Time")]
    public string? Rating1Time { get; set; }

    [JsonPropertyName("Rating2Time")]
    public string? Rating2Time { get; set; }

    [JsonPropertyName("Rating3Time")]
    public string? Rating3Time { get; set; }

    [JsonPropertyName("CheckpointAFriendlyName")]
    public string? CheckpointAFriendlyName { get; set; }

    [JsonPropertyName("CheckpointAFileName")]
    public string? CheckpointAFileName { get; set; }

    [JsonPropertyName("CheckpointADescription")]
    public string? CheckpointADescription { get; set; }

    [JsonPropertyName("CheckpointBFriendlyName")]
    public string? CheckpointBFriendlyName { get; set; }

    [JsonPropertyName("CheckpointBFileName")]
    public string? CheckpointBFileName { get; set; }

    [JsonPropertyName("CheckpointBDescription")]
    public string? CheckpointBDescription { get; set; }

    [JsonPropertyName("CheckpointCFriendlyName")]
    public string? CheckpointCFriendlyName { get; set; }

    [JsonPropertyName("CheckpointCFileName")]
    public string? CheckpointCFileName { get; set; }

    [JsonPropertyName("CheckpointCDescription")]
    public string? CheckpointCDescription { get; set; }

    [JsonPropertyName("CheckpointDFriendlyName")]
    public string? CheckpointDFriendlyName { get; set; }

    [JsonPropertyName("CheckpointDFileName")]
    public string? CheckpointDFileName { get; set; }

    [JsonPropertyName("CheckpointDDescription")]
    public string? CheckpointDDescription { get; set; }

    [JsonPropertyName("CheckpointEFriendlyName")]
    public string? CheckpointEFriendlyName { get; set; }

    [JsonPropertyName("CheckpointEFileName")]
    public string? CheckpointEFileName { get; set; }

    [JsonPropertyName("CheckpointEDescription")]
    public string? CheckpointEDescription { get; set; }

    [JsonPropertyName("CheckpointFFriendlyName")]
    public string? CheckpointFFriendlyName { get; set; }

    [JsonPropertyName("CheckpointFFileName")]
    public string? CheckpointFFileName { get; set; }

    [JsonPropertyName("CheckpointFDescription")]
    public string? CheckpointFDescription { get; set; }

    [JsonPropertyName("CheckpointGFriendlyName")]
    public string? CheckpointGFriendlyName { get; set; }

    [JsonPropertyName("CheckpointGFileName")]
    public string? CheckpointGFileName { get; set; }

    [JsonPropertyName("CheckpointGDescription")]
    public string? CheckpointGDescription { get; set; }

    [JsonPropertyName("NumberOfCheckpoints")]
    public string? NumberOfCheckpoints { get; set; }

    public string DisplayName => PackName ?? FriendlyName ?? "Unknown";
    
    public bool IsMapPack => PackedMaps != null && PackedMaps.Count > 0;
}

public class PackedMapEntry
{
    [JsonPropertyName("FriendlyName")]
    public string? FriendlyName { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }

    [JsonPropertyName("MapFileName")]
    public string? MapFileName { get; set; }

    [JsonPropertyName("StretchNameId")]
    public string? StretchNameId { get; set; }

    [JsonPropertyName("QualifyingTime")]
    public string? QualifyingTime { get; set; }

    [JsonPropertyName("Rating1Time")]
    public string? Rating1Time { get; set; }

    [JsonPropertyName("Rating2Time")]
    public string? Rating2Time { get; set; }

    [JsonPropertyName("Rating3Time")]
    public string? Rating3Time { get; set; }
}

