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
- utrudniona ewolucja schematu 
- gorsze skalowanie w przypadku 
- wymusza uzycie ORMa zamiast odczytywania gotowych obiektow
