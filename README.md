# SchemaStructor

SchemaStructor는 MySQL 데이터베이스 스키마를 분석하여 C# 모델 클래스, Enum을 자동으로 생성해주는 코드 제너레이터입니다.
![image](https://github.com/user-attachments/assets/45921524-5345-4be3-b9cf-58fb31e3e7e4)

# 문제

마스터베이스의 내용을 변경하고 이를 공통 라이브러리에서 구조체를 변경하며 관리하였습니다.<br/>
이는 일일히 찾아가며 고쳐야 하는 귀찮음과 실수할 가능성이 매우 높은 일이였습니다 <br/>

실제로 마스터 데이터베이스가 변경점이 생긴다면 수작업하여 변경하면서 실수가 벌어진 적도 있었습니다.<br/>
그래서 자동화를 함으로써 실수도 줄이고 일관성을 높일 필요가 있다고 판단하여 제작하게 되었습니다<br/>

또한 GunShooterOnline프로젝트에 사용될 웹 서버, 소켓 서버의 마스터데이터 베이스를 호출하는 스크립트도 자동화 하는 것으로 목표를 새웠습니다.<br/>

# 해결

## C#클래스 Enum으로 변경
MySQL의 스키마를 검색하여 Json으로 변경하기 위해<br/>

MySQL에서 작성할 때 규칙을 세워 작성합니다<br/>
작성 규칙은 검색 하는 Name, Type, Nullable, Default, Comment를 작성하는게 규칙입니다.<br/>

이를 JSON형식으로 추출합니다<br/>
JSON을 바탕으로 구조체를 생성합니다 ENUM이라면 enum클래스도 생성하여 구조체를 저장하고<br/>
아까 받은 DEFAULT값을 기본값으로 설정하고 COMMENT는 주석으로 자동화 처리 하였습니다<br/>

## Read only Database Context
웹 서버와 소켓 서버에서 마스터데이터 베이스의 값을 메모리에 저장하고 불러올 수 있는 읽기 전용 컨텍스트를 작성합니다<br/>
IEnumerable을 사용하여 LINQ도 사용이 가능하다 Get과 같은 기능은 Find를 통해 만들어주었습니다.<br/>

## 자동화
자동화는 패킷을 자동화 했던 기억을 살려 Format을 작성하고 필요한 데이터를 json에서 뽑아와 사용하였습니다<br/>
```
var dbTable = string.Format(DbTableFormat.context,
    Program.ProjectName,
    Program.SchemaName);
File.WriteAllText($"{reposiotryFolderPath}/DbTable.cs", dbTable);
```

# 성능 최적화

테이블의 양이 적었을 때(10개)는 데이터베이스와의 커넥션 시간이 더 크게 작용했습니다<br/>
하지만 테이블의 양이 많다고 가정하였을 때 (1000개) 테이블의 컬럼을 검색하는 시간이 늘어 났습니다.<br/>

이를 해결하기 위해 멀티쓰레드를 사용하여 각각의 스레드가 테이블을 하나씩 처리해 나갈 수 있도록 하였습니다<br/>
또한 History를 만들어 최근 테이블의 업데이트가 최근에 이루어져 있는 테이블만 적용할 수 있도록 수정하였습니다<br/>

결과적으로 테이블 1000개라고 가정 하였을 때<br/>
싱글 스레드 기준 5000ms<br/>
멀티 스레드(4) 기준 3000ms<br/>
대략 40%의 이득을 볼 수 있었습니다.<br/>

또한 마스터 테이블 특성상 많은 변화는 없기 때문에 테이블의 업데이트를 검색하는 I/O시간은 추가되었지만<br/>
History를 통해 처음부터 다시 업데이트 하는 것보다 높은 효과를 얻을 수 있었습니다.<br/>

# 사용법

MySQL 8.0 이상 (UPDATE_TIME 얻기 위해)<br/>

appsettings.json 파일을 프로젝트에 맞게 수정해야 합니다.<br/>
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

각 설정값의 의미를 설명하면<br/>
ConnectionString은 MySQL 데이터베이스 연결 정보<br/>
StructOutputPath은 Struct 및 Enum을 저장할 폴더 경로<br/>
ProjectName은 관리될 프로젝트의 이름<br/>
SchemaName은 추출될 MySQL 스키마<br/>
TableNameSeparator는 테이블명을 클래스명으로 변환할 때 사용할 구분자<br/>

# 결과

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


