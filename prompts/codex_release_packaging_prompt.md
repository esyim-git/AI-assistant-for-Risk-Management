목표: 운영환경 반입용 portable release ZIP 생성 절차를 검증하고 보강한다.

요구사항:
- Windows 11 x64 대상
- self-contained publish
- 운영환경에 .NET SDK 불필요
- 운영환경에 인터넷 불필요
- ZIP + SHA256 생성
- Release Note 생성
- Dependency List 생성
- 모델 파일은 포함하지 않음

작업:
1. build/01_publish-win-x64.ps1 점검
2. build/02_package-release.ps1 점검
3. build/03_verify-package.ps1 점검
4. README.md와 docs/24_Release_Packaging_Guide.md의 절차 일치 여부 확인
5. 필요 시 스크립트를 보강하라.

금지:
- source ZIP을 운영 반입 대상으로 설명하지 말 것
- force push 금지
- 외부 다운로드 금지
