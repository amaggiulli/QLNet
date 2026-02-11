```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7623)
Intel Core i9-9880H CPU 2.30GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.305
  [Host]     : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX2
  Job-EAYKKK : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX2
  ShortRun   : .NET 9.0.9 (9.0.925.41916), X64 RyuJIT AVX2

Jit=RyuJit  Platform=X64  Runtime=.NET 9.0  

```
| Method                                      | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Min       | Max      | Op/s  | Gen0     | Gen1     | Allocated |
|-------------------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|----------:|---------:|------:|---------:|---------:|----------:|
| &#39;YieldToCalls with bond creation (CURRENT)&#39; | Job-EAYKKK | Default        | Default     | Default     | 11.58 ms | 0.441 ms | 1.293 ms | 11.63 ms |  8.012 ms | 14.25 ms | 86.35 | 593.7500 | 500.0000 |   4.82 MB |
| &#39;YieldToCalls with bond creation (CURRENT)&#39; | ShortRun   | 3              | 1           | 3           | 10.80 ms | 3.075 ms | 0.169 ms | 10.82 ms | 10.623 ms | 10.96 ms | 92.58 | 593.7500 | 500.0000 |   4.82 MB |
