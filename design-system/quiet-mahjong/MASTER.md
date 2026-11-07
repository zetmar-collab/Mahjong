# Quiet Mahjong — wygląd aplikacji WPF

Kierunek zatwierdzony przez implementację i obrazy kontrolne 7.11.2026.

## Założenia

Spokojna, jednoosobowa gra desktopowa offline. Zachowujemy dotykowe, kremowe płytki, sukno i zielono-złotą identyfikację. Powłoka jest prostsza od planszy: bez rozmyć, animowanych teł, zewnętrznych fontów czy dodatkowych silników grafiki.

Wskazówki lokalnego skilla ui-ux-pro-max dla WPF: centralne zasoby, nazwy dostępności i widoczny fokus. Wyszukiwanie design-system dla board game wskazało sukno i złoto; internetowe układy landing page, liquid glass i WebGL nie odpowiadają tej aplikacji i nie zostały zastosowane.

## Kolory

| Token | Ciemny | Jasny | Wysoki kontrast |
| --- | --- | --- | --- |
| Surface | #102722 | #F3F1E9 | #000000 |
| Panel | #19372F | #FFFFFF | #101010 |
| Text | #F7F3E8 | #203A30 | #FFFFFF |
| Muted | #B8C9BF | #52665B | #FFFFFF |
| Accent | #E8C782 | #264E3C | #FFE15B |
| AccentInk | #192D25 | #FFFFFF | #000000 |
| AccentText | #E8C782 | #264E3C | #FFE15B |
| Focus | #91DCC5 | #276650 | #77EDFF |

Oddzielamy wypełnienie głównego przycisku od jego tekstu i od akcentu tekstowego. TextBlock nie wymusza globalnie koloru — etykieta przycisku dziedziczy właściwy AccentInk. Teksty nad dowolnym tłem zdjęciowym mają nieprzezroczystą podkładkę.

## Typografia i odstępy

Systemowe Segoe UI dla interfejsu i Georgia dla głównych tytułów. Treść 15–16, opisy 12–14, nagłówki sekcji 20, tytuły 30–46 jednostek WPF. Nie sprowadzamy zwykłych etykiet do 10–11. Odstępy z rytmu 4/8: karty 20 padding, przerwy sekcji 16, promienie kontrolek 8–10 i paneli 16.

## Hierarchia i zachowanie

- W menu wyróżniamy Kontynuuj, jeżeli istnieje zapis; w przeciwnym przypadku Nową grę. Pozostałe działania są drugorzędne.
- Na ekranie wyboru gry przyciski pozostają poza przewijaną treścią. Kod partii jest opcjonalnym rozwijanym panelem.
- Wybrany układ/zestaw/tło ma również tekst Wybrano, nie tylko inny kolor.
- Ustawienia grupujemy w interfejs/dostępność, dźwięki, tła i dane. Zmiany nadal zapisują się automatycznie.
- Filtry wyników mają widoczne etykiety i powiązania dostępności. Wiersze pozostają wirtualizowane.
- Kontrolki formularzy, suwaki, rozwijane listy i paski przewijania współpracują z motywem.
- Przycisk ma co najmniej 44 × 44 jednostki; nacisk daje zmianę przezroczystości. Fokus jest osobną obwódką poza geometrią treści, nie zmienia układu.
- Nie dodajemy nowych animacji. Istniejące podpowiedzi i konfetti nadal respektują ograniczenie ruchu.

## Weryfikacja

521 kontroli WPF. Automatycznie sprawdzane pary tekst/tło mają kontrast >=4.5:1 we wszystkich trzech motywach. Testujemy też rozwijane listy, dziedziczenie koloru głównego przycisku, okno minimalne 1000 × 680, wcześniejsze rozdzielczości i rastry DPI. To nie jest deklaracja pełnej zgodności z WCAG ani test fizycznego DPI/czytnika ekranu.
