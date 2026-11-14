using System;
using System.Collections.Generic;
using UnityEngine;

namespace RageRefinement
{
    public static class Locale
    {
        private const string Preference = "RageRefinement.Language";
        public static readonly string[] Names = { "Čeština", "English", "Slovenčina", "Deutsch" };

        private static readonly string[,] Labels =
        {
            { "Language", "Jazyk", "Jazyk", "Sprache" },
            { "Options", "Nastavení", "Nastavenia", "Optionen" },
            { "Pause", "Pauza", "Pauza", "Pause" },
            { "Stage {0} / {1}", "Úroveň {0} / {1}", "Úroveň {0} / {1}", "Level {0} / {1}" },
            { "Checkpoint", "Kontrolní bod", "Kontrolný bod", "Kontrollpunkt" },
            { "Hold Jump for more height", "Pro vyšší skok podrž klávesu", "Pre vyšší skok podrž kláves", "Für höhere Sprünge Taste halten" },
            { "Talk to {0}  ·  {1}", "Promluv si: {0}  ·  {1}", "Porozprávaj sa: {0}  ·  {1}", "Sprich mit {0}  ·  {1}" },
            { "Watch the cutscene", "Sleduj scénu", "Sleduj scénu", "Sieh dir die Szene an" },
            { "Course complete", "Všechny úrovně dokončeny", "Všetky úrovne dokončené", "Alle Level geschafft" },
            { "Return to title", "Zpět na úvodní obrazovku", "Späť na úvodnú obrazovku", "Zurück zum Titelbild" },
            { "Quit game", "Ukončit hru", "Ukončiť hru", "Spiel beenden" },
            { "Quit the game?", "Ukončit hru?", "Ukončiť hru?", "Spiel beenden?" },
            { "Paused", "Pozastaveno", "Pozastavené", "Pausiert" },
            { "Game options", "Nastavení hry", "Nastavenia hry", "Spieloptionen" },
            { "Resume game", "Pokračovat ve hře", "Pokračovať v hre", "Spiel fortsetzen" },
            { "Start game", "Spustit hru", "Spustiť hru", "Spiel starten" },
            { "Skip to generated stages", "Rovnou na generované úrovně", "Rovno na generované úrovne", "Zu den generierten Leveln" },
            { "Controls", "Ovládání", "Ovládanie", "Steuerung" },
            { "Retry stage", "Zkusit úroveň znovu", "Skúsiť úroveň znova", "Level neu starten" },
            { "Retry this stage?", "Zkusit tuto úroveň znovu?", "Skúsiť túto úroveň znova?", "Dieses Level neu starten?" },
            { "You will restart this stage from the beginning.", "Tuto úroveň začneš od začátku.", "Túto úroveň začneš od začiatku.", "Du beginnst dieses Level von vorn." },
            { "Return to main menu", "Zpět do hlavní nabídky", "Späť do hlavnej ponuky", "Zurück zum Hauptmenü" },
            { "Return to main menu?", "Zpět do hlavní nabídky?", "Späť do hlavnej ponuky?", "Zurück zum Hauptmenü?" },
            { "Back", "Zpět", "Späť", "Zurück" },
            { "Move left", "Pohyb doleva", "Pohyb doľava", "Nach links" },
            { "Move right", "Pohyb doprava", "Pohyb doprava", "Nach rechts" },
            { "Jump", "Skok", "Skok", "Springen" },
            { "Attack", "Útok", "Útok", "Angreifen" },
            { "Talk / continue", "Mluvit / pokračovat", "Hovoriť / pokračovať", "Sprechen / weiter" },
            { "Press a letter, Space or Enter. Esc cancels.", "Stiskni písmeno, mezerník nebo Enter. Esc volbu zruší.", "Stlač písmeno, medzerník alebo Enter. Esc zruší výber.", "Drücke einen Buchstaben, die Leertaste oder Enter. Esc bricht ab." },
            { "Reset keys", "Obnovit výchozí klávesy", "Obnoviť predvolené klávesy", "Tasten zurücksetzen" },
            { "Tap Attack to kick. Hold and release to shoot.\nArrow keys move. Left click also attacks.\nController: stick to move, A jump, X attack, Y talk, Start pause.", "Krátkým stiskem zaútočíš kopem. Podrž a pusť pro výstřel.\nŠipky slouží k pohybu a levé tlačítko myši k útoku.\nOvladač: páčka – pohyb, A – skok, X – útok, Y – mluvit, Start – pauza.", "Krátkym stlačením kopneš. Podrž a pusť, ak chceš vystreliť.\nŠípky slúžia na pohyb a ľavé tlačidlo myši na útok.\nOvládač: páčka – pohyb, A – skok, X – útok, Y – hovoriť, Start – pauza.", "Angriff kurz antippen zum Treten. Halten und loslassen zum Schießen.\nPfeiltasten zum Laufen, linke Maustaste zum Angreifen.\nController: Stick zum Laufen, A Springen, X Angriff, Y Sprechen, Start Pause." },
            { "That key is in use. Press a different key or Esc.", "Tato klávesa už je přiřazená. Zkus jinou nebo stiskni Esc.", "Tento kláves už je priradený. Skús iný alebo stlač Esc.", "Diese Taste ist schon belegt. Wähle eine andere oder drücke Esc." },
            { "You will lose progress in this run.", "Přijdeš o postup v této hře.", "Prídeš o postup v tejto hre.", "Dein Fortschritt in diesem Durchlauf geht verloren." },
            { "Go back", "Zpět", "Späť", "Zurück" },
            { "Graphics", "Grafika", "Grafika", "Grafik" },
            { "Fast", "Rychlá", "Rýchla", "Schnell" },
            { "Balanced", "Vyvážená", "Vyvážená", "Ausgewogen" },
            { "Sharp", "Ostrá", "Ostrá", "Scharf" },
            { "Display", "Zobrazení", "Zobrazenie", "Anzeige" },
            { "Fullscreen", "Celá obrazovka", "Celá obrazovka", "Vollbild" },
            { "Windowed", "V okně", "V okne", "Fenster" },
            { "Play", "Hrát", "Hrať", "Spielen" },
            { "Exit", "Konec", "Koniec", "Beenden" },
            { "Continue", "Pokračovat", "Pokračovať", "Weiter" },
            { "Space", "Mezerník", "Medzerník", "Leertaste" },
            { "Workshop", "Dílna", "Dielňa", "Werkstatt" },
            { "Start run", "Začít pokus", "Začať pokus", "Lauf starten" },
            { "Start next run", "Další pokus", "Ďalší pokus", "Nächster Lauf" },
            { "Restart run", "Začít pokus znovu", "Začať pokus znova", "Lauf neu starten" },
            { "Restart this run?", "Začít tento pokus znovu?", "Začať tento pokus znova?", "Diesen Lauf neu starten?" },
            { "Reshape and try again", "Znovu se vytvaruj", "Znova sa vytvaruj", "Neu formen und weiter" },
            { "Your clay and buffs stay with you.", "Hlína a vylepšení ti zůstanou.", "Hlina a vylepšenia ti zostanú.", "Ton und Verbesserungen bleiben dir." },
            { "Your clay and buffs are saved. This run will end.", "Hlína a vylepšení jsou uložené. Tento pokus skončí.", "Hlina a vylepšenia sú uložené. Tento pokus sa skončí.", "Ton und Verbesserungen sind gespeichert. Dieser Lauf endet." },
            { "Fight, earn clay, buy buffs, and try again.", "Bojuj, sbírej hlínu, kupuj vylepšení a zkus to znovu.", "Bojuj, zbieraj hlinu, kupuj vylepšenia a skús to znova.", "Kämpfe, sammle Ton, kaufe Verbesserungen und versuche es erneut." },
            { "Saved clay: {0}", "Uložená hlína: {0}", "Uložená hlina: {0}", "Gespeicherter Ton: {0}" },
            { "This run: {0} clay · {1} enemies defeated", "Tento pokus: {0} hlíny · {1} poražených", "Tento pokus: {0} hliny · {1} porazených", "Dieser Lauf: {0} Ton · {1} Gegner besiegt" },
            { "Clay: {0} · Shells: {1}", "Hlína: {0} · Štíty: {1}", "Hlina: {0} · Štíty: {1}", "Ton: {0} · Schilde: {1}" },
            { "Enemies left: {0}", "Zbývá nepřátel: {0}", "Zostáva nepriateľov: {0}", "Verbleibende Gegner: {0}" },
            { "Defeat {0} more enemies to open the exit.", "Pro otevření východu poraz ještě {0} nepřátel.", "Na otvorenie východu poraz ešte {0} nepriateľov.", "Besiege noch {0} Gegner, um den Ausgang zu öffnen." },
            { "Permanent buffs apply to your next run.", "Trvalá vylepšení platí od dalšího pokusu.", "Trvalé vylepšenia platia od ďalšieho pokusu.", "Dauerhafte Verbesserungen gelten ab dem nächsten Lauf." },
            { "Purchased. Ready for your next run.", "Zakoupeno. Připraveno pro další pokus.", "Zakúpené. Pripravené na ďalší pokus.", "Gekauft. Bereit für den nächsten Lauf." },
            { "Stronger kicks", "Silnější útoky", "Silnejšie útoky", "Stärkere Angriffe" },
            { "More hearts", "Více srdcí", "Viac sŕdc", "Mehr Herzen" },
            { "Clay shell", "Hliněný štít", "Hlinený štít", "Tonpanzer" },
            { "Second wind", "Druhý dech", "Druhý dych", "Zweiter Atem" },
            { "Hit damage: {0}", "Síla zásahu: {0}", "Sila zásahu: {0}", "Trefferschaden: {0}" },
            { "Hearts: {0}", "Srdce: {0}", "Srdcia: {0}", "Herzen: {0}" },
            { "Blocked hits per run: {0}", "Blokované zásahy za pokus: {0}", "Blokované zásahy za pokus: {0}", "Geblockte Treffer pro Lauf: {0}" },
            { "Hearts healed per defeat: {0}", "Léčení za poraženého: {0}", "Liečenie za porazeného: {0}", "Heilung pro besiegtem Gegner: {0}" },
            { "{0} clay", "{0} hlíny", "{0} hliny", "{0} Ton" },
            { "Maxed", "Maximum", "Maximum", "Maximum" },
            { "Heals 1 heart every {0} defeats", "Vyléčí 1 srdce za {0} poražené", "Vylieči 1 srdce za {0} porazených", "Heilt 1 Herz nach je {0} Gegnern" },
            { "Healing not unlocked", "Léčení není odemčené", "Liečenie nie je odomknuté", "Heilung noch nicht freigeschaltet" },
            { "Nearest: {0} · {1}", "Nejblíže: {0} · {1}", "Najbližšie: {0} · {1}", "Nächster: {0} · {1}" },
            { "Above", "Nahoře", "Hore", "Oben" },
            { "Below", "Dole", "Dole", "Unten" },
            { "Left", "Vlevo", "Vľavo", "Links" },
            { "Right", "Vpravo", "Vpravo", "Rechts" },
            { "Exit open · Head right", "Východ otevřen · Jdi vpravo", "Východ otvorený · Choď vpravo", "Ausgang offen · Nach rechts" },
            { "Paper Lane", "Papírová ulička", "Papierová ulička", "Papiergasse" },
            { "Sunflower Steps", "Slunečnicové schody", "Slnečnicové schody", "Sonnenblumentreppen" },
            { "Glaze Alley", "Glazurová ulička", "Glazúrová ulička", "Glasurgasse" },
            { "Mossy Rooftops", "Mechové střechy", "Machové strechy", "Moosdächer" },
            { "The Clayworks", "Hlinárna", "Hlináreň", "Die Tonwerke" },
            { "The Royal Kiln", "Královská pec", "Kráľovská pec", "Der königliche Ofen" },
            { "The Kiln King", "Král pece", "Kráľ pece", "Der Ofenkönig" },
            { "Lobber", "Vrhač", "Vrhač", "Werfer" },
            { "Shieldguard", "Štítonoš", "Štítonoš", "Schildwache" },
            { "Mender", "Léčitel", "Liečiteľ", "Heiler" },
            { "Splitter", "Dělič", "Delič", "Teiler" },
            { "Rushling", "Rychlík", "Rýchlik", "Flitzer" },
            { "Clay dash", "Hliněný úskok", "Hlinený úskok", "Tonspurt" },
            { "Ricochet", "Odraz střel", "Odraz striel", "Querschläger" },
            { "Forked shots", "Dělené střely", "Delené strely", "Streuschüsse" },
            { "Rhythm punch", "Rytmický úder", "Rytmický úder", "Rhythmusschlag" },
            { "Choose district", "Vybrat čtvrť", "Vybrať štvrť", "Viertel wählen" },
            { "Cleared districts stay unlocked.", "Vyčištěné čtvrti zůstávají odemčené.", "Vyčistené štvrte zostávajú odomknuté.", "Geschaffte Viertel bleiben freigeschaltet." },
            { "View tricks", "Zobrazit triky", "Zobraziť triky", "Tricks anzeigen" },
            { "View body buffs", "Zobrazit posílení", "Zobraziť posilnenia", "Körper-Boni anzeigen" },
            { "Shift / B dash · cooldown {0}s", "Úskok Shift / B · obnova {0}s", "Úskok Shift / B · obnova {0}s", "Spurt Shift / B · Pause {0}s" },
            { "Projectile bounces: {0}", "Odrazy střely: {0}", "Odrazy strely: {0}", "Abpraller pro Schuss: {0}" },
            { "Extra charged-shot pellets: {0}", "Střely navíc při nabití: {0}", "Strely navyše pri nabití: {0}", "Zusätzliche geladene Geschosse: {0}" },
            { "Double damage every {0} hits", "Dvojnásobný zásah každých {0} úderů", "Dvojnásobný zásah každých {0} úderov", "Doppelter Schaden alle {0} Treffer" },
            { "Buy to unlock this trick.", "Zakoupením odemkneš tento trik.", "Zakúpením odomkneš tento trik.", "Kaufen, um diesen Trick freizuschalten." },
            { "Dash ready · Shift / B", "Úskok připraven · Shift / B", "Úskok pripravený · Shift / B", "Spurt bereit · Shift / B" },
            { "Dash ready in {0}s", "Úskok za {0}s", "Úskok za {0}s", "Spurt in {0}s" },
            { "Shift / B: dash after unlocking Clay dash.", "Shift / B: úskok po odemčení Hliněného úskoku.", "Shift / B: úskok po odomknutí Hlineného úskoku.", "Shift / B: Spurt nach Freischalten von Tonspurt." },
            { "Slam incoming · Jump!", "Blíží se úder · Skoč!", "Blíži sa úder · Skoč!", "Schlag kommt · Spring!" },
            { "Volley incoming · Keep moving!", "Blíží se salva · Uhýbej!", "Blíži sa salva · Uhýbaj!", "Salve kommt · Bleib in Bewegung!" },
            { "Charge incoming · Dodge!", "Blíží se výpad · Uhni!", "Blíži sa výpad · Uhni!", "Ansturm kommt · Weiche aus!" },
            { "Phase 1 · Fired clay", "Fáze 1 · Vypálená hlína", "Fáza 1 · Vypálená hlina", "Phase 1 · Gebrannter Ton" },
            { "Phase 2 · Blazing clay", "Fáze 2 · Žhavá hlína", "Fáza 2 · Žeravá hlina", "Phase 2 · Glühender Ton" },
        };

        private static readonly Dictionary<string, int> LabelRows = IndexRows(Labels);
        private static int language = Mathf.Clamp(PlayerPrefs.GetInt(Preference, 0), 0, Names.Length - 1);

        public static int Language { get { return language; } }
        public static event Action Changed;

        public static void Select(int value)
        {
            if (value == language || value < 0 || value >= Names.Length) return;
            language = value;
            PlayerPrefs.SetInt(Preference, value);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }

        public static string UI(string english)
        {
            int row;
            if (language == 1 || !LabelRows.TryGetValue(english, out row)) return english;
            return Labels[row, language == 0 ? 1 : language];
        }

        internal static Dictionary<string, int> IndexRows(string[,] entries)
        {
            var result = new Dictionary<string, int>(entries.GetLength(0), StringComparer.Ordinal);
            for (int row = 0; row < entries.GetLength(0); row++) result.Add(entries[row, 0], row);
            return result;
        }
    }
}
