using System.Text;

namespace AIAgentFramework.Monitoring.Metrics;

/// <summary>
/// Prometheus 형식 메트릭 내보내기
/// </summary>
public class PrometheusExporter
{
    private readonly MetricsCollector _metricsCollector;
    
    public PrometheusExporter(MetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
    }
    
    /// <summary>
    /// Prometheus 형식으로 메트릭 내보내기
    /// </summary>
    public string ExportMetrics()
    {
        var sb = new StringBuilder();
        var snapshots = _metricsCollector.GetSnapshots();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        // AI Agent Framework 메트릭 헤더
        sb.AppendLine("# HELP ai_agent_info AI Agent Framework 정보");
        sb.AppendLine("# TYPE ai_agent_info gauge");
        sb.AppendLine($"ai_agent_info{{version=\"1.0.0\",framework=\"AIAgentFramework\"}} 1 {timestamp}");
        sb.AppendLine();
        
        // 활성 오케스트레이션
        sb.AppendLine("# HELP ai_agent_active_orchestrations 현재 실행 중인 오케스트레이션 수");
        sb.AppendLine("# TYPE ai_agent_active_orchestrations gauge");
        
        var activeOrchestrations = _metricsCollector.GetActiveOrchestrationsByType();
        var totalActive = _metricsCollector.GetActiveOrchestrationsCount();
        
        sb.AppendLine($"ai_agent_active_orchestrations_total {totalActive} {timestamp}");
        
        foreach (var (type, count) in activeOrchestrations)
        {
            if (count > 0)
            {
                sb.AppendLine($"ai_agent_active_orchestrations{{type=\"{EscapePrometheusLabel(type)}\"}} {count} {timestamp}");
            }
        }
        sb.AppendLine();
        
        // 요청 메트릭
        ExportCounterMetrics(sb, snapshots, "request_", "ai_agent_requests", "처리된 요청", timestamp);
        
        // 성능 메트릭
        ExportHistogramMetrics(sb, snapshots, "request_", "ai_agent_request_duration", "요청 처리 시간 (밀리초)", timestamp);
        
        // 사용량 및 비용 메트릭 (시뮬레이션)
        sb.AppendLine("# HELP ai_agent_token_usage_total 총 토큰 사용량");
        sb.AppendLine("# TYPE ai_agent_token_usage_total counter");
        sb.AppendLine($"ai_agent_token_usage_total 0 {timestamp}");
        sb.AppendLine();
        
        sb.AppendLine("# HELP ai_agent_token_cost_total 총 토큰 비용 (USD)");
        sb.AppendLine("# TYPE ai_agent_token_cost_total counter");
        sb.AppendLine($"ai_agent_token_cost_total 0.00 {timestamp}");
        sb.AppendLine();
        
        // 오류율 메트릭
        sb.AppendLine("# HELP ai_agent_error_rate 오류율 (0-1)");
        sb.AppendLine("# TYPE ai_agent_error_rate gauge");
        
        var errorRate = CalculateErrorRate(snapshots);
        sb.AppendLine($"ai_agent_error_rate {errorRate:F4} {timestamp}");
        sb.AppendLine();
        
        // 시스템 상태 메트릭
        sb.AppendLine("# HELP ai_agent_system_status 시스템 상태 (1=정상, 0=비정상)");
        sb.AppendLine("# TYPE ai_agent_system_status gauge");
        sb.AppendLine($"ai_agent_system_status 1 {timestamp}");
        sb.AppendLine();
        
        return sb.ToString();
    }
    
    private static void ExportCounterMetrics(StringBuilder sb, IReadOnlyDictionary<string, MetricSnapshot> snapshots, string prefix, string metricName, string help, long timestamp)
    {
        sb.AppendLine($"# HELP {metricName}_total {help}");
        sb.AppendLine($"# TYPE {metricName}_total counter");
        
        var requestSnapshots = snapshots.Where(kvp => kvp.Key.StartsWith(prefix)).ToList();
        
        if (requestSnapshots.Any())
        {
            var totalCount = requestSnapshots.Sum(kvp => kvp.Value.Count);
            sb.AppendLine($"{metricName}_total {totalCount} {timestamp}");
            
            foreach (var (key, snapshot) in requestSnapshots)
            {
                var type = ExtractTypeFromKey(key, prefix);
                sb.AppendLine($"{metricName}_total{{type=\"{EscapePrometheusLabel(type)}\"}} {snapshot.Count} {timestamp}");
            }
        }
        else
        {
            sb.AppendLine($"{metricName}_total 0 {timestamp}");
        }
        
        sb.AppendLine();
    }
    
    private static void ExportHistogramMetrics(StringBuilder sb, IReadOnlyDictionary<string, MetricSnapshot> snapshots, string prefix, string metricName, string help, long timestamp)
    {
        sb.AppendLine($"# HELP {metricName} {help}");
        sb.AppendLine($"# TYPE {metricName} histogram");
        
        var histogramSnapshots = snapshots.Where(kvp => kvp.Key.StartsWith(prefix)).ToList();
        
        if (histogramSnapshots.Any())
        {
            foreach (var (key, snapshot) in histogramSnapshots)
            {
                var type = ExtractTypeFromKey(key, prefix);
                var labels = $"type=\"{EscapePrometheusLabel(type)}\"";
                
                // Histogram buckets (간단한 구현)
                var buckets = new[] { 10, 50, 100, 500, 1000, 5000, 10000, double.PositiveInfinity };
                var bucketCounts = GenerateBucketCounts(snapshot, buckets);
                
                foreach (var (bucket, count) in bucketCounts)
                {
                    var bucketLabel = bucket == double.PositiveInfinity ? "+Inf" : bucket.ToString();
                    sb.AppendLine($"{metricName}_bucket{{{labels},le=\"{bucketLabel}\"}} {count} {timestamp}");
                }
                
                sb.AppendLine($"{metricName}_sum{{{labels}}} {snapshot.Total:F2} {timestamp}");
                sb.AppendLine($"{metricName}_count{{{labels}}} {snapshot.Count} {timestamp}");
            }
        }
        else
        {
            sb.AppendLine($"{metricName}_bucket{{le=\"+Inf\"}} 0 {timestamp}");
            sb.AppendLine($"{metricName}_sum 0 {timestamp}");
            sb.AppendLine($"{metricName}_count 0 {timestamp}");
        }
        
        sb.AppendLine();
    }
    
    private static List<(double bucket, long count)> GenerateBucketCounts(MetricSnapshot snapshot, double[] buckets)
    {
        // 간단한 버킷 분포 시뮬레이션 (실제 구현에서는 히스토그램 데이터가 필요)
        var result = new List<(double bucket, long count)>();
        var cumulativeCount = 0L;
        var average = snapshot.Average;
        
        foreach (var bucket in buckets)
        {
            if (bucket == double.PositiveInfinity)
            {
                cumulativeCount = snapshot.Count;
            }
            else if (average <= bucket)
            {
                cumulativeCount = Math.Min(snapshot.Count, cumulativeCount + snapshot.Count / buckets.Length);
            }
            
            result.Add((bucket, cumulativeCount));
        }
        
        return result;
    }
    
    private static double CalculateErrorRate(IReadOnlyDictionary<string, MetricSnapshot> snapshots)
    {
        var totalRequests = snapshots.Where(kvp => kvp.Key.StartsWith("request_")).Sum(kvp => kvp.Value.Count);
        var errorRequests = snapshots.Where(kvp => kvp.Key.Contains("error")).Sum(kvp => kvp.Value.Count);
        
        return totalRequests > 0 ? (double)errorRequests / totalRequests : 0.0;
    }
    
    private static string ExtractTypeFromKey(string key, string prefix)
    {
        return key.Substring(prefix.Length);
    }
    
    private static string EscapePrometheusLabel(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
    
    /// <summary>
    /// 메트릭을 HTTP 응답으로 내보내기 (Content-Type 포함)
    /// </summary>
    public (string contentType, string content) ExportForHttp()
    {
        const string contentType = "text/plain; version=0.0.4; charset=utf-8";
        var content = ExportMetrics();
        
        return (contentType, content);
    }
}