using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dmrsv.RandomSelector;
using Dmrsv.RandomSelector.Enums;

namespace DjmaxRandomSelectorV.Desktop.Services
{
    /// <summary>
    /// Loads the V-Archive track database (AllTrackList.json) into the
    /// cross-platform <see cref="Track"/> model. Mirrors the mapping used by the
    /// Windows app's TrackDB, but with no Caliburn / WPF dependencies.
    /// </summary>
    public static class TrackLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public static IReadOnlyList<Track> Load(string path)
        {
            using FileStream stream = File.OpenRead(path);
            List<DbTrack> db = JsonSerializer.Deserialize<List<DbTrack>>(stream, Options)
                               ?? new List<DbTrack>();

            return db.Select(x =>
            {
                var info = new MusicInfo
                {
                    Id = x.Title,
                    Title = x.Name,
                    Composer = x.Composer,
                    Category = x.DlcCode,
                };
                return new Track
                {
                    Info = info,
                    // Preview: everything is playable. DLC ownership filtering is
                    // a stage-2 settings feature (see MACOS_PORTING.md).
                    IsPlayable = true,
                    Patterns = x.Patterns
                        .SelectMany(bt => bt.Value, (bt, df) => new Pattern
                        {
                            Info = info,
                            Button = bt.Key.AsButtonTunes(),
                            Difficulty = df.Key.AsDifficulty(),
                            Level = df.Value.Level,
                            Floor = df.Value.Floor,
                        })
                        .OrderBy(p => p.PatternId)
                        .ToArray(),
                };
            })
            // The Locator indexes locations by TrackId, so keep the list ordered by id.
            .OrderBy(t => t.Id)
            .ToList();
        }

        internal sealed record DbTrack
        {
            public int Title { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Composer { get; init; } = string.Empty;
            public string DlcCode { get; init; } = string.Empty;
            public string Dlc { get; init; } = string.Empty;

            [JsonPropertyName("patterns")]
            public Dictionary<string, Dictionary<string, DbPattern>> Patterns { get; init; } = new();
        }

        internal sealed record DbPattern
        {
            public int Level { get; init; }
            public double Floor { get; init; }
            public int Rating { get; init; }
        }
    }
}
