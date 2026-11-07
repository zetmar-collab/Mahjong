# Weryfikacja Mahjong Solitaire 1.2.1

7 listopada 2026, Windows 11 x64, SDK .NET 10.0.401.

- 31 zestawów testów silnika i danych, 0 błędów; 378 pełnych rozwiązań w podstawowym liczniku oraz dodatkowe scenariusze wyzwań i kodów.
- 521 kontroli WPF, wszystkie zaliczone — również z faktycznej instalacji.
- 1693 kontrole instalacji PL, ponownej instalacji EN i odinstalowania, 0 błędów; zweryfikowano 417 plików według manifestu SHA-256 po obu instalacjach.
- Kompilacja Release: 0 błędów i ostrzeżeń.
- Dane rzeczywistego gracza nie zmieniły się podczas testów instalatora.

Raport szczegółowy: docs/releases/v1.2.1-ui.md. Poprzednie raporty 1.2.0 i 1.1.0 są historyczne.

Logi lokalne: artifacts/installer-tests/471288b92f0f43db88f97122630ddb02/ oraz artifacts/ui-tests/.

Nie wykonano osobnej próby Windows 10, fizycznego testu wielu monitorów, odsłuchu audio ani pełnego testu czytnika ekranu. Pomiary kontrastu dotyczą wskazanych par kolorów i nie są deklaracją pełnej zgodności z WCAG. EXE oraz setup pozostają bez podpisu cyfrowego.
