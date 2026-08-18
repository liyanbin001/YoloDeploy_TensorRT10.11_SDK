using System.Collections.Concurrent;
using System.Text;

namespace YoloDeploy.SDK;

internal sealed record EngineResolveResult(
    string EnginePath,
    bool BuiltFromOnnx,
    bool CacheHit,
    bool BuiltNow,
    string BuildLog,
    GpuInfo Gpu);

internal static class EngineProvider
{
    private static readonly ConcurrentDictionary<string, object> BuildLocks =
        new(StringComparer.OrdinalIgnoreCase);

    internal static EngineResolveResult Resolve(DetectorOptionsBase options)
    {
        ValidateCommonOptions(options);

        string modelPath = Path.GetFullPath(options.ModelPath);
        string extension = Path.GetExtension(modelPath);
        GpuInfo gpu = GpuInfoProvider.Query();

        if (extension.Equals(".engine", StringComparison.OrdinalIgnoreCase))
        {
            return new EngineResolveResult(
                modelPath, false, false, false,
                "使用调用方提供的 TensorRT Engine。",
                gpu);
        }

        if (!extension.Equals(".onnx", StringComparison.OrdinalIgnoreCase))
            throw new YoloSdkException(
                $"不支持的模型格式：{extension}。SDK 支持 .onnx 或 .engine。");

        string sha256 = EngineCacheManager.ComputeSha256(modelPath);
        string precision = options.EnableFp16 ? "FP16" : "FP32";

        EngineCacheDescriptor descriptor =
            EngineCacheManager.CreateDescriptor(
                modelPath,
                sha256,
                gpu,
                precision,
                options.InputWidth,
                options.InputHeight,
                options.WorkspaceMiB);

        object buildLock = BuildLocks.GetOrAdd(
            descriptor.CacheKey,
            static _ => new object());

        lock (buildLock)
        {
            if (!options.ForceRebuildEngine &&
                EngineCacheManager.TryValidate(descriptor, out string validReason))
            {
                return new EngineResolveResult(
                    descriptor.EnginePath,
                    true,
                    true,
                    false,
                    validReason,
                    gpu);
            }

            string tempEnginePath =
                descriptor.EnginePath
                + $".building.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";

            try
            {
                var buildLog = new StringBuilder(65536);
                var error = new StringBuilder(8192);

                int code = NativeMethods.YoloBuildEngineFromOnnx(
                    modelPath,
                    tempEnginePath,
                    options.InputWidth,
                    options.InputHeight,
                    options.EnableFp16 ? 1 : 0,
                    options.WorkspaceMiB,
                    buildLog,
                    buildLog.Capacity,
                    error,
                    error.Capacity);

                if (code != 0)
                {
                    string details =
                        string.IsNullOrWhiteSpace(buildLog.ToString())
                            ? ""
                            : "\n\nTensorRT Build Log:\n" + buildLog;

                    throw new YoloSdkException(
                        "ONNX 转 TensorRT Engine 失败：" + error + details);
                }

                FileInfo tempFile = new(tempEnginePath);

                if (!tempFile.Exists || tempFile.Length <= 0)
                    throw new YoloSdkException(
                        "TensorRT Builder 返回成功，但没有生成有效 Engine 文件。");

                File.Move(tempEnginePath, descriptor.EnginePath, overwrite: true);

                EngineCacheManager.WriteMetadata(
                    descriptor,
                    buildLog.ToString());

                return new EngineResolveResult(
                    descriptor.EnginePath,
                    true,
                    false,
                    true,
                    buildLog.ToString(),
                    gpu);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempEnginePath))
                        File.Delete(tempEnginePath);
                }
                catch
                {
                }
            }
        }
    }

    internal static void ValidateCommonOptions(DetectorOptionsBase options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ModelPath))
            throw new ArgumentException("ModelPath 不能为空。", nameof(options));

        if (!File.Exists(options.ModelPath))
            throw new FileNotFoundException("模型文件不存在。", options.ModelPath);

        if (string.IsNullOrWhiteSpace(options.ClassNamesPath))
            throw new ArgumentException("ClassNamesPath 不能为空。", nameof(options));

        if (!File.Exists(options.ClassNamesPath))
            throw new FileNotFoundException(
                "类别名称文件不存在。",
                options.ClassNamesPath);

        if (options.InputWidth <= 0 || options.InputHeight <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "InputWidth/InputHeight 必须大于 0。");

        if (options.WorkspaceMiB < 64)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "WorkspaceMiB 至少应为 64。");

        if (options.MaxResults <= 0 || options.MaxResults > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(
                nameof(options),
                $"MaxResults 必须位于 1..{ushort.MaxValue}。");

        ValidateThreshold(options.ConfidenceThreshold, nameof(options.ConfidenceThreshold));
        ValidateThreshold(options.NmsThreshold, nameof(options.NmsThreshold));
        ValidateThreshold(options.MaskThreshold, nameof(options.MaskThreshold));
    }

    internal static void ValidateThreshold(float value, string name)
    {
        if (float.IsNaN(value) ||
            float.IsInfinity(value) ||
            value < 0 ||
            value > 1)
        {
            throw new ArgumentOutOfRangeException(
                name,
                "阈值必须位于 [0,1]。");
        }
    }
}
