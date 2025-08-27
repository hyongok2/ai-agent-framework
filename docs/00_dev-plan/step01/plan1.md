# Plan 1: 프로젝트 기초 설정 및 솔루션 구조

## 📋 개요

**목표**: 개발 환경과 프로젝트 구조 완성  
**예상 소요 시간**: 0.5일 (4시간)  
**품질 기준**: dev_guide.md 완전 준수

## 🎯 구체적 목표

1. ✅ **완전한 솔루션 구조** 구축
2. ✅ **개발 도구 및 품질 검증** 자동화 설정
3. ✅ **CI/CD 파이프라인** 기초 구축
4. ✅ **팀 협업 환경** 표준화

## 🏗️ 작업 단계

### **Task 1.1: 솔루션 및 프로젝트 생성** (1.5시간)

#### **디렉토리 구조 생성**
```
AIAgent/
├── AIAgent.sln                          # 메인 솔루션
├── src/
│   ├── AIAgent.Core/                     # 핵심 인터페이스 및 모델
│   │   └── AIAgent.Core.csproj
│   └── AIAgent.Common/                   # 공통 유틸리티
│       └── AIAgent.Common.csproj
├── tests/
│   ├── AIAgent.Core.Tests/               # Core 프로젝트 단위 테스트
│   │   └── AIAgent.Core.Tests.csproj
│   └── AIAgent.Common.Tests/             # Common 프로젝트 단위 테스트
│       └── AIAgent.Common.Tests.csproj
├── configs/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── prompts/                          # 프롬프트 디렉토리 (추후 사용)
├── docs/
│   ├── CLAUDE.md                         # 기존 파일
│   ├── dev_guide.md                      # 기존 파일
│   └── 00_dev-plan/                      # 개발 계획
│       └── step01/                       # Step 1 상세 계획들
└── .github/
    └── workflows/
        └── build.yml                     # CI/CD 워크플로우
```

#### **프로젝트 생성 명령어**
```bash
# 솔루션 생성
dotnet new sln -n AIAgent

# Core 프로젝트 (Class Library)
dotnet new classlib -n AIAgent.Core -o src/AIAgent.Core --framework net8.0
dotnet sln add src/AIAgent.Core

# Common 프로젝트 (Class Library)  
dotnet new classlib -n AIAgent.Common -o src/AIAgent.Common --framework net8.0
dotnet sln add src/AIAgent.Common

# 테스트 프로젝트들
dotnet new xunit -n AIAgent.Core.Tests -o tests/AIAgent.Core.Tests --framework net8.0
dotnet new xunit -n AIAgent.Common.Tests -o tests/AIAgent.Common.Tests --framework net8.0
dotnet sln add tests/AIAgent.Core.Tests
dotnet sln add tests/AIAgent.Common.Tests

# 프로젝트 참조 설정
dotnet add tests/AIAgent.Core.Tests reference src/AIAgent.Core
dotnet add tests/AIAgent.Common.Tests reference src/AIAgent.Common
dotnet add src/AIAgent.Common reference src/AIAgent.Core
```

### **Task 1.2: 프로젝트 파일 설정** (1시간)

#### **Directory.Build.props 생성**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>nullable</WarningsNotAsErrors>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <PropertyGroup>
    <Company>AI Agent Framework</Company>
    <Product>AI Agent Framework</Product>
    <Copyright>Copyright © 2024</Copyright>
    <Version>0.1.0</Version>
    <AssemblyVersion>0.1.0.0</AssemblyVersion>
    <FileVersion>0.1.0.0</FileVersion>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Debug'">
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <DefineConstants>TRACE</DefineConstants>
    <Optimize>true</Optimize>
  </PropertyGroup>
</Project>
```

#### **Directory.Build.targets 생성**
```xml
<Project>
  <ItemGroup>
    <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.507">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.15.0.81779">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup Condition="'$(IsTestProject)' == 'true'">
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### **Task 1.3: 개발 도구 설정** (1시간)

#### **.editorconfig 생성**
```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
trim_trailing_whitespace = true
insert_final_newline = true
indent_style = space
indent_size = 4

[*.{cs,csx,vb,vbx}]
indent_size = 4
insert_final_newline = true

[*.{json,js,jsx,ts,tsx}]
indent_size = 2

[*.{yml,yaml}]
indent_size = 2

# .NET formatting rules
[*.{cs,vb}]
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# C# formatting rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_new_line_between_query_expression_clauses = true

# Indentation preferences
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_indent_labels = flush_left

# Space preferences
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_space_before_colon_in_inheritance_clause = true
csharp_space_after_colon_in_inheritance_clause = true
csharp_space_around_binary_operators = before_and_after
csharp_space_between_method_declaration_empty_parameter_list_parentheses = false
csharp_space_between_method_call_name_and_opening_parenthesis = false
csharp_space_between_method_call_empty_parameter_list_parentheses = false
```

#### **stylecop.json 생성**
```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "AI Agent Framework",
      "copyrightText": "Copyright (c) {companyName}. All rights reserved.\nLicensed under the MIT license. See LICENSE file in the project root for full license information.",
      "xmlHeader": false,
      "fileNamingConvention": "stylecop"
    },
    "orderingRules": {
      "usingDirectivesPlacement": "outsideNamespace"
    }
  }
}
```

### **Task 1.4: CI/CD 파이프라인 설정** (0.5시간)

#### **.github/workflows/build.yml 생성**
```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

env:
  DOTNET_VERSION: '8.0.x'
  SOLUTION_FILE: 'AIAgent.sln'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Cache NuGet packages
      uses: actions/cache@v3
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
        restore-keys: |
          ${{ runner.os }}-nuget-
          
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}
      
    - name: Build solution
      run: dotnet build ${{ env.SOLUTION_FILE }} --configuration Release --no-restore
      
    - name: Run tests
      run: dotnet test ${{ env.SOLUTION_FILE }} --configuration Release --no-build --verbosity normal --collect:"XPlat Code Coverage" --logger trx --results-directory ./TestResults
      
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: ./TestResults
        
  code-quality:
    runs-on: ubuntu-latest
    needs: build-and-test
    
    steps:
    - name: Checkout code
      uses: actions/checkout@v4
      
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
        
    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}
      
    - name: Run StyleCop Analysis
      run: dotnet build ${{ env.SOLUTION_FILE }} --configuration Release --verbosity normal
```

## 🔍 검증 기준

### **필수 통과 조건**

#### **1. 빌드 성공**
```bash
# 로컬 환경에서 다음 명령어들이 모두 성공해야 함
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

#### **2. 코드 품질 검증**
- [ ] StyleCop 규칙 위반 0건
- [ ] SonarAnalyzer 경고 0건  
- [ ] 컴파일러 경고 0건
- [ ] Nullable Reference Types 경고 0건

#### **3. 프로젝트 구조 검증**
- [ ] 모든 폴더가 의도된 구조대로 생성
- [ ] 프로젝트 간 참조 관계 정확
- [ ] 네임스페이스 명명 규칙 준수
- [ ] 파일명과 클래스명 일치

#### **4. CI/CD 파이프라인 검증**
- [ ] GitHub Actions 워크플로우 정상 실행
- [ ] 빌드 및 테스트 단계 모두 성공
- [ ] Artifact 업로드 성공

### **성능 기준**
- **빌드 시간**: 로컬에서 30초 이내
- **테스트 실행 시간**: 5초 이내
- **CI/CD 파이프라인**: 5분 이내 완료

## 📝 완료 체크리스트

### **기본 설정**
- [ ] 솔루션 파일 생성 완료 (`AIAgent.sln`)
- [ ] Core 프로젝트 생성 완료 (`AIAgent.Core`)
- [ ] Common 프로젝트 생성 완료 (`AIAgent.Common`)
- [ ] 테스트 프로젝트들 생성 완료
- [ ] 프로젝트 참조 관계 설정 완료

### **개발 도구**
- [ ] `Directory.Build.props` 설정 완료
- [ ] `Directory.Build.targets` 설정 완료
- [ ] `.editorconfig` 설정 완료
- [ ] `stylecop.json` 설정 완료
- [ ] `.gitignore` 업데이트 완료

### **코드 품질**
- [ ] StyleCop Analyzers 적용 완료
- [ ] SonarAnalyzer 적용 완료
- [ ] Nullable Reference Types 활성화
- [ ] TreatWarningsAsErrors 설정

### **CI/CD**
- [ ] GitHub Actions 워크플로우 생성
- [ ] 빌드 파이프라인 테스트 성공
- [ ] 테스트 실행 파이프라인 성공
- [ ] Artifact 업로드 확인

## 🎯 성공 지표

완료 시 다음이 모두 달성되어야 함:

1. ✅ **완전히 동작하는 솔루션**: 에러 없이 빌드/테스트 실행
2. ✅ **자동화된 품질 검증**: CI/CD에서 코드 품질 자동 검사
3. ✅ **표준화된 개발 환경**: 팀 전체가 동일한 환경에서 개발 가능
4. ✅ **확장 가능한 구조**: 다음 단계 작업을 위한 견고한 기반

---

**다음 계획**: [Plan 2: 핵심 인터페이스 및 모델 정의](plan2.md)