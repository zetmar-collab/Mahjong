# Wyniki weryfikacji — Mahjong 1.1.0 — 28.10.2026

## Gotowe wydanie

EXE: artifacts/Mahjong-win-x64/Mahjong.exe. Wersja pliku 1.1.0.0, wersja produktu 1.1.0. Self-contained Windows x64, .NET 10.0.12. Własna ikona jest osadzona w EXE i oknie. Odczytano również ikonę z pliku wykonywalnego do artifacts/exe-icon-check.png.

## Wykonane sprawdzenia

- Kompilacja projektu WPF Release: 0 błędów i 0 ostrzeżeń. Publikacja self-contained: sukces. Log: artifacts/update-publish.txt.
- **20 zestawów testów silnika, 0 błędów; 378 pełnych rozwiązań odtworzonych przez Rules.Remove.** Log: artifacts/update-core-tests.txt. Starty obejmują 6 układów, 3 zestawy i 20 ustalonych seedów; dodatkowo 18 przetasowań.
- Nowe testy wyników: zapis metadanych, polskie znaki, odczyt, brak duplikatów, odmowa zapisu niedokończonej gry, nieprawidłowe nazwy i liczby, sortowanie od najnowszego, zachowanie uszkodzonej historii bez nadpisania.
- **459 sprawdzeń WPF, 0 błędów**, na końcowym EXE. Log: artifacts/update-ui-final-verified/ui-tests.txt.
- Pełne partie sześciu układów, podpowiedzi, powtarzane kliknięcia, równoczesne żądania nowej gry i przetasowania, pauza, PL/EN i zachowanie limitu przetasowania po odczycie.
- Sześć zapisanych ukończonych wyników, ponowny zapis bez duplikatu i historia zachowana po otwarciu nowej sesji.
- Import PNG, JPEG, BMP, GIF i TIFF. Usunięcie źródła nie usuwa tła; ponowne otwarcie zachowuje lokalną kopię i wybór.
- Błędny oraz większy niż 32 MB plik odrzucono bez zmiany obrazu i ustawienia. Brak zapisanej kopii daje komunikat i powrót do sukna. Filtr wyboru pliku: poprawny.
- Animacja zwycięstwa występuje raz; zapis wyniku i przebudowa ekranu nie powtarzają jej. Ograniczenie animacji wyłącza konfetti.
- Osadzona ikona okna wczytana poprawnie. Siedem WAV: nagłówki i wywołania odtwarzania.

## Kontrola wyglądu

39 obrazów WPF w artifacts/update-ui-final-verified/: gratulacje, historia PL/EN, własne tło i podgląd w ustawieniach, układy, tła, motywy i pomoc. Rasteryzacja 1280×720 i 1920×1080 przy 100%, 150%, 200%. Przejrzano gratulacje, historię i ustawienia oraz źródłową ikonę 256 px.

## Granice wyników

Windows 11 x64 build 26200, SDK 10.0.401. Brak osobnej próby na Windows 10. DPI sprawdzono przez rasteryzację WPF, bez zmiany systemowego skalowania i fizycznych monitorów. Nie obsługiwano ręcznie natywnego okna wyboru pliku; sprawdzono filtr i pełny import pięciu formatów. Nie potwierdzono odsłuchu na fizycznych głośnikach. Trudności nie kalibrowano badaniem z grupą graczy. EXE bez podpisu Authenticode. Raport opisuje testy lokalne sprzed publikacji; informacje o później dodanym instalatorze zawiera INSTALLER.md.

Instrukcje: README.md.
