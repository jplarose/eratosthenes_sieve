# Prime Sieve Library — Correctness & Performance Report

This test suite is designed to validate correctness and provide a readable, console‑first performance report for the sieve implementation.

The core API used by the tests:

```csharp
long NthPrime(long n0Based);
```
where **`prime[0] = 2`**.

Internally, the implementation can choose between a **Regular Sieve** (single contiguous bitset) and a **Segmented Sieve** (bounded working set). A consolidated MSTest class produces a readable **report** in the console that validates correctness and explains when each method is preferable.

---

## Validation Fixtures

Place these in a `fixtures/` folder and mark them **Copy to Output Directory**:

- **First 10,000 primes**: `fixtures/primes_10k.csv`  
  Columns: `index_0_based,prime` (0 → 2).  
- **Checkpoints**: `fixtures/prime_checkpoints.json`  
  Contains:
  - π(x) counts for x ∈ {10, 100, 1k, 10k, 100k, 1M}  
    (e.g., π(10)=4, π(100)=25, π(1k)=168, π(10k)=1,229, π(100k)=9,592, π(1M)=78,498)
  - pₙ values for n ∈ {10, 100, 1k, 10k, 100k} (1‑based) with their 0‑based index, e.g.,  
    p₁₀=29, p₁₀₀=541, p₁₀₀₀=7,919, p₁₀₀₀₀=104,729, p₁₀₀₀₀₀=1,299,709

Two small tests consume these fixtures:
1) **First‑10k exact match**: the first 10k generated primes must match `primes_10k.csv` element‑wise.  
2) **Checkpoint sweep**: verify π(x) counts and spot‑check pₙ values via `NthPrime`.

---

## Tests (single consolidated class)

- **Correctness smoke checks** (small, medium, large n)  
- **Head‑to‑head timings** at key points using **Median/IQR** (multiple trials, warm‑ups)  
- **Segment size sensitivity** sweeps (for segmented sieve)  
- **Auto‑threshold sanity** (below/at a threshold, auto mode must match the forced mode)  
- **Crossover point analysis** (robust):  
  - warm‑ups + multiple trials  
  - **Median/IQR** timing (no I/O in measured regions)  
  - **moving‑median smoothing** of the seg/reg runtime ratio  
  - **persistence rule** (segmented must win in ≥K of the next Q points)  
  - fallback to **global smoothed minimum** if no persistent crossing appears

This avoids outliers (e.g., a single slow run at 70k) from dictating the recommendation.

---

## Sample Console Output

The following is an example of the console report the tests print. Formatting here matches the test output.

```
╔══════════════════════════════════════════════════════════════════════╗
║ CROSSOVER POINT • ROBUST ANALYSIS (Median/IQR + smoothing + persistence) ║
╚══════════════════════════════════════════════════════════════════════╝
We search coarse→fine, compute medians, smooth ratios, and require a persistent win for Segmented.

— Searching 50,000..150,000 (step 10,000) —
        n │  Reg Med │ Reg IQR │  Seg Med │ Seg IQR │ Ratio │ Winner
  50,000  │    13.2  │    1.1  │    16.1  │    1.3  │  1.22 │ Regular
  60,000  │    12.5  │    1.0  │    12.4  │    1.0  │  0.99 │ Segmented
  70,000  │    13.0  │    1.0  │    19.1  │    1.5  │  1.47 │ Regular
  ...

— Searching 90,000..110,000 (step 2,000) —
        n │  Reg Med │ Reg IQR │  Seg Med │ Seg IQR │ Ratio │ Winner
  98,000  │    22.7  │    1.8  │    22.3  │    1.7  │  0.98 │ Segmented
 100,000  │    23.1  │    1.9  │    23.4  │    1.9  │  1.02 │ Regular
 102,000  │    24.6  │    2.0  │    22.1  │    1.7  │  0.90 │ Segmented
  ...

— Summary • Crossover Decision —
  Smoothing: moving median window = 5
  Persistence: need 3 of next 5 points to keep Segmented faster (ratio < 0.98)
  First persistent crossing: 110,000
  Global best (smoothed): n=130,000, smooth ratio=0.77

  ⇒ Recommended threshold: 110,000
```

---

## Theory Card (printed in test summary)

- **Complexity**: both sieves perform ~O(n log log n) work over the scanned range.  
- **Memory model**:  
  - Regular: ~1 bit per odd integer up to pₙ (fastest if it fits in cache/RAM).  
  - Segmented: bounded working set = segment size × odd density (more overhead, scales better).  
- **Upper bound for pₙ** (practical): `n (ln n + ln ln n)`; asymptotic: `n (ln n + ln ln n − 1)`.

---

## Roadmap: Beyond Millions → Billions

To reach **billion‑scale n** without scanning the entire range:

1. **Prime counting π(x)** — implement **Meissel–Lehmer** (or LMO) for sublinear π(x).  
2. **Binary search** for x where π(x) = n+1 (remember: the API is 0‑based).  
3. **Local segmented sweep** around x to locate pₙ exactly.

This “**count → zoom**” strategy avoids linear scanning of massive intervals and is the standard approach at scale.

---

## How to Run

1. Put the fixtures in `./fixtures/` and set **Copy to Output Directory**.  
2. Run the MSTest suite (the consolidated class).  
3. Read the console output — it’s a self‑contained report (correctness + performance + recommended threshold for your host).

You can run everything or target the consolidated suite. I include the console logger so you see the detailed report inline:

```bash
# Run all tests (recommended for a first pass)
dotnet test --logger:"console;verbosity=detailed"

# Run just the consolidated report class
# (exact class name from the test file)
dotnet test --filter FullyQualifiedName~PerformanceTests --logger:"console;verbosity=detailed"

```

---

## Summary

- **One API**: `NthPrime(long n0Based)` (0‑based; 2 is index 0).  
- **One test class**: correctness + performance + crossover report.  
- **Fixtures**: `primes_10k.csv`, `prime_checkpoints.json`.  
- **Robust decisions**: medians, IQR, smoothing, persistence; outlier‑resistant.  
- **Scalable plan**: π(x) + binary search + local segmented sweep for billion‑scale n.
