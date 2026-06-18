# Model Pack Placeholder

모델 파일은 GitHub Repository에 포함하지 않는다.

초기 MVP는 모델 없이 실행되어야 한다.
Local LLM을 도입할 경우 별도 승인된 Model Pack으로 반입한다.

Model Pack 예시:

```text
ModelPack-v0.1
├─ models
│  └─ model.gguf
├─ inference
│  └─ local_inference_runtime.exe
├─ model_license.txt
├─ model_hash.txt
└─ README_MODEL_PACK.md
```

주의:
- 모델 라이선스 확인
- 해시 확인
- 보안검사
- 내부자료 학습 여부 확인
- 운영환경 권한통제
