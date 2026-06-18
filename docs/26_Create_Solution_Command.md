# Create Solution Command

Dev PC에서 필요 시 다음 명령으로 solution 파일을 생성한다.

```powershell
dotnet new sln -n RiskManagementAI
dotnet sln add src/RiskManagementAI.Core/RiskManagementAI.Core.csproj
dotnet sln add src/RiskManagementAI.App/RiskManagementAI.App.csproj
dotnet sln add tests/RiskManagementAI.SmokeTests/RiskManagementAI.SmokeTests.csproj
```

초기 Starter는 폴더 구조와 csproj를 우선 제공한다.
