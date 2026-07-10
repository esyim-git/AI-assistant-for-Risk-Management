# 54. Gate B/C Evidence - v0.7.1

## 0. Purpose And Current Decision

This is the release-specific evidence ledger and execution runbook for the published v0.7.1 portable ZIP.

- **Current decision: `BLOCKED`.** Packaging and local verification are complete, but v0.7.1 Test-PC evidence has not been attached.
- `docs/48_GateBC_v0.7.0_Evidence.md` is historical v0.7.0 evidence. Its user-reported PASS rows do not carry forward to v0.7.1.
- Gate B/C starts from the published asset, not from source or a local rebuild.
- A release cut, tag, or successful local test does not make Gate B/C PASS.

## 1. Package Source Of Truth

| Item | Value |
|---|---|
| Release | <https://github.com/esyim-git/AI-assistant-for-Risk-Management/releases/tag/v0.7.1> |
| Tag / Build Commit | `v0.7.1` / `fa7552567cb432ec6a4afe9900b3eca480fc5780` |
| ZIP | `RiskManagementAI-v0.7.1-win-x64-portable.zip` |
| ZIP SHA256 | `282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018` |
| Manifest | version `0.7.1`, required entries 27/27, hash/size mismatch 0 |
| Local gate | build 0 warnings / 0 errors; `Total=907 PASS=907 FAIL=0`; `build/00~03` PASS |
| Signing | unsigned; STAB-WP-05 remains `APPROVAL_REQUIRED` |

The published ZIP, its `.sha256` sidecar, and the Release body are the hash authority. A later rebuild from the tag can have different generated timestamps and is not a substitute for the published asset.

## 2. Evidence Safety Rules

- Use only repository `samples/` dummy data or masked synthetic data.
- Do not capture real positions, customers, accounts, internal regulation/NCR originals, credentials, paths containing employee names, or model files.
- Store screenshots/logs locally under the gitignored `evidence/gateBC/v0.7.1/` tree. Never force-add or commit that tree; only a separately sanitized summary may be attached after policy review.
- `user-reported` is not formal PASS. A formal PASS row needs the evidence file named in the worksheet.
- Any SHA mismatch, unexpected file, startup failure, formula/link/macro issue, or policy rejection is `FAIL`/`BLOCKED`; stop the round and report it.

## 3. Ordered Execution

### Step 0 - Prepare The Offline Test PC

1. On the connected transfer PC, download exactly the ZIP and `.sha256` from the Release URL.
2. Transfer both files through the approved company procedure.
3. On the Test PC, disconnect the network before launching the application.
4. Create an evidence folder:

```powershell
New-Item -ItemType Directory -Force .\evidence\gateBC\v0.7.1 | Out-Null
```

### Step 1 - B0 SHA256

```powershell
$Zip = '.\RiskManagementAI-v0.7.1-win-x64-portable.zip'
$Expected = '282B71385FEE83B4ED7AD221CAF84AD3A6B4E2B5E5191601F4240AEED0419018'
$Actual = (Get-FileHash -LiteralPath $Zip -Algorithm SHA256).Hash
"Expected=$Expected`r`nActual=$Actual" | Out-File .\evidence\gateBC\v0.7.1\B0-sha256.txt
if ($Actual -ne $Expected) { throw 'B0 FAIL: published ZIP SHA256 mismatch.' }
```

### Step 2 - B1/B2 Structure And Forbidden Files

```powershell
$Extract = '.\RiskManagementAI-v0.7.1'
Expand-Archive -LiteralPath $Zip -DestinationPath $Extract -Force
$ExtractRoot = (Resolve-Path -LiteralPath $Extract).Path
$PrefixLength = $ExtractRoot.Length + 1
Get-ChildItem -LiteralPath $Extract -Recurse |
  ForEach-Object {
    [pscustomobject]@{
      RelativePath = $_.FullName.Substring($PrefixLength)
      Length = if ($_.PSIsContainer) { $null } else { $_.Length }
    }
  } |
  Out-File .\evidence\gateBC\v0.7.1\B1-tree.txt

$Forbidden = Get-ChildItem -LiteralPath $Extract -Recurse -File | Where-Object {
  $RelativePath = $_.FullName.Substring($PrefixLength)
  $_.Extension -match '^\.(gguf|bin|safetensors|onnx|pt|pem|key|pfx|p12|cer|crt|der|env)$' -or
  $RelativePath -match '(?i)(^|[\\/])(real_data|secrets|credentials|exports|internal_[^\\/]*)([\\/]|$)' -or
  $_.Name -like 'internal_*'
}
$Forbidden |
  ForEach-Object { $_.FullName.Substring($PrefixLength) } |
  Out-File .\evidence\gateBC\v0.7.1\B2-forbidden-scan.txt
if ($Forbidden) { throw 'B2 FAIL: forbidden release content detected.' }
```

Confirm `RiskManagementAI.exe`, `run.bat`, `approved_manifest.json`, and `config/`, `rules/`, `kb/`, `templates/`, `samples/`, `deploy/`, `logs/`, `reports/` exist. Confirm manifest version `0.7.1`.

### Step 3 - B3 Offline / NoModel Startup

1. Confirm the Test PC is offline.
2. Run `RiskManagementAI-v0.7.1\run.bat`.
3. Confirm startup succeeds without SDK, NuGet, model, or internet.
4. Confirm NoModel mode, external API blocked, telemetry 0, auto-update 0, and SQL/VBA auto-execution 0.
5. Save a masked startup screenshot and any hash-only startup log as `B3-*`.

### Step 4 - B4~B15 Functional Evidence

Use dummy inputs only and complete the rows in section 5 in this order:

1. **B4**: load the packaged CP949 and UTF-8 dummy CSV files. In Excel 2021, open copies of the dummy exposure/limit CSV files and Save As `.xlsx`, then load those XLSX copies. For 7-state coverage, use separate synthetic copies only: normal (<0.9), warning (0.9~1.0), breach (>1.0), unmatched exposure key (`NO_LIMIT`), non-positive or inactive limit (`INVALID_LIMIT`), missing required dummy header (`MAPPING_ERROR`), and duplicate limit join key (`DUPLICATE_LIMIT`).
2. **B5**: inspect all 9 reconciliation rows. Require every applicable check and source-to-analysis balance to PASS; when optional currency/unit columns are absent on both sides, accept `RECON_CURRENCY_MISMATCH` and `RECON_UNIT_MISMATCH` as N/A rather than PASS.
3. **B6**: compare Dashboard and generated `LIMIT_MONITORING`; capture identical counts/amounts and `DuplicateLimitCount`.
4. **B9**: open `RISK_VISUAL`; confirm 7-state distribution, TopN, HHI, Heatmap, currency warning, and exact numeric Exception Count.
5. **B10**: confirm WPF chart/heatmap rendering at normal and resized window dimensions.
6. **B11/B12**: confirm public KB citation/review notice and NCR metadata/structure only; no internal/NCR original and no official-calculation claim.
7. **B13**: confirm SQL/VBA/Excel checker, Excel Function Helper, as-you-type Smart Assist, explicit insertion only, and Esc/Close focus restoration.
8. **B14**: confirm History/Audit uses hashes only and contains no raw prompt, code, user ID, or data row.
9. **B15**: close and restart; confirm the application and saved safe UI settings recover normally.

`B7` streaming profile and `B8` Prior-Day Analytics are Core-verified but have no WPF call site in v0.7.1. Record them as `N/A (local-gate only)`, not Test-PC PASS.

### Step 5 - Gate C

1. **C1**: open the generated workbook in Excel 2021, including `RISK_VISUAL`; formula errors 0.
2. **C2**: external links 0, macros 0, formula injection 0.
3. **C3**: company antivirus/EDR scan PASS; save the sanitized result.
4. **C4**: PDB and personal build path 0; retain the B1 tree and package verification result.
5. **C4b**: in a disposable copy of the extracted folder, alter one dummy rule asset and confirm startup is blocked by Fail-Closed integrity. Never alter the clean test folder.
6. **C5**: record whether company policy accepts this unsigned ZIP. If signing is mandatory, Gate C remains `BLOCKED`; do not bypass policy.
7. **C6**: record startup, sample analysis, and report-generation elapsed time plus peak memory.
8. **C7**: record rollback steps to the approved previous ZIP and verify its SHA before use. Do not switch an active production runtime as part of this test.

## 4. Pass Rules

- Gate B can be PASS only when B0~B6 and B9~B15 have formal evidence and B7/B8 are explicitly accepted as `N/A (local-gate only)`.
- Gate C can be PASS only when C1~C4b, C6, and C7 have formal evidence and C5 is either policy-approved `ACCEPTED_RISK` or a valid signed path.
- Any missing required row keeps the corresponding Gate and overall decision `BLOCKED`.
- Team Pilot remains blocked until both Gates are formally closed.

## 5. Evidence Worksheet

| Item | Execute | Result (`PASS`/`FAIL`/`BLOCKED`/`N/A`/`ACCEPTED_RISK`) | Evidence file / value | Validator / time |
|---|---|---|---|---|
| B0 published ZIP SHA256 | [ ] | | `B0-sha256.txt` | |
| B1 required tree / manifest 0.7.1 | [ ] | | `B1-tree.txt` | |
| B2 forbidden files 0 | [ ] | | `B2-forbidden-scan.txt` | |
| B3 offline NoModel startup | [ ] | | `B3-*` | |
| B4 CP949/UTF-8/XLSX + 7 states | [ ] | | `B4-*` | |
| B5 reconciliation 9 / balance | [ ] | | applicable PASS; optional currency/unit may be N/A; `B5-*` | |
| B6 Dashboard = Report | [ ] | | `B6-*` | |
| B7 streaming profile WPF reachability | [ ] | `N/A (local-gate only)` | SmokeTest 907 | |
| B8 Prior-Day WPF reachability | [ ] | `N/A (local-gate only)` | SmokeTest 907 | |
| B9 RISK_VISUAL / Exception Count | [ ] | | `B9-*` | |
| B10 WPF chart / resize | [ ] | | `B10-*` | |
| B11 public KB citation | [ ] | | `B11-*` | |
| B12 NCR metadata only | [ ] | | `B12-*` | |
| B13 checker / Helper / Smart Assist | [ ] | | `B13-*` | |
| B14 History / hash-only Audit | [ ] | | `B14-*` | |
| B15 restart / safe settings | [ ] | | `B15-*` | |
| C1 Excel 2021 / formula errors 0 | [ ] | | `C1-*` | |
| C2 links / macros / injection 0 | [ ] | | `C2-*` | |
| C3 antivirus / EDR | [ ] | | `C3-*` | |
| C4 PDB / personal paths 0 | [ ] | | `C4-*` | |
| C4b Fail-Closed disposable-copy test | [ ] | | `C4b-*` | |
| C5 unsigned policy | [ ] | | policy record | |
| C6 performance / peak memory | [ ] | | `C6-*` | |
| C7 rollback record | [ ] | | `C7-*` | |

## 6. Operator Reply Order

Report results in six small batches so a failed item stops the round cleanly:

1. B0
2. B1~B3
3. B4~B6
4. B9~B15
5. C1~C5
6. C6~C7

For each batch, provide the result and sanitized evidence filename. Do not paste company data or internal screenshots into chat.

> Related: `docs/28` Security Review, `docs/41` Approval/Pilot Gates, `docs/48` v0.7.0 historical evidence, `docs/52` v0.7.1 release evidence, `.claude/skills/risk-gate-bc/`.
