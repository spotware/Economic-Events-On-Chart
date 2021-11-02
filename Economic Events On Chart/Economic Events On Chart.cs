using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;
using System.Collections.Generic;
using System.Xml;
using System.Web;
using System.Net;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.Internet)]
    public class EconomicEventsOnChart : Indicator
    {
        [Parameter("Data URI", DefaultValue = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml")]
        public string DataUri { get; set; }

        protected override void Initialize()
        {
            var events = GetNewsEvents();

            foreach (var newsEvent in events)
            {
                Print("{0} | {1} | {2} | {3} | {4} | {5}", newsEvent.Title, newsEvent.Currency, newsEvent.Impact, newsEvent.Time.ToString("o"), newsEvent.Actual, newsEvent.Forecast);
            }

            Print(events.Count());
        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index
            // Result[index] = ...
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
        public DateTimeOffset Time { get; set; }

        [XmlElement("impact")]
        public NewsEventImpact Impact { get; set; }

        [XmlElement("previous")]
        public string Previous { get; set; }

        [XmlElement("actual")]
        public string Actual { get; set; }

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
}