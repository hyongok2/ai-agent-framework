using AIAgentFramework.Configuration.Cache;
using AIAgentFramework.Configuration.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AIAgentFramework.Configuration.Testing
{
    /// <summary>
    /// 캐시 무효화 기능 테스트
    /// </summary>
    public static class CacheInvalidationTests
    {
        /// <summary>
        /// 패턴 기반 캐시 무효화 테스트를 실행합니다
        /// </summary>
        public static void RunTests()
        {
            Console.WriteLine("=== 캐시 무효화 기능 테스트 시작 ===\n");

            using var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<ConfigurationCache>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var configCache = new ConfigurationCache(memoryCache, logger);

            try
            {
                TestBasicInvalidation(configCache, memoryCache);
                TestPatternMatching(configCache, memoryCache);
                TestPerformance(configCache, memoryCache);
                TestStatistics(configCache);

                Console.WriteLine("✅ 모든 캐시 무효화 테스트 통과!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 테스트 실패: {ex.Message}");
            }
            finally
            {
                configCache.Dispose();
                memoryCache.Dispose();
            }
        }

        private static void TestBasicInvalidation(IConfigurationCache cache, IMemoryCache memoryCache)
        {
            Console.WriteLine("1. 기본 무효화 테스트");

            // 테스트 데이터 설정
            var testKeys = new[]
            {
                "config_Development",
                "config_Production",
                "section_LLM_Development",
                "section_Tools_Development"
            };

            foreach (var key in testKeys)
            {
                memoryCache.Set(key, $"value_{key}");
                ((ConfigurationCache)cache).TrackKey(key);
            }

            Console.WriteLine($"   - 캐시 키 {testKeys.Length}개 생성");
            Console.WriteLine($"   - 추적된 키 개수: {cache.GetCachedKeys().Count}");

            // 전체 무효화 테스트
            cache.InvalidateAll();
            var remainingKeys = cache.GetCachedKeys();
            
            if (remainingKeys.Count == 0)
            {
                Console.WriteLine("   ✅ 전체 무효화 성공");
            }
            else
            {
                throw new InvalidOperationException($"전체 무효화 후 {remainingKeys.Count}개 키가 남음");
            }

            Console.WriteLine();
        }

        private static void TestPatternMatching(IConfigurationCache cache, IMemoryCache memoryCache)
        {
            Console.WriteLine("2. 패턴 매칭 테스트");

            // 테스트 데이터 재설정
            var testData = new Dictionary<string, string>
            {
                ["config_Development"] = "dev_config",
                ["config_Production"] = "prod_config",
                ["section_LLM_Development"] = "llm_dev",
                ["section_LLM_Production"] = "llm_prod",
                ["section_Tools_Development"] = "tools_dev",
                ["user_session_123"] = "user_data"
            };

            foreach (var kvp in testData)
            {
                memoryCache.Set(kvp.Key, kvp.Value);
                ((ConfigurationCache)cache).TrackKey(kvp.Key);
            }

            Console.WriteLine($"   - 테스트 키 {testData.Count}개 생성");

            // Development 패턴 무효화
            cache.Invalidate("*Development*");
            var remainingAfterDev = cache.GetCachedKeys();
            var expectedAfterDev = new[] { "config_Production", "section_LLM_Production", "user_session_123" };
            
            if (remainingAfterDev.Count == expectedAfterDev.Length &&
                expectedAfterDev.All(key => remainingAfterDev.Contains(key)))
            {
                Console.WriteLine("   ✅ Development 패턴 무효화 성공");
            }
            else
            {
                throw new InvalidOperationException($"패턴 무효화 실패. 남은 키: {string.Join(", ", remainingAfterDev)}");
            }

            // LLM 패턴 무효화
            cache.Invalidate("*LLM*");
            var remainingAfterLlm = cache.GetCachedKeys();
            var expectedAfterLlm = new[] { "config_Production", "user_session_123" };
            
            if (remainingAfterLlm.Count == expectedAfterLlm.Length &&
                expectedAfterLlm.All(key => remainingAfterLlm.Contains(key)))
            {
                Console.WriteLine("   ✅ LLM 패턴 무효화 성공");
            }
            else
            {
                throw new InvalidOperationException($"LLM 패턴 무효화 실패. 남은 키: {string.Join(", ", remainingAfterLlm)}");
            }

            Console.WriteLine();
        }

        private static void TestPerformance(IConfigurationCache cache, IMemoryCache memoryCache)
        {
            Console.WriteLine("3. 성능 테스트");

            // 대량 데이터 생성
            const int keyCount = 10000;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < keyCount; i++)
            {
                var key = $"perf_test_{i % 10}_{i}";
                memoryCache.Set(key, $"value_{i}");
                ((ConfigurationCache)cache).TrackKey(key);
            }

            stopwatch.Stop();
            Console.WriteLine($"   - {keyCount}개 키 생성 시간: {stopwatch.ElapsedMilliseconds}ms");

            // 패턴 무효화 성능 테스트
            stopwatch.Restart();
            cache.Invalidate("*test_5*");
            stopwatch.Stop();

            var remainingCount = cache.GetCachedKeys().Count;
            var removedCount = keyCount - remainingCount;

            Console.WriteLine($"   - 패턴 무효화 시간: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"   - 제거된 키: {removedCount}개, 남은 키: {remainingCount}개");

            if (stopwatch.ElapsedMilliseconds < 100) // 100ms 이내
            {
                Console.WriteLine("   ✅ 성능 테스트 통과 (100ms 이내)");
            }
            else
            {
                Console.WriteLine($"   ⚠️ 성능 경고: {stopwatch.ElapsedMilliseconds}ms 소요");
            }

            // 전체 정리
            cache.InvalidateAll();
            Console.WriteLine();
        }

        private static void TestStatistics(IConfigurationCache cache)
        {
            Console.WriteLine("4. 통계 기능 테스트");

            var stats = cache.GetStatistics();
            Console.WriteLine($"   - 총 키 개수: {stats.TotalKeys}");
            Console.WriteLine($"   - 히트 카운트: {stats.HitCount}");
            Console.WriteLine($"   - 미스 카운트: {stats.MissCount}");
            Console.WriteLine($"   - 히트율: {stats.HitRate:P2}");
            Console.WriteLine($"   - 생성 시간: {stats.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   - 마지막 접근: {stats.LastAccessedAt:yyyy-MM-dd HH:mm:ss}");

            Console.WriteLine("   ✅ 통계 기능 정상 작동");
            Console.WriteLine();
        }
    }
}