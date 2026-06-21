using System.Collections.Generic;
using System.Linq;
using Dmrsv.RandomSelector;
using Dmrsv.RandomSelector.Enums;

namespace DjmaxRandomSelectorV.Desktop.Services
{
    /// <summary>
    /// Wires the cross-platform selection pipeline (filter -> pick -> select ->
    /// locate) together. Stage-2 preview: a single "Freestyle, all DLC" filter.
    /// Pressing the hotkey (or the button) calls <see cref="SelectRandom"/>.
    /// </summary>
    public sealed class SelectorService
    {
        private readonly IReadOnlyList<Track> _tracks;
        private readonly BasicFilter _filter;
        private readonly PatternPicker _picker;
        private readonly SelectorWithHistory _selector;
        private readonly Locator _locator;

        private List<Pattern> _candidates = new();

        public SelectorService(IReadOnlyList<Track> tracks)
        {
            _tracks = tracks;

            _filter = new BasicFilter();
            foreach (var category in tracks.Select(t => t.Category).Distinct())
            {
                _filter.Categories.Add(category);
            }

            _picker = new PatternPicker();
            _picker.SetPickMethod(MusicForm.Free, LevelPreference.Lowest);

            _selector = new SelectorWithHistory(new History<int>(capacity: 5));

            _locator = new Locator
            {
                LocatesStyle = false,  // Freestyle: navigate to the track only
                CanLocate = true,      // send keystrokes to the focused game window
            };
            _locator.MakeLocations(_tracks);

            UpdateCandidates();
        }

        public int CandidateCount => _candidates.Count;

        /// <summary>
        /// Picks a random pattern from the current candidates and drives the game
        /// (best effort — keystrokes only reach a focused window and need macOS
        /// Accessibility permission). Returns the picked pattern, or null if the
        /// candidate set is empty.
        /// </summary>
        public Pattern? SelectRandom()
        {
            if (_filter.IsUpdated)
            {
                UpdateCandidates();
            }

            Pattern? selected = _selector.Select(_candidates);
            if (selected is not null)
            {
                _locator.Locate(selected);
            }
            return selected;
        }

        private void UpdateCandidates()
        {
            _candidates = _picker.Pick(_filter.Filter(_tracks)).ToList();
        }
    }
}
