# task_20260309

ASP.NET Core 8.0 기반 직원 관리 Web API입니다.

## 주요 기능

- **직원 목록 조회** (페이징) - `GET /api/employee`
- **직원 상세 조회** (이름) - `GET /api/employee/{name}`
- **직원 일괄 추가** - `POST /api/employee`
  - JSON 배열 또는 CSV/JSON 파일 업로드 지원

## 기술 스택

- .NET 8.0, ASP.NET Core Web API
- Entity Framework Core
- FluentValidation, CsvHelper, Serilog, Swagger

## 실행 방법

```bash
dotnet run
```

실행 후:
- **Swagger UI**: `https://localhost:{port}/swagger`
- **샘플 페이지**: `https://localhost:{port}/` (루트)

### 샘플 페이지 (wwwroot/index.html)

직원 디렉터리 데모 UI. API 호출 예시 확인용.

## 설정

### Persistence (기본값)

| 설정 | 기본값 | 설명 |
|------|--------|------|
| `Database:Provider` | `Sqlite` | DB 프로바이더 |
| `Database:ConnectionString` | `Data Source=app.db` | 연결 문자열 |
| `ConnectionStrings:DefaultConnection` | `Data Source=app.db` | 기본 연결 문자열 |

`appsettings.json`에서 위 값을 수정하여 PostgreSQL(Npgsql) 또는 SQL Server로 변경할 수 있습니다.

---

## 확장성

| 영역 | 방식 | 설명 |
|------|------|------|
| **포맷 추가** | `IEmployeeParser` 구현 + DI 등록 | XML, Excel 등 새 포맷은 파서를 추가 구현 |
| **RDB 교체** | `appsettings` | `Database:Provider`와 `ConnectionString` 변경으로 전환 |

---

## 고려 사항

### GET /api/employee (직원 목록 조회)

### GET /api/employee/{name} (이름으로 직원 조회)

| 고려 항목 | 설계 |
|-----------|------|
| **매칭** | 이름 **정확 일치**만. 부분 검색/유사 검색 없음 |
| **동명이인** | `GetAllByNameAsync`로 동명이인 전체 반환 |
| **미존재** | 0건이면 **404 NotFound** 반환 |

### POST /api/employee (직원 일괄 추가)

| 고려 항목 | 설계 |
|-----------|------|
| **입력 방식** | multipart/form-data(`file`, `rawData`) 또는 body 직접(text/plain, text/csv, application/json) |
| **form 필드명** | `file`(파일), `rawData`(텍스트). 둘 다 입력 시 병합 후 이메일 기준 중복 제거 |
| **파싱** | 확장자/내용으로 CSV·JSON 파서 자동 선택. 실패 시 400 |
| **검증** | FluentValidation. 이메일 형식, 필수값 등. 실패 시 전체 거부(400) |
| **이메일 중복** | 파싱 단계에서 `MergeByKey`로 먼저 나온 것 우선. DB 저장 시 `ExistsByEmailAsync`로 재확인 |
| **실패 처리** | 저장 실패 시 로깅 |

---

## 테스트

```bash
dotnet test
```

### 주요 테스트 케이스

| 영역 | 테스트 | 검증 내용 |
|------|--------|-----------|
| **API** | `Get_employee_page1_페이징된_JSON_응답` | 목록 조회 200, items/totalCount/page/pageSize 반환 |
| **API** | `Post_employee_유효한_데이터_시_201_Created` | rawData(CSV)로 직원 추가 시 201 |
| **API** | `Post_employee_ContentType_없으면_400` | 허용되지 않은 Content-Type 시 400 |
| **API** | `Get_employee_페이징_파라미터_기본값_적용` | page/pageSize 미지정 시 기본값 1, 10 |
| **Validator** | `Validate_이메일_형식_오류_시_실패` | 잘못된 이메일 형식 검출 |
| **Validator** | `Validate_이미_등록된_이메일_시_실패` | DB 기반 이메일 중복 검증 |
| **Validator** | `Validate_유효한_데이터_시_성공` | 정상 데이터 통과 |
| **Repository** | `Add_및_SaveChanges_데이터_저장_성공` | 저장 후 Id 부여, 조회 검증 |
| **Repository** | `동일_이메일_저장_시_유니크_인덱스_위반_예외` | Email UNIQUE 제약 동작 |
| **Repository** | `GetPagedAsync_페이징_정상` | 페이징 결과 및 totalCount |
| **Parser** | `ParseFromStringAsync_헤더_있는_CSV_파싱` | CSV → DTO 변환 |
| **Parser** | `ParseFromStringAsync_유효한_JSON_배열_파싱` | JSON → DTO 변환 |
| **MergeByKey** | `MergeByKey_중복_이메일_시_먼저_나온_것_우선` | 이메일 기준 중복 제거, 먼저 나온 것 우선 |