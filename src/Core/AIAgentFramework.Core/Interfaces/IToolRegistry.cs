using System;
using System.Collections.Generic;

namespace AIAgentFramework.Core.Interfaces
{
    /// <summary>
    /// 도구를 관리하는 타입 안전한 레지스트리 인터페이스
    /// </summary>
    public interface IToolRegistry
    {
        /// <summary>
        /// 타입으로 도구를 등록합니다 (DI에서 인스턴스 생성)
        /// </summary>
        void Register<T>() where T : class, ITool;

        /// <summary>
        /// 인스턴스로 도구를 등록합니다
        /// </summary>
        void Register<T>(T instance) where T : class, ITool;

        /// <summary>
        /// 이름으로 도구를 등록합니다
        /// </summary>
        void Register(string name, ITool tool);

        /// <summary>
        /// 타입으로 도구를 해결합니다
        /// </summary>
        T Resolve<T>() where T : class, ITool;

        /// <summary>
        /// 이름으로 도구를 해결합니다
        /// </summary>
        ITool Resolve(string name);

        /// <summary>
        /// 도구가 등록되어 있는지 확인합니다
        /// </summary>
        bool IsRegistered(string name);

        /// <summary>
        /// 타입으로 도구가 등록되어 있는지 확인합니다
        /// </summary>
        bool IsRegistered<T>() where T : class, ITool;

        /// <summary>
        /// 모든 등록된 도구를 가져옵니다
        /// </summary>
        IEnumerable<ITool> GetAll();

        /// <summary>
        /// 모든 등록된 도구 이름을 가져옵니다
        /// </summary>
        IEnumerable<string> GetAllNames();

        /// <summary>
        /// 카테고리별로 도구를 가져옵니다
        /// </summary>
        IEnumerable<ITool> GetByCategory(string category);

        /// <summary>
        /// 레지스트리를 초기화합니다
        /// </summary>
        void Clear();
    }
}