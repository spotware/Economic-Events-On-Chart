using System;
using cAlgo.API;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    public class EconomicEventsOnChart : Indicator
    {
        private Color _colorHighImpact, _colorMediumImpact, _colorLowImpact, _colorOthers;

        [Parameter("Data URI", DefaultValue = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml", Group = "General")]
        public string DataUri { get; set; }

        [Parameter("Only Symbol Events", DefaultValue = true, Group = "General")]
        public bool OnlySymbolEvents { get; set; }

        [Parameter("Show", DefaultValue = true, Group = "High Impact")]
        public bool ShowHighImpact { get; set; }

        [Parameter("Color", DefaultValue = "Red", Group = "High Impact")]
        public string ColorHighImpact { get; set; }

        [Parameter("Style", DefaultValue = LineStyle.Solid, Group = "High Impact")]
        public LineStyle LineStyleHighImpact { get; set; }

        [Parameter("Thickness", DefaultValue = 1, Group = "High Impact")]
        public int ThicknessHighImpact { get; set; }

        [Parameter("Show", DefaultValue = true, Group = "Medium Impact")]
        public bool ShowMediumImpact { get; set; }

        [Parameter("Color", DefaultValue = "Gold", Group = "Medium Impact")]
        public string ColorMediumImpact { get; set; }

        [Parameter("Style", DefaultValue = LineStyle.Solid, Group = "Medium Impact")]
        public LineStyle LineStyleMediumImpact { get; set; }

        [Parameter("Thickness", DefaultValue = 1, Group = "Medium Impact")]
        public int ThicknessMediumImpact { get; set; }

        [Parameter("Show", DefaultValue = true, Group = "Low Impact")]
        public bool ShowLowImpact { get; set; }

        [Parameter("Color", DefaultValue = "Yellow", Group = "Low Impact")]
        public string ColorLowImpact { get; set; }

        [Parameter("Style", DefaultValue = LineStyle.Solid, Group = "Low Impact")]
        public LineStyle LineStyleLowImpact { get; set; }

        [Parameter("Thickness", DefaultValue = 1, Group = "Low Impact")]
        public int ThicknessLowImpact { get; set; }

        [Parameter("Show", DefaultValue = false, Group = "Others")]
        public bool ShowOthers { get; set; }

        [Parameter("Color", DefaultValue = "Gray", Group = "Others")]
        public string ColorOthers { get; set; }

        [Parameter("Style", DefaultValue = LineStyle.Solid, Group = "Others")]
        public LineStyle LineStyleOthers { get; set; }

        [Parameter("Thickness", DefaultValue = 1, Group = "Others")]
        public int ThicknessOthers { get; set; }

        protected override void Initialize()
        {
            RemoveEventLines();

            _colorHighImpact = GetColor(ColorHighImpact);
            _colorMediumImpact = GetColor(ColorMediumImpact);
            _colorLowImpact = GetColor(ColorLowImpact);
            _colorOthers = GetColor(ColorOthers);

            var events = GetNewsEvents();

            DisplayEvents(events);
        }

        public override void Calculate(int index)
        {
        }

        private IEnumerable<NewsEvent> GetNewsEvents()
        {
            using (var webClient = new WebClient())
            {
                var data = webClient.DownloadString(DataUri);

                return GetNewsEventsFromXml(data);
            }
        }

        private IEnumerable<NewsEvent> GetNewsEventsFromXml(string xml)
        {
            var xmlSerializer = new XmlSerializer(typeof(WeeklyEvents));

            var stream = new StringReader(xml);

            var weeklyEvents = xmlSerializer.Deserialize(stream) as WeeklyEvents;

            foreach (var newsEvent in weeklyEvents.Events)
            {
                var timeString = string.Format("{0} {1}", newsEvent.UtcDate, newsEvent.UtcTime);

                DateTimeOffset time;

                if (DateTimeOffset.TryParseExact(timeString, "MM-dd-yyyy h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out time))
                {
                    newsEvent.Time = time;
                }
            }

            return weeklyEvents.Events;
        }

        private void DisplayEvents(IEnumerable<NewsEvent> events)
        {
            foreach (var newsEvent in events)
            {
                if (!newsEvent.Time.HasValue
                    || (newsEvent.Impact == NewsEventImpact.High && !ShowHighImpact)
                    || (newsEvent.Impact == NewsEventImpact.Medium && !ShowMediumImpact)
                    || (newsEvent.Impact == NewsEventImpact.Low && !ShowLowImpact)
                    || ((newsEvent.Impact == NewsEventImpact.None || newsEvent.Impact == NewsEventImpact.Holiday) && !ShowOthers)
                    || (OnlySymbolEvents && !IsEventRelatedToSymbol(newsEvent.Currency))) continue;

                var lineSettings = GetLineSettings(newsEvent.Impact);

                var eventLine = Chart.DrawVerticalLine(string.Format("{0} | {1} | {2} | Event", newsEvent.Title, newsEvent.Currency, newsEvent.Impact), newsEvent.Time.Value.UtcDateTime, lineSettings.Color, lineSettings.Thickness, lineSettings.Style);

                var stringBuilder = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(newsEvent.Forecast))
                {
                    stringBuilder.Append(string.Format("Forecast: {0} | ", newsEvent.Forecast));
                }

                if (!string.IsNullOrWhiteSpace(newsEvent.Previous))
                {
                    stringBuilder.Append(string.Format("Previous: {0} | ", newsEvent.Previous));
                }

                if (newsEvent.Time.HasValue)
                {
                    var time = newsEvent.Time.Value.ToOffset(Application.UserTimeOffset);

                    stringBuilder.Append(string.Format("Time: {0:s}", time));
                }

                eventLine.Comment = stringBuilder.ToString();
                eventLine.IsInteractive = true;
                eventLine.IsLocked = true;
            }
        }

        private bool IsEventRelatedToSymbol(string eventCurrency)
        {
            return SymbolName.StartsWith(eventCurrency, StringComparison.OrdinalIgnoreCase) || SymbolName.EndsWith(eventCurrency, StringComparison.OrdinalIgnoreCase);
        }

        private Color GetColor(string colorString, int alpha = 255)
        {
            var color = colorString[0] == '#' ? Color.FromHex(colorString) : Color.FromName(colorString);

            return Color.FromArgb(alpha, color);
        }

        private LineSettings GetLineSettings(NewsEventImpact impact)
        {
            switch (impact)
            {
                case NewsEventImpact.High:
                    return new LineSettings
                    {
                        Color = _colorHighImpact,
                        Style = LineStyleHighImpact,
                        Thickness = ThicknessHighImpact
                    };

                case NewsEventImpact.Medium:
                    return new LineSettings
                    {
                        Color = _colorMediumImpact,
                        Style = LineStyleMediumImpact,
                        Thickness = ThicknessMediumImpact
                    };

                case NewsEventImpact.Low:
                    return new LineSettings
                    {
                        Color = _colorLowImpact,
                        Style = LineStyleLowImpact,
                        Thickness = ThicknessLowImpact
                    };

                default:
                    return new LineSettings
                    {
                        Color = _colorOthers,
                        Style = LineStyleOthers,
                        Thickness = ThicknessOthers
                    };
            }
        }

        private void RemoveEventLines()
        {
            var chartObjects = Chart.Objects.ToArray();

            foreach (var chartObject in chartObjects)
            {
                if (chartObject.ObjectType != ChartObjectType.VerticalLine || !chartObject.IsInteractive || string.IsNullOrEmpty(chartObject.Name) || !chartObject.Name.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Chart.RemoveObject(chartObject.Name);
            }
        }
    }

    [XmlRoot("weeklyevents")]
    public class WeeklyEvents
    {
        [XmlElement("event")]
        public List<NewsEvent> Events { get; set; }
    }

    public class NewsEvent
    {
        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("country")]
        public string Currency { get; set; }

        [XmlElement("date")]
        public string UtcDate { get; set; }

        [XmlElement("time")]
        public string UtcTime { get; set; }

        [XmlIgnore]
        public DateTimeOffset? Time { get; set; }

        [XmlElement("impact")]
        public NewsEventImpact Impact { get; set; }

        [XmlElement("previous")]
        public string Previous { get; set; }

        [XmlElement("forecast")]
        public string Forecast { get; set; }
    }

    public enum NewsEventImpact
    {
        None,

        High,

        Medium,

        Low,

        Holiday
    }

    public struct LineSettings
    {
        public Color Color { get; set; }

        public LineStyle Style { get; set; }

        public int Thickness { get; set; }
    }
}