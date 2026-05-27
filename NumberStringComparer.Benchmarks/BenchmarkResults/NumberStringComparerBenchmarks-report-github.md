### Ran with following loop bounds on 05-26-2026, taking 10 minutes:
- _mixedLong: 20,000
- _alphanumeric: 20,000
- _commaSeparated: 20,000

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8457/25H2/2025Update/HudsonValley2)
Intel Core i7-10750H CPU 2.60GHz (Max: 2.59GHz), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.314
  [Host]     : .NET 9.0.16 (9.0.16, 9.0.1626.22923), X64 RyuJIT x86-64-v3
  Job-RUIMLP : .NET 9.0.16 (9.0.16, 9.0.1626.22923), X64 RyuJIT x86-64-v3

MaxIterationCount=50  
```

| Method                  | Mean       | Error     | StdDev    | Median     | Gen0        | Allocated  |
|------------------------ |-----------:|----------:|----------:|-----------:|------------:|-----------:|
| MixedLong_Original      | 3,346.1 ms |  78.74 ms | 157.25 ms | 3,321.6 ms | 260000.0000 | 1560.74 MB |
| MixedLong_Current       | 1,111.8 ms |  56.63 ms | 107.75 ms | 1,082.7 ms |  87000.0000 |  523.43 MB |
| Alphanumeric_Original   | 3,517.4 ms | 119.05 ms | 237.75 ms | 3,420.4 ms | 334000.0000 | 2004.69 MB |
| Alphanumeric_Current    |   980.2 ms |  18.25 ms |  25.58 ms |   980.8 ms | 121000.0000 |  728.21 MB |
| CommaSeparated_Original |   857.0 ms |  16.92 ms |  32.61 ms |   862.1 ms | 169000.0000 | 1014.19 MB |
| CommaSeparated_Current  |   531.6 ms |  15.84 ms |  31.26 ms |   533.7 ms |  98000.0000 |   589.3 MB |

----

This shows a significant memory usage drop which was the whole point of this exercise.