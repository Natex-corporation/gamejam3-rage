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
