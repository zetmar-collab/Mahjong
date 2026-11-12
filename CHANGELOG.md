# Changelog

## 1.2.2 — 2026-11-12

- Naprawiono pozostającą na planszy nakładkę „Ładowanie nowej planszy…” po uruchomieniu nowej gry. Plansza odświeża się po zakończeniu ładowania i ponownie przyjmuje kliknięcia.
- Dodano testy WPF usunięcia nakładki, trafiania wskaźnikiem w płytkę i wyboru przez przycisk dla sześciu układów, ponownej gry, wyzwania dnia, kodu partii i przetasowania.

## 1.2.1 — 2026-11-07

- Ujednolicony wygląd formularzy, list, suwaków, rozwijanych sekcji i pasków przewijania we wszystkich motywach.
- Nowa hierarchia menu, karty ustawień, opisane filtry wyników i stale widoczne przyciski nowej gry.
- Poprawiony kontrast jasnego motywu, osobna obwódka fokusu bez przesuwania treści i nazwy przycisków zoomu.
- Usunięto przezroczyste brzegi proceduralnego tła lasu.
- Własne logo w wysokiej rozdzielczości, ikona pełnego ekranu rysowana wektorowo i uporządkowane statystyki gry.
- 521 kontroli WPF, w tym pomiary kontrastu i układ okna 1000 × 680; 31 zestawów testów silnika.

## 1.2.0 — 2026-11-07

- Naprawiono wszystkie siedem usterek potwierdzonych w audycie: zakres czasu, ochronę zapisu przed drugą instancją, skróty w polu nazwy, subskrypcje widoków, czas ponad dobę, budżet solvera i datę ukończenia.
- Cofanie par (również Ctrl+Z), z zapisem historii ruchów. Czas i pomoc pozostają naliczone; nie można cofać przez przetasowanie ani po zwycięstwie.
- Automatyczny zapis zwycięstwa, późniejsze jednorazowe uzupełnienie nazwy i stała data ukończenia.
- Filtrowana, wirtualizowana historia oraz rekordy osobno dla układu, zestawu i kategorii pomocy.
- Eksport/import kopii danych z sumami SHA-256, walidacją archiwum, obrazu i JSON oraz zachowaniem poprzednich plików.
- Powiększanie planszy 100–300%, przewijanie, trzy interaktywne ćwiczenia PL/EN, wyzwanie dnia offline i kody całego początkowego rozkładu MJ1.
- Limity plików danych, uporządkowany zapis w tle, odświeżanie samego zaznaczenia, ograniczony log diagnostyczny i testy WPF w CI.
- Zachowano odczyt zapisów i wyników wersji 1.1; nie wykonano migracji danych gracza.

## 1.1.0 — 2026-10-28

- Ręczne zapisywanie wyników ukończonych partii, opcjonalna nazwa gracza i lokalna historia bez duplikatów.
- Własne tło z PNG, JPEG, BMP, GIF lub TIFF, kopiowane do danych aplikacji i zachowane po ponownym uruchomieniu.
- Komunikat „Gratulacje!” z konfetti respektującym ograniczenie animacji.
- Własna ikona EXE i okna w siedmiu rozmiarach.
- Testy trwałości wyników, poprawności importu i zachowania stanu po błędzie obrazu.

## 1.0.0 — 2026-10-28

- Offline Mahjong Solitaire dla Windows x64, C# / WPF / .NET 10.
- Sześć geometrii, trzy zestawy, gwarantowane rozwiązania startowe.
- Jedno transakcyjne przetasowanie, podpowiedzi, zapis i wznowienie.
- PL / EN, trzy motywy, pięć teł, lokalne efekty dźwiękowe.
- Testy silnika oraz zintegrowane testy aplikacji WPF.
