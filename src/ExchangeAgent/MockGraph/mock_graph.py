#!/usr/bin/env python3
"""
Minimal stand-in for the Microsoft Graph calendarView endpoint, for trying out the Exchange Agent
WITHOUT an Entra tenant. Python 3.8+, standard library only.

  python mock_graph.py [--port 8099]

Serves  GET /v1.0/users/{room-id}/calendar/calendarView?startDateTime=...&endDateTime=...[&$top=N]
and answers like Graph does: events as JSON, in pages of at most $top entries, with an
"@odata.nextLink" while more pages follow. All data is invented and relative to "now".

  room id                                 answer
  11111111-1111-1111-1111-111111111111    13 events (one running now, one cancelled), paged
  22222222-2222-2222-2222-222222222222    2 events
  33333333-3333-3333-3333-333333333333    0 events (empty calendar)
  44444444-4444-4444-4444-444444444444    HTTP 403 ErrorAccessDenied (like a blocked access policy)
  anything else                           HTTP 404 ErrorInvalidUser

There is no authentication: the Authorization header is ignored. Use the mock only on localhost.
"""
import argparse
import json
import sys
from datetime import datetime, timedelta, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, quote, urlparse

ROOM_1 = "11111111-1111-1111-1111-111111111111"
ROOM_2 = "22222222-2222-2222-2222-222222222222"
ROOM_3 = "33333333-3333-3333-3333-333333333333"
ROOM_DENIED = "44444444-4444-4444-4444-444444444444"
ROOM_MAIL = {ROOM_1: "workshop@example.com", ROOM_2: "meeting@example.com", ROOM_3: "empty@example.com"}

PEOPLE = [
    ("Alex Example", "alex@example.com"),
    ("Sam Sample", "sam@example.com"),
    ("Robin Demo", "robin@example.com"),
]


def graph_time(moment):
    return {"dateTime": moment.strftime("%Y-%m-%dT%H:%M:%S.0000000"), "timeZone": "UTC"}


def person(name, mail):
    return {"name": name, "address": mail}


def make_event(room, number, start, minutes, subject, organizer=0, guests=(1,), cancelled=False, all_day=False):
    org = PEOPLE[organizer]
    attendees = [{"type": "resource", "status": {"response": "accepted", "time": "0001-01-01T00:00:00Z"},
                  "emailAddress": person("Room " + room[:4], ROOM_MAIL.get(room, "room@example.com"))}]
    for index in guests:
        name, mail = PEOPLE[index]
        attendees.append({"type": "required", "status": {"response": "accepted" if index == 1 else "none",
                          "time": "2026-01-01T08:00:00Z"}, "emailAddress": person(name, mail)})
    return {
        "id": "MOCK-%s-%03d" % (room[:4], number),
        "subject": subject,
        "isAllDay": all_day,
        "isCancelled": cancelled,
        "showAs": "busy",
        "start": graph_time(start),
        "end": graph_time(start + timedelta(minutes=minutes)),
        "organizer": {"emailAddress": person(*org)},
        "attendees": attendees,
    }


def events_for(room, now):
    hour = now.replace(minute=0, second=0, microsecond=0)
    if room == ROOM_1:
        result = [make_event(room, 0, now - timedelta(minutes=30), 90, "Running workshop: 3D printing basics", 0, (1, 2))]
        topics = ["Laser cutter safety", "CNC introduction", "Soldering evening", "Open lab", "Repair cafe",
                  "Electronics 101", "Wood working", "Textile lab", "Robotics club", "Team meeting", "Photo shooting"]
        for n, topic in enumerate(topics, start=1):
            result.append(make_event(room, n, hour + timedelta(hours=3 * n + 1), 60 + 30 * (n % 3), topic,
                                     n % 3, (1,) if n % 2 else (0, 2)))
        result.append(make_event(room, 99, hour + timedelta(hours=2), 60, "Cancelled: Visit", 1, (0,), cancelled=True))
        return result
    if room == ROOM_2:
        return [make_event(room, 1, hour + timedelta(hours=1), 45, "Board meeting", 1, (0, 2)),
                make_event(room, 2, hour + timedelta(days=1, hours=1), 120, "Evening lecture", 2, ())]
    return []


class Handler(BaseHTTPRequestHandler):
    server_version = "MockGraph/1.0"

    def log_message(self, fmt, *args):
        print("%s  %s" % (datetime.now().strftime("%H:%M:%S"), fmt % args))

    def send_json(self, status, payload):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def send_error_json(self, status, code, message):
        self.send_json(status, {"error": {"code": code, "message": message}})

    def do_GET(self):
        url = urlparse(self.path)
        query = {key: values[0] for key, values in parse_qs(url.query, keep_blank_values=True).items()}
        parts = [part for part in url.path.split("/") if part]
        print("          Prefer: %s" % self.headers.get("Prefer"))
        if len(parts) != 5 or parts[0] != "v1.0" or parts[1] != "users" or parts[3:] != ["calendar", "calendarView"]:
            return self.send_error_json(404, "ErrorInvalidRequest", "Only users/{id}/calendar/calendarView is mocked.")
        room = parts[2]
        if room == ROOM_DENIED:
            return self.send_error_json(403, "ErrorAccessDenied", "Access is denied. Check credentials and try again.")
        if room not in (ROOM_1, ROOM_2, ROOM_3):
            return self.send_error_json(404, "ErrorInvalidUser", "The requested user '%s' is invalid." % room)
        if "startDateTime" not in query or "endDateTime" not in query:
            return self.send_error_json(400, "BadRequest", "startDateTime and endDateTime are required.")

        now = datetime.now(timezone.utc).replace(tzinfo=None)
        try:
            window_start = datetime.fromisoformat(query["startDateTime"].rstrip("Z").split(".")[0])
            window_end = datetime.fromisoformat(query["endDateTime"].rstrip("Z").split(".")[0])
        except ValueError:
            return self.send_error_json(400, "BadRequest", "Cannot parse startDateTime/endDateTime.")
        wanted = []
        for event in events_for(room, now):
            start = datetime.fromisoformat(event["start"]["dateTime"].split(".")[0])
            end = datetime.fromisoformat(event["end"]["dateTime"].split(".")[0])
            if start < window_end and end > window_start:   # calendarView: events overlapping the window
                wanted.append(event)
        wanted.sort(key=lambda e: e["start"]["dateTime"])

        top = int(query.get("$top", "10"))
        skip = int(query.get("$skip", "0"))
        page = wanted[skip:skip + top]
        payload = {"@odata.context": "https://graph.microsoft.com/v1.0/$metadata#Collection(event)", "value": page}
        if skip + top < len(wanted):
            host = self.headers.get("Host", "localhost")
            next_query = dict(query)
            next_query["$skip"] = str(skip + top)
            # Graph builds the nextLink as an absolute URL that already contains the whole query
            payload["@odata.nextLink"] = "http://%s%s?%s" % (
                host, url.path, "&".join("%s=%s" % (key, quote(value, safe="$:,'/")) for key, value in next_query.items()))
        self.send_json(200, payload)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--port", type=int, default=8099)
    args = parser.parse_args()
    sys.stdout.reconfigure(line_buffering=True)
    server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    print("Mock Graph listening on http://127.0.0.1:%d/v1.0  (Ctrl+C stops)" % args.port)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
