# studia-projektowanie-aplikacji-internetowych

# Opis projektu

Projekt to aplikacja webowa typu mini-commerce dla osób chcących kupować/sprzedawać odlewki perfum.
Kluczowe funkcjonalności:
- Dodanie swoich perfum na sprzedaż przy określonych cenach
- Przeglądanie ofert innych uzytkowników
- Mozliwość złożenia zamówienia
- Mozliwość oceniania innych użytkowników po zawarciu transakcji

# Stos technologiczny:
- frontend: Blazor WebAssembly
- backend: .NET 
- baza danych: SQLite (+ EF)

# ADR - Architecute Decision Record

## ADR 01 - Baza danych - relacyjna vs. nierelacyjna

### 1. Decyzja
Dla tego typu aplikacji dobrze sprawdzi sie baza relacyjna.

### 2. Kontekst
Aplikacja wymaga bazy danych do persystencji danych. Do rozważenia mamy bazy relacyjne vs. nierelacyjne. Aplikacja to mini-ecommerce z dobrze zdefiniwoanami encjami i wlasnosciami kazdej z nich.

### 3. Alternatywy

 - Nierelacyjna baza danych (np. mongodb)

### 4. Uzasadnienie
W obrebie aplikacji funkcjonuja jedynie dobrze zdefiniowane encje o konkretnych wartosciach - z gory wiemy jakie wlasnosci bedzie mial user, jakie perfum. Relacje miedzy tymi encjami nie beda ulegaly zmianom i sa latwe do przewidzenia. Dodatkowo, skoro aplikacja ma byc ecommercem, to musimy zapewnic transakcyjnosc, a to jest cos z czym bazy nosql nie radza sobie najlepiej. Zasadniczo zadne z zalet baz nosql nie beda tu mialy wiekszego zastosowania (ewolucja schematu, skalowalnosc, obiektowosc)

### 5. Tradeoffy
- utrudniona ewolucja schematu, wymusza migracje
- gorsze skalowanie 
- wymusza uzycie ORMa zamiast odczytywania gotowych obiektow


## ADR 02 - Baza danych

### 1. Decyzja
**PostgreSQL**

### 2. Kontekst
Po zadecydowaniu ze uzyta zostanie relacyjna baza danych, balezy zdecydowac sie na konkretna baze.

### 3. Alternatywy
- SQLite - nie nadaje sie do zastosowan gdzie uzytkownikow jest wielu i zapisy nie sa rzadkoscia, ze wzgledu na lockowanie calosci bazy przy zapisie
- Microsoft SQL Server - rozwiazanie closed source, bardzo drogie licencje

### 4. Uzasadnienie
Postgres jest darmowy, w pelni open-source i jest de facto standardem. Poradzi sobie z dowolna skala ktora jest przewidziana dla tego typu aplikacji. Ma integracje z Entity Frameworkiem. Jest latwo dostepny do hostowania u providerow chmury.

### 5. Tradeoffy
- trzeba stawiac osobny serwer (kontener) zamiast pojedynczego pliku jak w sqlite
- potencjalny overkill, wieksze zuzycie zasobow niz sqlite
