using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AIAgentFramework.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AIAgentFramework.Core.Registry
{
    /// <summary>
    /// 타입 안전한 LLM 함수 레지스트리 구현
    /// </summary>
    public class TypedLLMFunctionRegistry : ILLMFunctionRegistry
    {
        private readonly ConcurrentDictionary<string, ILLMFunction> _functionsByName;
        private readonly ConcurrentDictionary<Type, ILLMFunction> _functionsByType;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TypedLLMFunctionRegistry> _logger;

        public TypedLLMFunctionRegistry(
            IServiceProvider serviceProvider,
            ILogger<TypedLLMFunctionRegistry> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _functionsByName = new ConcurrentDictionary<string, ILLMFunction>(StringComparer.OrdinalIgnoreCase);
            _functionsByType = new ConcurrentDictionary<Type, ILLMFunction>();
        }

        /// <inheritdoc />
        public void Register<T>() where T : class, ILLMFunction
        {
            var function = _serviceProvider.GetService<T>() 
                ?? ActivatorUtilities.CreateInstance<T>(_serviceProvider);
            
            Register(function);
        }

        /// <inheritdoc />
        public void Register<T>(T instance) where T : class, ILLMFunction
        {
            ArgumentNullException.ThrowIfNull(instance);
            
            var type = typeof(T);
            var name = instance.Name;

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"Function {type.Name} must have a valid name");
            }

            if (_functionsByName.TryAdd(name, instance))
            {
                _functionsByType.TryAdd(type, instance);
                _logger.LogInformation("Registered LLM function: {FunctionName} (Type: {FunctionType})", 
                    name, type.Name);
            }
            else
            {
                throw new InvalidOperationException($"Function with name '{name}' is already registered");
            }
        }

        /// <inheritdoc />
        public void Register(string name, ILLMFunction function)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(function);

            if (_functionsByName.TryAdd(name, function))
            {
                _functionsByType.TryAdd(function.GetType(), function);
                _logger.LogInformation("Registered LLM function: {FunctionName}", name);
            }
            else
            {
                throw new InvalidOperationException($"Function with name '{name}' is already registered");
            }
        }

        /// <inheritdoc />
        public T Resolve<T>() where T : class, ILLMFunction
        {
            var type = typeof(T);
            
            if (_functionsByType.TryGetValue(type, out var function) && function is T typedFunction)
            {
                return typedFunction;
            }

            // 인터페이스나 기본 클래스로 검색
            var matchingFunction = _functionsByType.Values
                .OfType<T>()
                .FirstOrDefault();

            if (matchingFunction != null)
            {
                return matchingFunction;
            }

            throw new InvalidOperationException($"Function of type '{type.Name}' is not registered");
        }

        /// <inheritdoc />
        public ILLMFunction Resolve(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (_functionsByName.TryGetValue(name, out var function))
            {
                return function;
            }

            throw new InvalidOperationException($"Function with name '{name}' is not registered");
        }

        /// <inheritdoc />
        public bool IsRegistered(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return _functionsByName.ContainsKey(name);
        }

        /// <inheritdoc />
        public bool IsRegistered<T>() where T : class, ILLMFunction
        {
            var type = typeof(T);
            return _functionsByType.ContainsKey(type) || 
                   _functionsByType.Values.Any(f => f is T);
        }

        /// <inheritdoc />
        public IEnumerable<ILLMFunction> GetAll()
        {
            return _functionsByName.Values.ToList();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllNames()
        {
            return _functionsByName.Keys.ToList();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _functionsByName.Clear();
            _functionsByType.Clear();
            _logger.LogInformation("LLM function registry cleared");
        }
    }
}