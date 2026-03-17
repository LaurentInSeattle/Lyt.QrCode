namespace Lyt.QrCode.Content;

#region Documentation 
/*
 
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
LOCATION: The physical or virtual location.
DESCRIPTION: Details about the event.

*/

#endregion Documentation 

public class QrCalendarEvent : QrContent<QrCalendarEvent>
{
    public QrCalendarEvent(
        string summary,
        DateTime start, DateTime end, bool isAllDay = false,
        string location = "", string description = "",
        bool includeVcalendarTags = false) : base(isBinaryData: false)
    {
        this.Summary = summary;
        this.Location = location;
        this.Description = description;
        this.IncludeVcalendarTags = includeVcalendarTags;

        string dtFormatStart;
        string dtFormatEnd;
        if (isAllDay)
        {
            // format only the date: No time
            dtFormatStart = dtFormatEnd = "yyyyMMdd";
        }
        else
        {
            dtFormatStart = dtFormatEnd = "yyyyMMddTHHmmss";

            // Override format for UTC Date/Time's
            if (start.Kind == DateTimeKind.Utc)
            {
                dtFormatStart = "yyyyMMddTHHmmssZ";
            }

            if (end.Kind == DateTimeKind.Utc)
            {
                dtFormatEnd = "yyyyMMddTHHmmssZ";
            }
        }

        this.StartString = start.ToString(dtFormatStart, CultureInfo.InvariantCulture);
        this.EndString = end.ToString(dtFormatEnd, CultureInfo.InvariantCulture);
    }

    public string Summary { get; private set; }

    public string StartString { get; private set; }

    public string EndString { get; private set; }

    public string Location { get; private set; }

    public string Description { get; private set; }

    public bool IncludeVcalendarTags { get; private set; }

    public override string RawString
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
            sb.AppendLine($"SUMMARY:{this.Summary}");
            sb.AppendLine($"DTSTART:{this.StartString}");
            sb.AppendLine($"DTEND:{this.EndString}");

            if (!string.IsNullOrWhiteSpace(this.Location))
            {
                sb.AppendLine($"SUMMARY:{this.Location}");
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
}
