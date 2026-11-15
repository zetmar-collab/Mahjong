# Weryfikacja Mahjong Solitaire 1.3.0

15 listopada 2026, Windows x64, SDK .NET 10.0.401.

- 39 zestawów silnika i danych, 0 błędów; 672 pełne rozwiązania w podstawowym liczniku, w tym 640 startów dla 8 plansz i 4 zestawów.
- 769 kontroli WPF, wszystkie zaliczone: pełne partie, rodzinna konfiguracja i punktacja, jadeitowe płytki, postęp, zapis, odłożenie aktualizacji, pauza i wznowienie po dialogu aktualizacji.
- Aktualizator: kontrolowane HTTP, przypięcie adresu repozytorium, wersje, limity, SHA-256, anulowanie, brak sieci i ponowna weryfikacja pliku przed uruchomieniem. Testy nie uruchamiają instalatora.
- Porównanie 40 pełnych ścieżek z generatorem 1.2.2: identyczne wyniki. Przykładowy lokalny pomiar: 39,6 ms przed indeksowaniem i 19,5 ms po nim; nie jest to benchmark całej gry.
- Nie aktualizowano rzeczywistej instalacji użytkownika. Brak oddzielnego testu na Windows 10; EXE pozostaje bez podpisu cyfrowego.

[Raport 1.3.0](docs/releases/v1.3.0-family-updates.md). Logi lokalne: artifacts/v1.3-core-tests.txt oraz artifacts/ui-tests/ui-tests.txt.

## Historyczna weryfikacja 1.2.2

12 listopada 2026, Windows x64, SDK .NET 10.0.401.

- 561 kontroli WPF, wszystkie zaliczone, w tym regresja pozostającej nakładki ładowania i trafiania wskaźnika w płytki.
- 31 zestawów testów silnika i danych, 0 błędów; 378 pełnych rozwiązań w podstawowym liczniku.
- Test regresji przed poprawką odtworzył błąd już przy pierwszym układzie.
- Kompilacja Release: 0 błędów i ostrzeżeń.

Szczegóły: [raport 1.2.2](docs/releases/v1.2.2-loading.md). Log WPF: artifacts/ui-tests/ui-tests.txt.

## Historyczna weryfikacja 1.2.1

7 listopada 2026, Windows 11 x64, SDK .NET 10.0.401.

- 31 zestawów testów silnika i danych, 0 błędów; 378 pełnych rozwiązań w podstawowym liczniku oraz dodatkowe scenariusze wyzwań i kodów.
- 521 kontroli WPF, wszystkie zaliczone — również z faktycznej instalacji.
- 1693 kontrole instalacji PL, ponownej instalacji EN i odinstalowania, 0 błędów; zweryfikowano 417 plików według manifestu SHA-256 po obu instalacjach.
- Kompilacja Release: 0 błędów i ostrzeżeń.
- Dane rzeczywistego gracza nie zmieniły się podczas testów instalatora.

Raport szczegółowy: docs/releases/v1.2.1-ui.md. Poprzednie raporty 1.2.0 i 1.1.0 są historyczne.

Logi lokalne: artifacts/installer-tests/471288b92f0f43db88f97122630ddb02/ oraz artifacts/ui-tests/.

Nie wykonano osobnej próby Windows 10, fizycznego testu wielu monitorów, odsłuchu audio ani pełnego testu czytnika ekranu. Pomiary kontrastu dotyczą wskazanych par kolorów i nie są deklaracją pełnej zgodności z WCAG. EXE oraz setup pozostają bez podpisu cyfrowego.
