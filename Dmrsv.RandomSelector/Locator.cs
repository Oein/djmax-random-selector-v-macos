using System;
using System.Text.RegularExpressions;
using Dmrsv.RandomSelector.Enums;
using Dmrsv.RandomSelector.Input;

namespace Dmrsv.RandomSelector
{
    public class Locator
    {
        public int InputInterval { get; set; } = 30;
        public bool LocatesStyle { get; set; } = true;
        public bool CanLocate { get; set; } = true;
        public bool PressesStart { get; set; } = false;

        private List<LocationInfo?> _locations;

        private readonly IInputSender _input;

        public Locator() : this(InputSender.Create())
        {
        }

        public Locator(IInputSender input)
        {
            _input = input;
            _locations = new List<LocationInfo?>();
        }

        public void MakeLocations(IEnumerable<Track> trackList)
        {
            var getGroup = (Track t) =>
            {
                char initial = char.ToLower(t.Title[0]);
                return Regex.IsMatch(initial.ToString(), "[a-z]", RegexOptions.IgnoreCase) ? initial : '#';
            };
            var groupByInitial = trackList.Where(t => t.IsPlayable)
                                          .OrderBy(t => t.Title, new TitleComparer())
                                          .ThenByDescending(t => t.Id == 170 || t.Id == 267 ? t.Id : 0)
                                          .GroupBy(t => getGroup(t))
                                          .ToDictionary(g => g.Key, g => g.ToList());
            var getIndex = (Track t) =>
            {
                char initial = char.ToLower(getGroup(t));
                int index = groupByInitial[initial].IndexOf(t);
                int count = groupByInitial[initial].Count();
                return index <= (count - 1) / 2 || "wxyzWXYZ".Contains(initial) ? index : index - count;
            };

            _locations = trackList.Select(track =>
            {
                if (!track.IsPlayable)
                {
                    return null;
                }
                return new LocationInfo()
                {
                    TrackId = track.Id,
                    Group = getGroup(track),
                    Index = getIndex(track),
                    DifficultyOrder = track.Patterns
                                           .GroupBy(p => p.Button)
                                           .SelectMany(g => g.Select((p, order) => new { p.Style, order }))
                                           .ToDictionary(o => o.Style, o => o.order)
                };
            }).ToList();
        }

        public void Locate(Pattern pattern)
        {
            if (!CanLocate)
            {
                return;
            }
            LocationInfo? loc = _locations[pattern.TrackId];
            if (loc is null)
            {
                return;
            }
            ResetMusicCursor();

            // input initial letter of title
            char group = loc.Group;
            if (loc.Index < 0)
            {
                if (group == '#')
                {
                    group = 'a';
                }
                else if (group == 'z')
                {
                    group = '#';
                }
                else
                {
                    group = (char)(group + 1);
                }
            }
            if (group != '#')
            {
                Input(LetterKey(group));
            }

            // locate to track
            Key arrow = loc.Index < 0 ? Key.Up : Key.Down;
            int distance = Math.Abs(loc.Index);
            RepeatInputs(distance, arrow);

            int difficultyOrder = loc.DifficultyOrder[pattern.Style];
            if (LocatesStyle)
            {
                SelectButton(pattern.Button.AsString()[0]);
                RepeatInputs(difficultyOrder, Key.Right);
            }
            if (PressesStart)
            {
                int startDelay = 800 - InputInterval * (difficultyOrder + 1);
                startDelay = startDelay < 0 ? 0 : startDelay;
                Thread.Sleep(startDelay);
                Input(Key.F5);
            }
        }

        private static Key LetterKey(char letter)
        {
            return Enum.Parse<Key>(char.ToUpper(letter).ToString());
        }

        private void KeyDown(Key key)
        {
            _input.KeyDown(key);
            Thread.Sleep(InputInterval);
        }

        private void KeyUp(Key key)
        {
            _input.KeyUp(key);
            Thread.Sleep(InputInterval);
        }

        private void Input(Key key)
        {
            KeyDown(key);
            KeyUp(key);
        }

        private void RepeatInputs(int number, Key key)
        {
            for (int i = 0; i < number; i++)
            {
                Input(key);
            }
        }

        private void ResetMusicCursor()
        {
            KeyDown(Key.ShiftRight);
            KeyDown(Key.ShiftLeft);
            KeyUp(Key.ShiftRight);
            KeyUp(Key.ShiftLeft);
        }

        private void SelectButton(char button)
        {
            Key key = button switch
            {
                '4' => Key.Numpad4,
                '5' => Key.Numpad5,
                '6' => Key.Numpad6,
                '8' => Key.Numpad8,
                _ => throw new NotImplementedException(),
            };
            KeyDown(Key.ControlLeft);
            KeyDown(key);
            KeyUp(key);
            KeyUp(Key.ControlLeft);
        }
    }
}
