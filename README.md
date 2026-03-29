# AC Overlay – Setup & Benutzung

## Was ist das?

Ein Live-Telemetrie-Overlay für Assetto Corsa. Es zeigt dir während der Fahrt deinen Geschwindigkeitsverlauf und deine Streckenposition an – und vergleicht dich mit deiner besten Runde.

---

## Was du brauchst

- **Assetto Corsa** (Steam) oder Content Manager
- **[.NET 8 SDK](https://dotnet.microsoft.com/download)** – einfach installieren, Next Next Finish
- **[Python 3](https://www.python.org/downloads/)** – beim Installieren ✅ "Add Python to PATH" anklicken!

---

## Einmalige Einrichtung

### 1. Firewall-Regel hinzufügen (einmalig, als Admin)

Öffne die **Eingabeaufforderung als Administrator** (Startmenü → `cmd` → Rechtsklick → *Als Administrator ausführen*) und führe aus:

```
netsh http add urlacl url=http://localhost:54321/ user=Jeder
```

> ⚠️ Falls du eine Fehlermeldung bekommst, ersetze `Jeder` durch deinen Windows-Benutzernamen, z.B. `user=DESKTOP-ABC\username`

---

## Starten

1. Assetto Corsa starten und in eine Session gehen
2. Doppelklick auf **`start_all.bat`** (liegt in `ACOverlay\ACOverlay\`)
3. Warten – der Browser öffnet sich automatisch

Das war's. ✅

---

## Benutzung

**Overlay (im Spiel):**
- 🟢 Grüne Linie = dein aktueller Geschwindigkeitsverlauf
- ⬜ Weiße gestrichelte Linie = deine beste gespeicherte Runde
- Die Karte rechts zeigt deine Position (🟡 Punkt) und die Strecke (🟢 Gas, 🔴 Bremse, 🔵 Rollen)

**Analyzer (Browser):**
- Links siehst du alle Auto+Strecken-Kombos die du in dieser Session gefahren bist
- Klick drauf → alle deine Runden werden als überlagerte Graphen angezeigt
- Hover über den Graph → siehst die Geschwindigkeit aller Runden an dieser Position
- **⚡ Beste Theoretische Zeit** → berechnet die schnellstmögliche Runde aus all deinen Fahrten
- **💾 Speichern** → überschreibt deine Bestzeit mit der theoretischen Zeit

---

## Bestzeiten

Bestzeiten werden automatisch gespeichert unter:
```
Dokumente\ACOverlay\best_strecke__auto.json
```
Pro Strecke + Auto getrennt. Nichts geht verloren wenn du das Programm beendest.

---

## Beenden

Die beiden schwarzen Fenster (ACOverlay + Webserver) einfach schließen – oder im Taskmanager `dotnet.exe` und `python.exe` beenden.
