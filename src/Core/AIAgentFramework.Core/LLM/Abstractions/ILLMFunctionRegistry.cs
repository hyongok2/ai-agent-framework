using System;
using System.Collections.Generic;

namespace AIAgentFramework.Core.LLM.Abstractions
{
    /// <summary>
    /// LLM 함수를 관리하는 타입 안전한 레지스트리 인터페이스
    /// </summary>
    public interface ILLMFunctionRegistry
    {
        /// <summary>
        /// 타입으로 LLM 함수를 등록합니다 (DI에서 인스턴스 생성)
        /// </summary>
        void Register<T>() where T : class, ILLMFunction;

        /// <summary>
        /// 인스턴스로 LLM 함수를 등록합니다
        /// </summary>
        void Register<T>(T instance) where T : class, ILLMFunction;

        /// <summary>
        /// 이름으로 LLM 함수를 등록합니다
        /// </summary>
        void Register(string name, ILLMFunction function);

        /// <summary>
        /// 타입으로 LLM 함수를 해결합니다
        /// </summary>
        T Resolve<T>() where T : class, ILLMFunction;

        /// <summary>
        /// 이름으로 LLM 함수를 해결합니다
        /// </summary>
        ILLMFunction Resolve(string name);

        /// <summary>
        /// 함수가 등록되어 있는지 확인합니다
        /// </summary>
        bool IsRegistered(string name);

        /// <summary>
        /// 타입으로 함수가 등록되어 있는지 확인합니다
        /// </summary>
        bool IsRegistered<T>() where T : class, ILLMFunction;

        /// <summary>
        /// 모든 등록된 LLM 함수를 가져옵니다
        /// </summary>
        IEnumerable<ILLMFunction> GetAll();

        /// <summary>
        /// 모든 등록된 함수 이름을 가져옵니다
        /// </summary>
        IEnumerable<string> GetAllNames();

        /// <summary>
        /// 레지스트리를 초기화합니다
        /// </summary>
        void Clear();
    }
}