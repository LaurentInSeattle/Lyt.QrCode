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

DTSTART / DTEND: Date and time in UTC format (YYYYMMDDTHHMMSSZ). For example, 20260320T120000Z is March 20, 2026, at 12:00 UTC.

LOCATION: The physical or virtual location.

DESCRIPTION: Details about the event.

*/

#endregion Documentation 

internal class CalendarEventContent
{
}
