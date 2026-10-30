# Zasoby i licencje

Kod gry, układy JSON, ręcznie zdefiniowane wektory płytek, proceduralne tła i siedem syntetycznych plików WAV powstały w ramach tego projektu. Autor: Mahjong contributors. Licencja MIT w pliku LICENSE.

Grafika nie używa emoji, fontów z internetu, obrazów stockowych ani płatnych API. Znaki klasyczne są własnymi, uproszczonymi rysunkami wektorowymi. Segoe UI i Georgia są fontami systemowymi Windows i nie są dołączane do wydania. Jeśli Georgia jest niedostępna, WPF używa systemowego zastępnika.

Wydanie self-contained zawiera Microsoft .NET Runtime i Windows Desktop Runtime. Są to składniki platformy Microsoft, na licencjach opisanych w [dotnet/runtime](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) i [dotnet/wpf](https://github.com/dotnet/wpf/blob/main/LICENSE.TXT). Informacje o składnikach zewnętrznych platformy: [runtime THIRD-PARTY-NOTICES](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT), [WPF notices](https://github.com/dotnet/wpf/blob/main/THIRD-PARTY-NOTICES.txt).

Aplikacja nie używa dodatkowych pakietów NuGet. Instalacja SDK i pobranie pakietów runtime na etapie publikacji mogą wymagać internetu; gotowa gra działa offline.

Wersja 1.1 dodaje oryginalną ikonę płytki z kwiatem i wektorowe konfetti, również na licencji MIT. Ikonę odtwarza skrypt scripts/generate-icon.ps1; nie wymaga dodatkowych fontów ani pobierania zasobów.

Instalator zbudowano przy użyciu Inno Setup 7.1.1, autorzy: Jordan Russell i Martijn Laan. Zachowano oryginalne oznaczenia silnika instalacyjnego. Licencja: installer/Inno-Setup-LICENSE.txt w źródłach oraz Inno-Setup-LICENSE.txt w zainstalowanej grze. Oficjalna strona: https://jrsoftware.org/.
