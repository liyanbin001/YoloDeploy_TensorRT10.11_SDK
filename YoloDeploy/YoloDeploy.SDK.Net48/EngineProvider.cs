using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace YoloDeploy.SDK
{
    internal sealed class EngineResolveResult
    {
        internal EngineResolveResult(
            string enginePath,
            bool builtFromOnnx,
            bool cacheHit,
            bool builtNow,
            string buildLog,
            GpuInfo gpu)
        {
            EnginePath = enginePath;
            BuiltFromOnnx = builtFromOnnx;
            CacheHit = cacheHit;
            BuiltNow = builtNow;
            BuildLog = buildLog;
            Gpu = gpu;
        }

        internal string EnginePath { get; private set; }
        internal bool BuiltFromOnnx { get; private set; }
        internal bool CacheHit { get; private set; }
        internal bool BuiltNow { get; private set; }
        internal string BuildLog { get; private set; }
        internal GpuInfo Gpu { get; private set; }
    }

    internal static class EngineProvider
    {
        private static readonly ConcurrentDictionary<string, object>
            BuildLocks =
                new ConcurrentDictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase);

        internal static EngineResolveResult Resolve(
            DetectorOptionsBase options)
        {
            ValidateCommonOptions(options);

            string modelPath =
                Path.GetFullPath(
                    options.ModelPath);

            string extension =
                Path.GetExtension(
                    modelPath);

            GpuInfo gpu =
                GpuInfoProvider.Query();

            if (extension.Equals(
                ".engine",
                StringComparison.OrdinalIgnoreCase))
            {
                return new EngineResolveResult(
                    modelPath,
                    false,
                    false,
                    false,
                    "使用调用方提供的 TensorRT Engine。",
                    gpu);
            }

            if (!extension.Equals(
                ".onnx",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new YoloSdkException(
                    "不支持的模型格式："
                    + extension
                    + "。SDK 仅支持 .onnx 或 .engine。");
            }

            string sha256 =
                EngineCacheManager.ComputeSha256(
                    modelPath);

            string precision =
                options.EnableFp16
                    ? "FP16"
                    : "FP32";

            EngineCacheDescriptor descriptor =
                EngineCacheManager.CreateDescriptor(
                    modelPath,
                    sha256,
                    gpu,
                    precision,
                    options.InputWidth,
                    options.InputHeight,
                    options.WorkspaceMiB);

            object buildLock =
                BuildLocks.GetOrAdd(
                    descriptor.CacheKey,
                    delegate(string key)
                    {
                        return new object();
                    });

            lock (buildLock)
            {
                string validReason;

                if (!options.ForceRebuildEngine &&
                    EngineCacheManager.TryValidate(
                        descriptor,
                        out validReason))
                {
                    return new EngineResolveResult(
                        descriptor.EnginePath,
                        true,
                        true,
                        false,
                        validReason,
                        gpu);
                }

                int processId =
                    Process
                        .GetCurrentProcess()
                        .Id;

                string tempEnginePath =
                    descriptor.EnginePath
                    + ".building."
                    + processId
                    + "."
                    + Guid.NewGuid().ToString("N")
                    + ".tmp";

                try
                {
                    StringBuilder buildLog =
                        new StringBuilder(65536);

                    StringBuilder error =
                        new StringBuilder(8192);

                    int code =
                        NativeMethods.YoloBuildEngineFromOnnx(
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
                            string.IsNullOrWhiteSpace(
                                buildLog.ToString())
                                ? string.Empty
                                : "\r\n\r\nTensorRT Build Log:\r\n"
                                  + buildLog;

                        throw new YoloSdkException(
                            "ONNX 转 TensorRT Engine 失败："
                            + error
                            + details);
                    }

                    FileInfo tempFile =
                        new FileInfo(
                            tempEnginePath);

                    if (!tempFile.Exists ||
                        tempFile.Length <= 0)
                    {
                        throw new YoloSdkException(
                            "TensorRT Builder 返回成功，但没有生成有效 Engine 文件。");
                    }

                    if (File.Exists(
                            descriptor.EnginePath))
                    {
                        File.Delete(
                            descriptor.EnginePath);
                    }

                    File.Move(
                        tempEnginePath,
                        descriptor.EnginePath);

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
                        if (File.Exists(
                                tempEnginePath))
                        {
                            File.Delete(
                                tempEnginePath);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        internal static void ValidateCommonOptions(
            DetectorOptionsBase options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            if (string.IsNullOrWhiteSpace(
                    options.ModelPath))
            {
                throw new ArgumentException(
                    "ModelPath 不能为空。",
                    "options");
            }

            if (!File.Exists(
                    options.ModelPath))
            {
                throw new FileNotFoundException(
                    "模型文件不存在。",
                    options.ModelPath);
            }

            if (string.IsNullOrWhiteSpace(
                    options.ClassNamesPath))
            {
                throw new ArgumentException(
                    "ClassNamesPath 不能为空。",
                    "options");
            }

            if (!File.Exists(
                    options.ClassNamesPath))
            {
                throw new FileNotFoundException(
                    "类别名称文件不存在。",
                    options.ClassNamesPath);
            }

            if (options.InputWidth <= 0 ||
                options.InputHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "options",
                    "InputWidth/InputHeight 必须大于 0。");
            }

            if (options.WorkspaceMiB < 64)
            {
                throw new ArgumentOutOfRangeException(
                    "options",
                    "WorkspaceMiB 至少应为 64。");
            }

            if (options.MaxResults <= 0 ||
                options.MaxResults > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    "options",
                    "MaxResults 必须位于 1.."
                    + ushort.MaxValue
                    + "。");
            }

            ValidateThreshold(
                options.ConfidenceThreshold,
                "ConfidenceThreshold");

            ValidateThreshold(
                options.NmsThreshold,
                "NmsThreshold");

            ValidateThreshold(
                options.MaskThreshold,
                "MaskThreshold");
        }

        internal static void ValidateThreshold(
            float value,
            string name)
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
}
