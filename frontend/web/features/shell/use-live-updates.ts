"use client";

import { useEffect, useState } from "react";
import { API_URL } from "@/lib/http";

export function useLiveUpdates() {
  const [live, setLive] = useState(false); const [events, setEvents] = useState(3);
  useEffect(() => {
    let connection: import("@microsoft/signalr").HubConnection | undefined;
    import("@microsoft/signalr").then(({ HubConnectionBuilder, LogLevel }) => { connection = new HubConnectionBuilder().withUrl(`${API_URL}/hubs/institute`).withAutomaticReconnect().configureLogging(LogLevel.Warning).build(); connection.on("InstituteEvent", () => setEvents(value => value + 1)); connection.start().then(() => setLive(true)).catch(() => setLive(false)); });
    return () => { void connection?.stop(); };
  }, []);
  return { live, events };
}
