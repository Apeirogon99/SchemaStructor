# SchemaStructor

SchemaStructor는 MySQL 데이터베이스 스키마를 분석하여 C# 모델 클래스, Enum을 자동으로 생성해주는 코드 제너레이터입니다.

![image](https://github.com/user-attachments/assets/45921524-5345-4be3-b9cf-58fb31e3e7e4)

# 📋 목차
- [문제 상황](#문제-상황)
- [해결 방안](#해결-방안)
- [성능 최적화](#성능-최적화)
- [사용법](#사용법)
- [결과 예시](#결과-예시)

# 문제 상황

마스터베이스의 내용을 변경하고 이를 공통 라이브러리에서 구조체를 변경하며 관리하였습니다.<br/>
이는 일일히 찾아가며 고쳐야 하는 귀찮음과 실수할 가능성이 매우 높은 일이였습니다 <br/>

실제로 마스터 데이터베이스가 변경점이 생긴다면 수작업하여 변경하면서 실수가 벌어진 적도 있었습니다.<br/>
그래서 자동화를 함으로써 실수도 줄이고 일관성을 높일 필요가 있다고 판단하여 제작하게 되었습니다<br/>

또한 GunShooterOnline프로젝트에 사용될 웹 서버, 소켓 서버의 마스터데이터 베이스를 호출하는 스크립트도 자동화 하는 것으로 목표를 새웠습니다.<br/>

# 해결 방안

### C# 클래스 및 Enum 자동 생성
MySQL 작성 시 다음 규칙을 준수합니다:
 - Name, Type, Nullable, Default, Comment 정보를 명확히 작성

스키마 정보를 JSON 형식으로 추출
- JSON 데이터를 기반으로 구조체 자동 생성
- ENUM 타입의 경우 별도 enum 클래스 생성
- DEFAULT 값을 기본값으로 설정
- COMMENT를 코드 주석으로 자동 변환


### Read only Database Context
웹 서버와 소켓 서버에서 사용할 읽기 전용 컨텍스트를 자동 생성합니다
- 마스터데이터를 메모리에 저장하여 빠른 접근 제공
- IEnumerable 인터페이스 구현으로 LINQ 사용 가능
- Find 메서드를 통한 Get 기능 제공

### 자동화화
패킷 자동화 경험을 바탕으로 Format 기반 자동화를 구현했습니다
```
var dbTable = string.Format(DbTableFormat.context,
    Program.ProjectName,
    Program.SchemaName);
File.WriteAllText($"{reposiotryFolderPath}/DbTable.cs", dbTable);
```

# 성능 최적화

### 문제 분석
- 테이블 수가 적을 때(10개): 데이터베이스 커넥션 시간이 주요 병목
- 테이블 수가 많을 때(1000개): 컬럼 검색 시간이 주요 병목
- 한번 실행할 때 마다 모든 테이블 업데이트시 발생하는 시간

### 해결책
- 멀티스레드 도입: 각 스레드가 개별 테이블을 병렬 처리
- History 관리: 최근 업데이트된 테이블만 선택적으로 처리

### 성능 개선 결과
테이블 1000개 기준
- 싱글 스레드: 5,000ms
- 멀티 스레드(4코어): 3,000ms
- 약 40% 성능 향상

### 증분 업데이트
마스터 테이블의 특성상 변화가 적다는 점을 활용
- 테이블 업데이트 시간 검색을 위한 I/O 시간은 추가되지만
- History를 통한 증분 업데이트로 전체적인 처리 시간 단축

# 사용법
### MySQL 8.0 이상 (UPDATE_TIME 얻기 위해)

### appsettings.json 파일을 프로젝트에 맞게 수정해야 합니다.
```
{
  "ApplicationSettings": {
    "ConnectionString": "Server=127.0.0.1;user=root;Password=!Q2w3e4r;Database=master_database;Pooling=true;Min Pool Size=0;Max Pool Size=40;AllowUserVariables=True;",
    "StructOutputPath": "P:\\GunShooterOnline\\GSO_WebServer\\",
    "ReposiotryOutputPath": "P:\\GunShooterOnline\\GSO_WebServer\\GSO_WebServerLibrary\\",
    "ProjectName": "WebCommonLibrary",
    "SchemaName": "MasterDatabase",
    "TableNameSeparator": "_"
  }
}
```

### 설정값 의미
|설정값       | 설명    |
|---------|---------|
ConnectionString | MySQL 데이터베이스 연결 정보<br/>
StructOutputPath | Struct 및 Enum을 저장할 폴더 경로<br/>
ProjectName | 관리될 프로젝트의 이름<br/>
SchemaName | 추출될 MySQL 스키마<br/>
TableNameSeparator | 테이블명과 스키마의 구분자<br/>

# 결과 예시

## MySQL 테이블
![image](https://github.com/user-attachments/assets/16adaba2-d7ed-4849-b951-0cea2ba77739)

## Json 작성
![image](https://github.com/user-attachments/assets/b1e8300d-50b7-4175-bdbb-044d62d88415)

## Class 및 Enum 작성
![image](https://github.com/user-attachments/assets/c6e544aa-695c-489b-bf9f-6f881c040163)

## DB Context
![image](https://github.com/user-attachments/assets/8894752f-3f54-453f-8098-0b2fa380c6e3)

![image](https://github.com/user-attachments/assets/521abcb2-e4da-458b-b01e-44b1b519f332)

![image](https://github.com/user-attachments/assets/223788ee-708b-4a5e-bc78-dceef2a325b9)


