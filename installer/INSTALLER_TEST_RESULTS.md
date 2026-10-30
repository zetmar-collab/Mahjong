# Testy instalatora — 29.10.2026

Plik: Mahjong-Solitaire-1.1.0-Setup-x64.exe
Wersja aplikacji: 1.1.0
Kompilator: Inno Setup 7.1.1 x64. Pobrany instalator kompilatora miał prawidłowy podpis Pyrsys B.V.
SHA-256 gotowego Setup.exe: 523b0f703f053754f8b18b113c3ab1c876daba4a4ba4d6bb3929a5dfb1ffae6f

## Wykonane testy

**1696 kontroli zakończonych pomyślnie, 0 błędów.**

- Uruchomiono rzeczywisty instalator w trybie cichym w języku polskim, w osobnym katalogu testowym projektu.
- Zarejestrowano aplikację w HKCU, ze zgodną wersją i ścieżką instalacji.
- Sprawdzono obecność oraz SHA-256 wszystkich **417 plików** po instalacji i po ponownym uruchomieniu instalatora w języku angielskim.
- Sprawdzono skróty Start, polską i angielską instrukcję, cel skrótu gry oraz włączenie i wyłączenie skrótu na pulpicie.
- Aktualizacja zachowała dodatkowy plik użytkownika w katalogu aplikacji.
- Zainstalowane EXE uruchomiło pełny zestaw **459 kontroli WPF**, wszystkie zakończone pomyślnie: rozgrywka, wyniki, własne tło, języki, podpowiedzi, przetasowanie i pozostałe funkcje.
- Deinstalator zakończył się kodem 0, usunął grę, dołączony runtime, skróty i rejestrację aplikacji.
- Deinstalator pozostawił dodatkowy plik użytkownika. Sumy kontrolne rzeczywistych zapisów gry, wyników, ustawień i tła w profilu użytkownika nie zmieniły się podczas całego testu.
- Dane testowe SmokeData/SmokeResults oraz symbole debugowania nie są dołączone do pakietu.
- Po testach nie pozostała zainstalowana testowa kopia gry ani wpis aplikacji w rejestrze.

## Pliki kontrolne

Logi instalacji PL, aktualizacji EN, odinstalowania, kontroli plików i WPF:
artifacts\installer-tests\bdff59fc7bd440c5bd4d99473b7cf97c

Manifest plików i log kompilatora:
artifacts\installer-build\54ad749cf1ad419fb14e9941d14ab739

Skrypt odtwarzający test: scripts/test-installer.ps1.

## Granice testu

Testowano Windows 11 x64 build 26200. Nie wykonano osobnej próby na Windows 10 ani ręcznej kontroli wszystkich stron kreatora. Oba języki instalacji sprawdzono w trybie cichym; weryfikacja skrótów i ich celów była automatyczna. Gotowy instalator gry nie ma podpisu Authenticode. Raport opisuje testy lokalne sprzed publikacji wydania. Dane użytkownika nie są usuwane przez deinstalator.
