using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using DjmaxRandomSelectorV.Messages;
using DjmaxRandomSelectorV.Models;
using Dmrsv.RandomSelector.Enums;

namespace DjmaxRandomSelectorV.ViewModels
{
    public class VArchiveWizardViewModel : Screen
    {
        private readonly IEventAggregator _eventAggregator;

        private string _currentButton;
            private readonly TrackDB _trackDB;
        private string _currentBoard;
        public string Nickname { get; set; }
        public string SelectedButton { get; set; } = "4";
        public string CurrentButton
        {
            get => _currentButton;
            set
            {
                _currentButton = value;
                NotifyOfPropertyChange();
            }
        }
        public string CurrentBoard
        {
            get => _currentBoard;
            set
            {
                _currentBoard = value;
                NotifyOfPropertyChange();
            }
        }

        public BindableCollection<VArchivePatternItem> PatternItems { get; }

        public VArchiveWizardViewModel(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            PatternItems = new BindableCollection<VArchivePatternItem>();
        }

        public void RequestBoard()
        {
            if (string.IsNullOrEmpty(SelectedButton) || string.IsNullOrEmpty(SelectedBoard))
            {
                MessageBox.Show("Button or Board is empty.", "Invalid Request", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string url = $"https://v-archive.net/api/archive/{Nickname}/board/{SelectedButton}/{SelectedBoard}";
            HttpResponseMessage response = client.GetAsync(url).Result;
            BoardRoot root = response.Content.ReadFromJsonAsync<BoardRoot>().Result;
            if (root.Success != true)
            {
                MessageBox.Show(root.Message, "Request Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PatternItems.Clear();
            CurrentButton = root.Button + "B";
            CurrentBoard = root.Board switch
            {
                "MX" => "Lv. 12~15",
                "SC" => "SC",
                "SC5" => "SC~5",
                "SC10" => "SC~10",
                "SC15" => "SC~15",
                _ => "Lv. " + root.Board,
            };
            var items = from floor in root.Floors
                        let floorNumber = floor.FloorNumber
                        from p in floor.Patterns
                        select new VArchivePatternItem()
                        {
                            Id = p.Title,
                            Style = p.Pattern,
                            Title = p.Name,
                            Floor = floorNumber,
                            Score = p.Score is null ? null : double.Parse(p.Score),
                            IsMaxCombo = p.MaxCombo is null ? null : p.MaxCombo != 0,
                        };
            PatternItems.AddRange(items);
                if (string.IsNullOrEmpty(SelectedButton))
                {
                    MessageBox.Show("Button is empty.", "Invalid Request", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string url = $"https://v-archive.net/api/v2/archive/{Nickname}/button/{SelectedButton}";
                HttpResponseMessage response = client.GetAsync(url).Result;
                ArchiveV2Response root = response.Content.ReadFromJsonAsync<ArchiveV2Response>().Result;
                if (root.Success != true)
                {
                    MessageBox.Show(root.Message ?? "Unknown error", "Request Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                PatternItems.Clear();
                CurrentButton = root.Button + "B";
                CurrentBoard = "All Records";

                // TrackDB의 모든 곡 정보와 V2 API 기록 병합
                var recordDict = root.Records.ToDictionary(r => (r.Title, r.Pattern));
            
                int selectedButtonNum = int.Parse(SelectedButton);
                var selectedButtonTunes = (ButtonTunes)selectedButtonNum;
            
                var allPatterns = from track in _trackDB.AllTrack
                                 from pattern in track.Patterns
                                 where pattern.Button == selectedButtonTunes
                                 let patternStr = pattern.Difficulty.AsString()
                                 let recordKey = (track.Info.Id, patternStr)
                                 let record = recordDict.ContainsKey(recordKey) ? recordDict[recordKey] : null
                                 select new VArchivePatternItem()
                                 {
                                     Id = track.Info.Id,
                                     Style = patternStr,
                                     Title = track.Info.Title,
                                     Floor = record?.Floor ?? 0,
                                     Score = record?.Score,
                                     IsMaxCombo = record?.MaxCombo,
                                     HasRecord = record != null
                                 };
                PatternItems.AddRange(allPatterns);
        }

        #region Query
        public bool IncludesPlayed { get; set; } = false;
        public bool IncludesScore { get; set; } = false;
        public bool IncludesMaxCombo { get; set; } = true;
        public bool IncludesFloor { get; set; } = false;
        public bool IsPlayed { get; set; } = false;
        public bool IsNotPlayed { get; set; } = true;
        public double ScoreAbove { get; set; } = 0.0;
        public double ScoreBelow { get; set; } = 99.99;
        public bool IsMaxCombo { get; set; } = false;
        public bool IsNotMaxCombo { get; set; } = true;
        public double FloorAbove { get; set; } = 0.0;
        public double FloorBelow { get; set; } = 15.0;

        public void ApplyQuery(string command)
        {
            IEnumerable<VArchivePatternItem> items = PatternItems;
            if (IncludesPlayed)
            {
                items = IsPlayed
                      ? items.Where(i => i.Score is not null)
                      : items.Where(i => i.Score is null);
            }
            if (IncludesScore)
            {
                items = items.Where(i => i.Score is not null)
                        .Where(i => ScoreAbove <= i.Score && i.Score <= ScoreBelow);
            }
            if (IncludesMaxCombo)
            {
                items = items.Where(i => i.IsMaxCombo is not null)
                        .Where(i => i.IsMaxCombo == IsMaxCombo);
            }
            if (IncludesFloor)
            {
                items = items.Where(i => FloorAbove <= i.Floor && i.Floor <= FloorBelow);
            }

            bool checks = command != "deselect";
            if (command == "exclusive")
            {
                foreach (var item in PatternItems)
                {
                    item.IsChecked = false;
                }
            }

            foreach (var item in items)
            {
                item.IsChecked = checks;
            }
        }
        #endregion

        public void PublishItems(string command)
        {
            var items = from item in PatternItems
                        where item.IsChecked
                        select 100 * item.Id + 10 * (int)CurrentButton.AsButtonTunes() + (int)item.Style.AsDifficulty();
            _eventAggregator.PublishOnUIThreadAsync(new VArchiveMessage(items.ToArray(), command));
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            if (close)
            {
                _eventAggregator.PublishOnUIThreadAsync(new VArchiveMessage(null, "close"));
            }
            return Task.CompletedTask;
        }

        public record BoardRoot
        {
            public bool? Success { get; init; }
            public string Board { get; init; }
            public string Button { get; init; }
            public List<BoardFloor> Floors { get; init; }
            public string Message { get; init; }
        }

        public record BoardFloor
        {
            public double FloorNumber { get; init; }
            public List<BoardPattern> Patterns { get; init; }
        }

        public record BoardPattern
        {
            public int Title { get; init; }
            public string Name { get; init; }
            public string Pattern { get; init; }
            public string Score { get; init; }
            public int? MaxCombo { get; init; }
        }
            public record ArchiveV2Response
            {
                public bool? Success { get; init; }
                public string Button { get; init; }
                public string Nickname { get; init; }
                public int Count { get; init; }
                public List<ArchiveV2Record> Records { get; init; }
                public string Message { get; init; }
            }

            public record ArchiveV2Record
            {
                public int Title { get; init; }
                public string Name { get; init; }
                public string DlcCode { get; init; }
                public string Pattern { get; init; }
                public int Level { get; init; }
                public int? Floor { get; init; }
                public string FloorName { get; init; }
                public bool NewTab { get; init; }
                public double MaxRating { get; init; }
                public double Score { get; init; }
                public bool MaxCombo { get; init; }
                public double? Rating { get; init; }
                public double Djpower { get; init; }
                public double? MaxDjpower { get; init; }
                public string UpdatedAt { get; init; }
            }
    }
}
