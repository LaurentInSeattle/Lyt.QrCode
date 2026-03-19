namespace Lyt.QrCode.Content;

#region Documentation 
/*
 
See: https://en.wikipedia.org/wiki/ICalendar 

See: https://www.rfc-editor.org/rfc/rfc5545 

Minimal example: 

BEGIN:VCALENDAR
VERSION:2.0
BEGIN:VEVENT
SUMMARY:Event Title
DTSTART:YYYYMMDDTHHMMSSZ
DTEND:YYYYMMDDTHHMMSSZ
LOCATION:Venue
DESCRIPTION:Description
END:VEVENT
END:VCALENDAR

BEGIN:VCALENDAR/END:VCALENDAR: Defines the container. 

Note: Some simplified scanners work better without these lines, using only the VEVENT portion.

SUMMARY: The title of the event.
DTSTART / DTEND: Date and time usually in UTC format (YYYYMMDDTHHMMSSZ). For example, 20260320T120000Z is March 20, 2026, at 12:00 UTC.
LOCATION: The (physical or virtual) location of the event.
DESCRIPTION: Details about the event.

*/

#endregion Documentation 

/// <summary> ICS Calendar (iCal) Event </summary>
/// <remarks> VERY minimal implementation of the RFC 5545. </remarks>
/// <remarks> 
/// Consider implementing the RRULE tag (recuring rules). 
/// Ex: RRULE:FREQ=WEEKLY;INTERVAL=1;BYDAY=FR;UNTIL=20260601T000000Z 
/// </remarks>
public class QrCalendarEvent : QrContent<QrCalendarEvent>, IQrParsable<QrCalendarEvent>
{
    private const string dtFormatUtc = "yyyyMMddTHHmmssZ";
    private const string dtFormat = "yyyyMMdd";
    private const string dtFormatLocal = "yyyyMMddTHHmmss";

    private const string timeStampKey = "DTSTAMP:";
    private const string uniqueIdKey = "UID:";
    private const string summaryKey = "SUMMARY:";
    private const string locationKey = "LOCATION:";
    private const string descriptionKey = "DESCRIPTION:";
    private const string startKey = "DTSTART:";
    private const string endKey = "DTEND:";
    private const string startKeyAllDay = "DTSTART;VALUE=DATE:";
    private const string endKeyAllDay = "DTEND;VALUE=DATE:";

    public QrCalendarEvent(
        string summary,
        DateTime start, DateTime end, bool isAllDay = false,
        string location = "", string description = "",
        bool includeVcalendarTags = true)
        : base(isBinaryData: false)
    {
        if (end < start)
        {
            throw new ArgumentException("Event ends before it begins: end < start");
        }

        this.TimeStamp = DateTime.UtcNow.ToString(dtFormatUtc, CultureInfo.InvariantCulture);
        this.UniqueId = Guid.NewGuid().ToString();
        this.Summary = summary;
        this.Location = location;
        this.Description = description;
        this.IsAllDay = isAllDay;
        this.IncludeVcalendarTags = includeVcalendarTags;

        if (isAllDay)
        {
            // format only the date: No time
            this.StartString = start.ToString(dtFormat, CultureInfo.InvariantCulture);

            // End should next day at zero time 
            end = start.AddDays(1);
            this.EndString = end.ToString(dtFormat, CultureInfo.InvariantCulture);
        }
        else
        {
            string dtFormatStart = dtFormatLocal;
            string dtFormatEnd = dtFormatLocal;

            // Override format for UTC Date/Time's
            if (start.Kind == DateTimeKind.Utc)
            {
                dtFormatStart = dtFormatUtc;
            }

            if (end.Kind == DateTimeKind.Utc)
            {
                dtFormatEnd = dtFormatUtc;
            }

            this.StartString = start.ToString(dtFormatStart, CultureInfo.InvariantCulture);
            this.EndString = end.ToString(dtFormatEnd, CultureInfo.InvariantCulture);
        }
    }

    public string TimeStamp { get; private set; }

    public string UniqueId { get; private set; }

    public string Summary { get; private set; }

    public string StartString { get; private set; }

    public string EndString { get; private set; }

    public string Location { get; private set; }

    public string Description { get; private set; }

    public bool IncludeVcalendarTags { get; private set; }

    public bool IsAllDay { get; private set; }

    public override string QrString
    {
        get
        {
            var sb = new StringBuilder();
            if (this.IncludeVcalendarTags)
            {
                sb.AppendLine("BEGIN:VCALENDAR");
                sb.AppendLine("VERSION: 2.0");
            }

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"DTSTAMP:{this.TimeStamp}");
            sb.AppendLine($"UID:{this.UniqueId}");
            sb.AppendLine($"SUMMARY:{this.Summary}");
            if (this.IsAllDay)
            {
                sb.AppendLine($"DTSTART;VALUE=DATE:{this.StartString}");
                sb.AppendLine($"DTEND;VALUE=DATE:{this.EndString}");
            }
            else
            {
                sb.AppendLine($"DTSTART:{this.StartString}");
                sb.AppendLine($"DTEND:{this.EndString}");
            }

            if (!string.IsNullOrWhiteSpace(this.Location))
            {
                sb.AppendLine($"LOCATION:{this.Location}");
            }

            if (!string.IsNullOrWhiteSpace(this.Description))
            {
                sb.AppendLine($"DESCRIPTION:{this.Description}");
            }

            sb.AppendLine("END:VEVENT");
            if (this.IncludeVcalendarTags)
            {
                sb.AppendLine("END:VCALENDAR");
            }

            return sb.ToString();
        }
    }

    public static bool TryParse(string source, [NotNullWhen(true)] out QrCalendarEvent? qrCalendarEvent)
    {
        qrCalendarEvent = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source string cannot be null, empty or white space", nameof(source));
        }

        try
        {
            string timeStamp = string.Empty;
            string uniqueId = string.Empty;
            string summary = string.Empty;
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MinValue;
            bool isAllDay = false;
            string location = string.Empty;
            string description = string.Empty;
            string[] lines = source.SplitLines();

            foreach (string line in lines)
            {
                if (line.StartsWith("BEGIN") || line.StartsWith("END") || line.StartsWith("VERSION"))
                {
                    continue;
                }

                if (line.StartsWith(timeStampKey))
                {
                    timeStamp = line[timeStampKey.Length..];
                    continue;
                }

                if (line.StartsWith(uniqueIdKey))
                {
                    uniqueId = line[uniqueIdKey.Length..];
                    continue;
                }

                if (line.StartsWith(summaryKey))
                {
                    summary = line[summaryKey.Length..];
                    continue;
                }

                if (line.StartsWith(locationKey))
                {
                    location = line[locationKey.Length..];
                    continue;
                }

                if (line.StartsWith(descriptionKey))
                {
                    description = line[descriptionKey.Length..];
                    continue;
                }

                if (line.StartsWith(startKey))
                {
                    string startString = line[startKey.Length..];
                    if (DateTime.TryParse(startString, out DateTime maybeStart))
                    {
                        start = maybeStart;
                        continue;
                    }
                }

                if (line.StartsWith(endKey))
                {
                    string endString = line[endKey.Length..];
                    if (DateTime.TryParse(endString, out DateTime maybeEnd))
                    {
                        end = maybeEnd;
                        continue;
                    }
                }


                if (line.StartsWith(startKeyAllDay))
                {
                    string startString = line[startKeyAllDay.Length..];
                    if (DateTime.TryParse(startString, out DateTime maybeStart))
                    {
                        start = maybeStart;
                        isAllDay = true;
                        continue;
                    }
                }

                if (line.StartsWith(endKeyAllDay))
                {
                    string endString = line[endKeyAllDay.Length..];
                    if (DateTime.TryParse(endString, out DateTime maybeEnd))
                    {
                        end = maybeEnd;
                        isAllDay = true;
                        continue;
                    }
                }
                // Unknown key: Ignore and continue 
            }

            if ((start == DateTime.MinValue) || (end == DateTime.MinValue))
            {
                // Fail: no start or end date / time 
                return false;
            }


            if (!isAllDay)
            {
                DateTime nextDay = start.AddDays(1);
                if (end.Date == nextDay.Date)
                {
                    isAllDay = true;
                }
            } 

            qrCalendarEvent = new QrCalendarEvent(summary, start, end, isAllDay, location, description)
            {
                TimeStamp = timeStamp,
                UniqueId = uniqueId,
            };

            return true;
        }
        catch
        {
            // Swallow everything else 
        }

        return false;
    }
}
