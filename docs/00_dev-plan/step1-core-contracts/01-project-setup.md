# Phase 1: 프로젝트 초기 설정

## 목표
솔루션 구조 생성 및 기본 프로젝트 설정

## 작업 내용

### 1. 솔루션 및 프로젝트 생성

```powershell
# 루트 디렉토리 생성
mkdir AiAgentFramework
cd AiAgentFramework

# 솔루션 생성
dotnet new sln -n AiAgentFramework

# src 디렉토리 생성
mkdir src
cd src

# Core Abstractions 프로젝트 생성
dotnet new classlib -n Agent.Core.Abstractions -f net8.0
cd ..

# tests 디렉토리 생성
mkdir tests
cd tests

# 테스트 프로젝트 생성
dotnet new xunit -n Agent.Core.Abstractions.Tests -f net8.0
cd ..

# 솔루션에 프로젝트 추가
dotnet sln add src/Agent.Core.Abstractions/Agent.Core.Abstractions.csproj
dotnet sln add tests/Agent.Core.Abstractions.Tests/Agent.Core.Abstractions.Tests.csproj

# 테스트 프로젝트에 참조 추가
cd tests/Agent.Core.Abstractions.Tests
dotnet add reference ../../src/Agent.Core.Abstractions/Agent.Core.Abstractions.csproj
cd ../..
```

### 2. NuGet 패키지 설정

**Agent.Core.Abstractions.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="JsonSchema.Net" Version="5.5.0" />
  </ItemGroup>
</Project>
```

**Agent.Core.Abstractions.Tests.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="NSubstitute" Version="5.1.0" />
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
  </ItemGroup>
</Project>
```

### 3. 디렉토리 구조 생성

```powershell
# Core Abstractions 내부 구조
cd src/Agent.Core.Abstractions
mkdir Common, Orchestration, Streaming, Tools, Llm, Schema

# 테스트 프로젝트 구조
cd ../../tests/Agent.Core.Abstractions.Tests  
mkdir Common, Orchestration, Streaming, Tools, Llm, Schema

# schemas 디렉토리
cd ../..
mkdir schemas
cd schemas
mkdir core
```

### 4. 기본 설정 파일

**.editorconfig (루트):**
```ini
root = true

[*]
charset = utf-8
indent_style = space
indent_size = 4
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx}]
# C# coding conventions
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
```

**Directory.Build.props (루트):**
```xml
<Project>
  <PropertyGroup>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
</Project>
```

### 5. Git 설정

**.gitignore:**
```
## .NET
*.swp
*.*~
project.lock.json
.DS_Store
*.pyc
nupkg/

# Visual Studio Code
.vscode/

# Rider
.idea/

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/
[Oo]ut/
msbuild.log
msbuild.err
msbuild.wrn

# Visual Studio
.vs/

# Testing
TestResults/
coverage/
*.coverage
*.coveragexml
```

## 체크리스트

- [ ] 솔루션 파일 생성
- [ ] Core Abstractions 프로젝트 생성
- [ ] 테스트 프로젝트 생성
- [ ] NuGet 패키지 설치
- [ ] 디렉토리 구조 생성
- [ ] 설정 파일 추가
- [ ] 빌드 성공 확인
- [ ] 테스트 실행 확인

## 다음 단계
Phase 2: 핵심 식별자 구현