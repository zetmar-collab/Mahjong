# Mahjong Solitaire na Windows — polecenie dla modelu programistycznego

## 1. Zadanie

Stwórz kompletną, działającą grę Mahjong Solitaire na Windows, zgodnie z poniższą specyfikacją. Dostarcz kod źródłowy, wszystkie wymagane zasoby, testy logiki i instrukcję uruchomienia oraz zbudowania aplikacji. Nie poprzestawaj na opisie, makiecie ani pseudokodzie.

To gra jednoosobowa polegająca na zdejmowaniu par klocków z warstwowej planszy. Nie chodzi o wieloosobową grę mahjong.

Pracuj samodzielnie. Przy zwykłych decyzjach technicznych przyjmuj rozsądne założenia i zapisuj je w README. Pytaj tylko wtedy, gdy brak odpowiedzi rzeczywiście uniemożliwia realizację. Nie publikuj aplikacji i nie wysyłaj plików do zewnętrznych usług bez zgody użytkownika.

## 2. Technologia i sposób dostarczenia

- Aplikacja desktopowa dla Windows 10 i 11, x64, działająca offline.
- Preferowany stos: C#, WPF, MVVM oraz wspierana stabilna wersja .NET dostępna w środowisku. Sprawdź zgodność narzędzi przed rozpoczęciem i zapisz użyte wersje.
- Oddziel silnik zasad i generator plansz od interfejsu. Logikę powinno dać się testować bez uruchamiania okna gry.
- Zapewnij wersję przenośną: folder z plikiem EXE i zasobami. Preferuj publikację self-contained, aby gracz nie musiał osobno instalować .NET.
- Dołącz dokładne polecenia budowania, testowania i publikacji. Instalator jest opcjonalny.
- Nie zastępuj aplikacji stroną internetową ani rozwiązaniem wymagającym serwera.
- Używaj własnych zasobów albo zasobów o odpowiednich licencjach. Dodaj informacje o autorach i licencjach. Unikaj zależności od zdalnych fontów, emoji i płatnych usług.

## 3. Zasady gry

1. Gracz usuwa dwie pasujące do siebie, wolne płytki.
2. Płytka jest wolna, jeżeli nie przykrywa jej żadna inna płytka i ma całkowicie odsłoniętą co najmniej jedną boczną krawędź: lewą lub prawą.
3. Odsłonięta góra lub dół płytki nie wystarcza do jej odblokowania.
4. Uwzględnij rzeczywiste zachodzenie prostokątów, także przy przesunięciach o pół szerokości lub wysokości. Płytka może być przykryta przez kilka innych płytek.
5. Wybranie pierwszej wolnej płytki zaznacza ją. Ponowne kliknięcie usuwa zaznaczenie. Kliknięcie drugiej pasującej i wolnej płytki usuwa parę.
6. Kliknięcie innej wolnej, niepasującej płytki przenosi zaznaczenie na nią. Kliknięcie zablokowanej płytki nie usuwa pary i daje subtelną informację zwrotną.
7. Po każdym ruchu przelicz dostępne pary i sprawdź stan partii.
8. Usunięcie wszystkich płytek oznacza zwycięstwo. Brak ruchów nie oznacza zwycięstwa.

Klasyczny zestaw 144 płytek: 108 płytek trzech kolorów liczbowych, 16 wiatrów, 12 smoków, 4 kwiaty i 4 pory roku. Zwykłe płytki pasują do identycznych płytek. Dowolny kwiat pasuje do dowolnego kwiatu, a dowolna pora roku do dowolnej pory roku. Kwiat nie pasuje do pory roku. Wyjaśnij te wyjątki w pomocy PL i EN.

W zestawach geometrycznym i przyrodniczym domyślnie paruj identyczne symbole. Zachowaj parzyste liczebności wszystkich klas dopasowania. Oddziel identyfikator symbolu, klasę dopasowania i wygląd graficzny. Zmiana zestawu w ustawieniach dotyczy następnej nowej partii; nie zmienia reguł bieżącej gry.

## 4. Zestawy klocków

Dostarcz minimum trzy kompletne zestawy, dostępne w wyborze nowej gry wraz z podglądem:

| Zestaw | Charakter |
| --- | --- |
| Geometryczny | Łatwe do odróżnienia koła, trójkąty, kwadraty, romby, gwiazdy i inne figury. Duże symbole, wyraźny kontur. |
| Natura | Czytelne liście, kwiaty, drzewa, owoce i zwierzęta. Unikaj drobnych szczegółów i niemal identycznych ikon. |
| Klasyczny | Tradycyjne kręgi, bambusy, znaki, wiatry, smoki, kwiaty i pory roku. |

Nie rozróżniaj płytek wyłącznie kolorem. W większych zestawach użyj odrębnych sylwetek oraz czytelnych dodatkowych oznaczeń lub liczb. Podstawowe figury muszą być łatwe do rozpoznania również przez początkujących. Dla klasycznych znaków zapewnij opcjonalne oznaczenia pomocnicze.

Każda płytka ma mieć widoczne krawędzie i delikatny cień. Rozróżnij stan zwykły, zaznaczony, wskazany podpowiedzią i zablokowany. Zablokowane płytki mają pozostać czytelne.

## 5. Układy i poziomy trudności

Dostarcz minimum sześć rzeczywiście różnych, poprawnych geometrycznie układów:

| Układ | Poziom | Liczba płytek | Założenie |
| --- | --- | --- | --- |
| Polana | Bardzo łatwy | 36 | Jedna warstwa, dużo wolnych boków. |
| Schodki | Łatwy | 72 | Niskie stosy i liczne dostępne pary. |
| Motyl | Średni | 108 | Symetryczne skrzydła, kilka warstw. |
| Żółw | Średnio trudny | 144 | Klasyczny, rozpoznawalny układ żółwia. |
| Twierdza | Trudny | 144 | Więcej warstw i wąskie miejsca ograniczające dostęp. |
| Smok | Bardzo trudny | 144 | Rozbudowane zależności odblokowywania i mniej dostępnych ruchów. |

Trudność wynika przede wszystkim z geometrii i liczby dostępnych decyzji, a nie z niskiej czytelności obrazków. Opisy poziomów są projektowymi założeniami; zweryfikuj je przez testy i rozgrywki.

Przechowuj układy jako dane, np. JSON: identyfikator pozycji, współrzędne i warstwa. Waliduj liczbę pozycji, unikalność, brak niedozwolonych przecięć w jednej warstwie i spójność relacji blokowania. Mniejsze plansze używają odpowiedniego podzbioru symboli z parzystymi liczebnościami.

## 6. Generowanie rozwiązywalnych partii

- Każda nowa partia musi mieć co najmniej jedną pełną, legalną sekwencję usuwania wszystkich par.
- Samo sprawdzenie, że istnieje jedna para na początku, nie dowodzi rozwiązywalności.
- Zastosuj generator z konstruktywną gwarancją rozwiązania albo solver sprawdzający pełne rozwiązanie. Opisz, skąd bierze się gwarancja.
- Przykładowa poprawna konstrukcja: dla pustych typów na ustalonej geometrii wyznacz kompletną sekwencję zdejmowania wolnych pozycji parami, a następnie przypisz dopasowane symbole do tych par. Cofaj wybory, gdy geometria prowadzi do ślepego zaułka.
- Zapisz seed oraz sekwencję kontrolną. W testach odtwórz ją przez ten sam silnik zasad, którego używa interfejs.
- Generowanie nie może zawieszać okna. Zastosuj limit czasu lub prób i bezpieczny wariant zapasowy, np. wcześniej zweryfikowany rozkład dla danego układu. Nie przedstawiaj niezweryfikowanej planszy jako gwarantowanie rozwiązywalnej.
- Wyjaśnij w pomocy, że rozwiązywalny start nie gwarantuje sukcesu po dowolnych decyzjach gracza.

## 7. Podpowiedź

- Przycisk „Podpowiedź” / „Hint” wskazuje jednocześnie dwie pasujące, obecnie wolne płytki.
- Wyróżnij je wyraźną obwódką i krótką, spokojną animacją. Nie usuwaj ich automatycznie.
- Podpowiedzi są wielokrotnego użytku. Ich licznik jest widoczny, bez obowiązkowej kary punktowej.
- Przy kolejnych kliknięciach bez zmiany planszy można cyklicznie pokazywać dostępne pary.
- Jeżeli nie ma pary, pokaż jasny komunikat i możliwość przetasowania, jeśli pozostała.
- Nie nazywaj zwykłej legalnej pary „najlepszym ruchem”. Obietnica bezpiecznego ruchu wymaga sprawdzenia pełnego rozwiązania aktualnego stanu.

## 8. Jedno przetasowanie na partię

Przyjmij następującą jednoznaczną interpretację: gracz ma od początku każdej nowej partii jedno przetasowanie dostępne w dowolnym momencie. Nie jest to restart partii.

1. Przycisk pokazuje „Przetasuj (1)” / „Shuffle (1)”.
2. Przetasowanie zmienia przypisanie symboli wyłącznie do pozycji pozostałych płytek. Nie zmienia geometrii, nie przywraca usuniętych par, nie zeruje czasu ani liczników.
3. Zachowuje dokładny multizbiór pozostałych symboli, także odrębnych kwiatów i pór roku.
4. Nowe przypisanie musi mieć pełne legalne rozwiązanie dla obecnej geometrii. Nie wystarcza wygenerowanie pojedynczego ruchu.
5. Jeżeli obliczenie się nie powiedzie, pozostaw poprzednią planszę i nie zużywaj przetasowania. Pokaż czytelny komunikat zamiast fałszywej gwarancji.
6. Limit zostaje zużyty dopiero po pomyślnym zatwierdzeniu nowego rozkładu. Zapisz zmianę razem ze stanem gry.
7. Po wykorzystaniu przycisk pokazuje „Przetasuj (0)” i jest nieaktywny do rozpoczęcia nowej partii.
8. Zablokuj wielokrotne wywołania podczas obliczeń i animacji.
9. Zamknięcie aplikacji i wznowienie gry nie odnawia limitu.
10. „Zagraj ponownie” rozpoczyna nową partię z początkowym rozkładem i nowym limitem. Jasno odróżnij tę opcję od wznowienia zapisu.

Jeśli nie ma legalnych par i limit został wykorzystany, wyświetl stan „Brak ruchów” oraz opcje nowej partii lub powrotu do menu. Nie tasuj automatycznie.

## 9. Tła, motywy i dźwięk

Dostarcz minimum pięć teł: zielone sukno, drewno, spokojny las, jasny papier i ciemne tło minimalistyczne. Mogą to być własne grafiki, tekstury lub starannie wykonane tła proceduralne. Zapewnij podgląd i natychmiastową zmianę bez restartu gry.

Tła planszy i motywy interfejsu to niezależne ustawienia. Dostarcz motywy: jasny, ciemny i wysoki kontrast. Przełączanie motywu nie zmienia układu ani symboli płytek. Utrzymuj czytelny kontrast na każdym tle.

Dostarcz krótkie efekty: wybór płytki, usunięcie pary, niedozwolony ruch, podpowiedź, przetasowanie, zwycięstwo i brak ruchów. Zapewnij wyciszenie oraz regulację głośności efektów. Spokojna muzyka lub ambient są opcjonalne i mają osobną regulację. Dźwięki muszą działać offline; brak urządzenia audio nie może przerywać gry.

## 10. Język polski i angielski

- Pełne tłumaczenie wszystkich ekranów, przycisków, komunikatów, pomocy, ustawień i informacji o zwycięstwie.
- Przełącznik „Polski / English” działa podczas gry bez jej restartu.
- Przy pierwszym uruchomieniu wybierz polski dla polskich ustawień systemu, w pozostałych przypadkach angielski. Zapamiętaj ręczny wybór.
- Trzymaj teksty w zasobach lokalizacyjnych, bez tekstów interfejsu rozsianych po kodzie.
- Obsłuż polskie znaki i odmianę liczebników. Przykłady: „1 para”, „2 pary”, „5 par”; „1 płytka”, „2 płytki”, „5 płytek”. Uwzględnij liczby 12–14 i 22–24.
- Zmiana języka nie może zmieniać identyfikatorów zapisanych układów i ustawień.

## 11. Ekrany i obsługa

Menu główne: Nowa gra, Kontynuuj, Ustawienia, Jak grać, Wyjście. „Kontynuuj” jest aktywne tylko dla poprawnego zapisu.

Nowa gra: wybór układu z miniaturą, poziomem i liczbą płytek; wybór zestawu; podgląd oraz przycisk rozpoczęcia.

Ekran gry: plansza w centrum, czas, liczba pozostałych płytek, liczba dostępnych par, licznik podpowiedzi, przyciski podpowiedzi, przetasowania, pauzy i menu. Wyraźnie odróżnij liczbę kombinacji dostępnych par od liczby różnych symboli.

Pauza zatrzymuje czas aktywnej gry i zasłania płytki. Powrót do menu zapisuje partię. Rozpoczęcie nowej gry przy istniejącym zapisie wymaga potwierdzenia zastąpienia partii.

Interfejs ma być spokojny i estetyczny, bez reklam. Zapewnij zmianę rozmiaru okna, tryb pełnoekranowy, skalowanie DPI i dopasowanie całej planszy bez ucinania. Zweryfikuj co najmniej 1280×720 oraz 1920×1080 i skalowanie 100%, 150%, 200%. Nie zmniejszaj symboli do nieczytelnego rozmiaru; przewidź sensowne minimum okna.

Obsługa myszą i klawiaturą: Tab do przycisków, strzałki do wyboru płytki, Enter/Spacja do zaznaczenia, Esc do pauzy, H do podpowiedzi. Przy nawigacji omijaj usunięte pozycje i zapewnij widoczny fokus. Dodaj dostępne nazwy kontrolek oraz opcję ograniczenia animacji.

## 12. Zapis stanu

Automatycznie zapisuj po każdym zakończonym ruchu i przetasowaniu oraz przy wyjściu. Ustawienia zapisuj po zmianie. Użyj katalogu użytkownika w LocalApplicationData, bez wymagania praw administratora.

Zapis obejmuje: wersję formatu, identyfikator partii, układ i pozycje, symbole pozostałych płytek, stan usuniętych pozycji, seed, użyty zestaw, czas aktywnej gry, liczbę podpowiedzi i wykorzystanie przetasowania. Nie polegaj wyłącznie na seedzie, ponieważ bieżący stan może już zawierać przetasowanie.

Stosuj zapis atomowy i walidację przy wczytywaniu. Uszkodzony albo niezgodny plik nie może powodować awarii. Pokaż komunikat i pozwól rozpocząć nową partię. Nie zeruj po cichu wykorzystanego przetasowania. Przechowuj jedną aktywną partię; wiele profili i synchronizacja nie są wymagane.

## 13. Architektura i testy

Wydziel odpowiedzialności: model płytki i planszy, wykrywanie blokad, dopasowania, wyszukiwanie par, generator i solver, obsługa partii, zapis, ustawienia, lokalizacja oraz audio. Nazwy klas dobierz do projektu; unikaj niepotrzebnych warstw.

Testy automatyczne mają sprawdzać rzeczywiste reguły:

- Płytka przykryta, zablokowana z obu boków, wolna z lewej albo prawej; częściowe zachodzenie i kilka płytek przykrywających.
- Zgodne i niezgodne pary, kwiaty, pory roku oraz próba użycia tej samej płytki dwukrotnie.
- Wszystkie układy i pełne rozwiązania dla wielu ustalonych seedów oraz wszystkich zestawów.
- Podpowiedź zawsze wskazuje dwie różne, wolne i pasujące płytki albo poprawnie zgłasza brak ruchów.
- Przetasowanie zachowuje pozycje i multizbiór, daje pełne rozwiązanie, zużywa limit raz i nie zużywa go przy błędzie.
- Drugie przetasowanie jest blokowane również po zapisaniu i ponownym wczytaniu partii.
- Zapis i odczyt zachowują stan; uszkodzony zapis jest obsługiwany bez awarii.
- Wielokrotne kliknięcia nie usuwają pary dwa razy i nie uruchamiają kilku przetasowań.
- Kompletność kluczy PL/EN i poprawna odmiana liczebników.

Ręcznie sprawdź pełną rozgrywkę, widoczność warstw, motywy, tła, dźwięki, pauzę, zmianę języka, klawiaturę i skalowanie. Jeśli środowisko nie pozwala uruchomić aplikacji Windows, powiedz to wprost i podaj konkretne kroki weryfikacji. Nie deklaruj testów ani działania EXE bez ich faktycznego wykonania.

## 14. Kryteria odbioru

- [ ] Działająca aplikacja Windows, pełny kod i komplet lokalnych zasobów.
- [ ] Trzy zestawy płytek, w tym łatwa geometria, natura i klasyczny.
- [ ] Sześć układów o zróżnicowanej trudności.
- [ ] Poprawne reguły zdejmowania płytek i rozwiązywalne początki.
- [ ] Podpowiedź wyróżniająca legalną parę.
- [ ] Dokładnie jedno pomyślne przetasowanie na partię, z zachowaniem limitu po wznowieniu.
- [ ] Pięć teł i trzy niezależne motywy interfejsu.
- [ ] Efekty dźwiękowe, wyciszenie i regulacja głośności.
- [ ] Pełny polski i angielski, przełączane podczas gry.
- [ ] Zapis, wznowienie, pauza i obsługa braku ruchów.
- [ ] Czytelny interfejs i poprawne skalowanie.
- [ ] Wyniki wykonanych testów i jawnie opisane ograniczenia.
- [ ] README z budowaniem, publikacją, sterowaniem i licencjami.

## 15. Sposób realizacji i odpowiedź końcowa

Najpierw krótko przedstaw wybrany stos, strukturę projektu i istotne założenia. Następnie wykonaj implementację: silnik i testy, generator, interfejs, zasoby, ustawienia, zapis oraz publikację. Kontynuuj pracę do spełnienia kryteriów odbioru.

Przed doborem bibliotek sprawdź ich dokumentację i zgodność z wybranym środowiskiem. Nie wymyślaj API, zależności ani wyników testów. Gdy brakuje narzędzia do zbudowania EXE, dostarcz kompletny projekt i skrypt publikacji oraz wyraźnie wskaż, czego nie udało się uruchomić.

W odpowiedzi końcowej podaj: lokalizację projektu i dostępnego wydania, sposób uruchomienia, krótką listę wykonanych funkcji, wyniki testów i konkretne pozostałe ograniczenia. Nie zastępuj gotowego kodu obietnicą późniejszej realizacji.
