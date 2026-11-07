# Instalator Mahjong Solitaire

Aktualny instalator: **[Mahjong Solitaire 1.2.1 — Windows x64](https://github.com/zetmar-collab/Mahjong/releases/download/v1.2.1/Mahjong-Solitaire-1.2.1-Setup-x64.exe)**. Lokalna kopia: artifacts/installer/Mahjong-Solitaire-1.2.1-Setup-x64.exe.

Weryfikacja 1.2.1: **1693 kontrole instalatora i 521 kontroli WPF**, bez błędów. [Pełny raport UI i instalacji](docs/releases/v1.2.1-ui.md). Starsze wyniki poniżej są historyczne.

## Weryfikacja 1.2.0 (7.11.2026)

Instalacja PL, ponowna instalacja EN, kontrola 417 plików i odinstalowanie: **1693 kontrole, 0 błędów**. Zainstalowana aplikacja zaliczyła **496 kontroli WPF**. Dane rzeczywistego gracza pozostały bez zmian. Szczegóły i sumy kontrolne: [raport 1.2.0](docs/releases/v1.2.0-validation.md).

## Dla gracza

Uruchom Setup.exe, wybierz Polski lub English i wykonaj kroki kreatora. Instalator zawiera grę, wszystkie lokalne zasoby, ikonę, instrukcję i runtime .NET. Internet i osobna instalacja .NET nie są potrzebne.

Instalacja jest dla bieżącego użytkownika, bez wymagania administratora. Domyślny katalog: **%LOCALAPPDATA%/Programs/QuietMahjong**. Skróty w menu Start obejmują grę, instrukcję i odinstalowanie. Skrót na pulpicie można odznaczyć. Po instalacji można od razu uruchomić grę.

Kolejne uruchomienie instalatora aktualizuje tę samą aplikację. Zamknij grę przed aktualizacją. Zapis partii, wyniki, ustawienia i własny obraz pozostają w **%LOCALAPPDATA%/QuietMahjong**. Deinstalator nie usuwa tego katalogu. Odinstalowanie jest dostępne także przez Ustawienia Windows → Aplikacje.

Instalator nie ma podpisu cyfrowego wydawcy. Nie został przetestowany na osobnym komputerze z Windows 10; testy wykonano na Windows 11 x64.

## Odtworzenie instalatora

Wymagania dla budującego: Windows, PowerShell 7, SDK .NET 10 oraz Inno Setup 7. Gracz nie potrzebuje żadnego z tych narzędzi. Oficjalny kompilator: https://jrsoftware.org/isdl.php.

~~~powershell
pwsh -File ./scripts/build-installer.ps1 -CompilerPath 'C:/Program Files/Inno Setup 7/ISCC.exe'
~~~

Skrypt publikuje self-contained win-x64, tworzy świeżą kopię plików bez SmokeData, SmokeResults i PDB, generuje manifest SHA-256, kompiluje pojedynczy EXE oraz zapisuje jego sumę kontrolną. W celu opakowania już przetestowanego wydania:

~~~powershell
pwsh -File ./scripts/build-installer.ps1 -SkipPublish -CompilerPath 'C:/Program Files/Inno Setup 7/ISCC.exe'
~~~

Źródła: **installer/Mahjong.iss**, **installer/USER_GUIDE.txt**, **installer/wizard-large.png**. Wersja jest odczytywana z projektu aplikacji; AppId jest stały między aktualizacjami. Instalator używa Inno Setup 7.1.1; pobrany kompilator miał prawidłowy podpis Pyrsys B.V. Kompilator w tym środowisku znajduje się lokalnie w artifacts/tools/inno-7.1.1; nie jest dołączany do instalatora gry.

Szczegóły ostatniego budowania: **artifacts/installer/build-info.json**. Manifest plików i log kompilacji: katalog wskazany przez BuildFolder. Test instalacji, aktualizacji i odinstalowania: **scripts/test-installer.ps1**. Ten test odmawia działania, jeśli aplikacja jest już zarejestrowana jako zainstalowana, żeby nie zmienić istniejącej instalacji gracza.

Licencja Inno Setup: **installer/Inno-Setup-LICENSE.txt**; jej kopia jest dołączona do zainstalowanej gry. Licencje gry i .NET opisano w THIRD_PARTY_NOTICES.md.

## Wynik wykonanego testu (29.10.2026)

Instalacja PL, aktualizacja EN, kontrola 417 plików i odinstalowanie: **1696 kontroli, 0 błędów**. Gra uruchomiona z katalogu instalacji przeszła **459 kontroli WPF**. Dane rzeczywistego gracza pozostały bez zmian. Pełny raport: **installer/INSTALLER_TEST_RESULTS.md** (kopia obok Setup.exe: **artifacts/installer/TEST_RESULTS.md**).
