using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace YoloDeploy.SDK
{
    internal sealed class EngineCacheDescriptor
    {
        public string OnnxPath { get; set; }
        public string OnnxSha256 { get; set; }
        public string Precision { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }
        public int WorkspaceMiB { get; set; }
        public GpuInfo Gpu { get; set; }
        public string CacheKey { get; set; }
        public string EnginePath { get; set; }
        public string MetadataPath { get; set; }
    }

    internal sealed class EngineCacheMetadata
    {
        public int SchemaVersion { get; set; }
        public string SourceOnnxPath { get; set; }
        public string OnnxSha256 { get; set; }
        public string Precision { get; set; }
        public int InputWidth { get; set; }
        public int InputHeight { get; set; }
        public int WorkspaceMiB { get; set; }

        public string GpuName { get; set; }
        public int ComputeCapabilityMajor { get; set; }
        public int ComputeCapabilityMinor { get; set; }
        public int MultiProcessorCount { get; set; }
        public ulong TotalGlobalMemoryBytes { get; set; }

        public int CudaRuntimeVersion { get; set; }
        public int CudaDriverVersion { get; set; }
        public int TensorRtMajor { get; set; }
        public int TensorRtMinor { get; set; }
        public int TensorRtPatch { get; set; }
        public int TensorRtBuild { get; set; }

        public long EngineLengthBytes { get; set; }

        // ISO-8601 string keeps cache metadata portable across managed SDK versions.
        public string CreatedUtc { get; set; }

        public string BuildLog { get; set; }

        public EngineCacheMetadata()
        {
            SchemaVersion = 2;
            SourceOnnxPath = string.Empty;
            OnnxSha256 = string.Empty;
            Precision = string.Empty;
            GpuName = string.Empty;
            CreatedUtc = string.Empty;
            BuildLog = string.Empty;
        }
    }

    internal static class EngineCacheManager
    {
        private const int CurrentSchemaVersion = 2;

        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer();

        internal static readonly string CacheRoot =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "YoloDeploy",
                "EngineCache");

        internal static string ComputeSha256(
            string filePath)
        {
            using (FileStream stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash =
                    sha256.ComputeHash(stream);

                StringBuilder sb =
                    new StringBuilder(hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(
                        hash[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        internal static EngineCacheDescriptor CreateDescriptor(
            string onnxPath,
            string onnxSha256,
            GpuInfo gpu,
            string precision,
            int inputWidth,
            int inputHeight,
            int workspaceMiB)
        {
            Directory.CreateDirectory(CacheRoot);

            string stem =
                SanitizeToken(
                    Path.GetFileNameWithoutExtension(
                        onnxPath));

            string gpuToken =
                SanitizeToken(gpu.Name);

            int hashLength =
                Math.Min(16, onnxSha256.Length);

            string hashToken =
                onnxSha256.Substring(
                    0,
                    hashLength);

            string precisionToken =
                SanitizeToken(
                    precision.ToLowerInvariant());

            string cacheKey =
                stem + "_"
                + hashToken + "_"
                + gpuToken + "_"
                + "cc"
                + gpu.ComputeCapabilityMajor
                + gpu.ComputeCapabilityMinor
                + "_sm"
                + gpu.MultiProcessorCount
                + "_trt"
                + gpu.TensorRtMajor + "_"
                + gpu.TensorRtMinor + "_"
                + gpu.TensorRtPatch + "_"
                + gpu.TensorRtBuild
                + "_"
                + precisionToken
                + "_"
                + inputWidth + "x" + inputHeight
                + "_ws" + workspaceMiB;

            string enginePath =
                Path.Combine(
                    CacheRoot,
                    cacheKey + ".engine");

            return new EngineCacheDescriptor
            {
                OnnxPath =
                    Path.GetFullPath(onnxPath),

                OnnxSha256 =
                    onnxSha256,

                Precision =
                    precision.ToUpperInvariant(),

                InputWidth =
                    inputWidth,

                InputHeight =
                    inputHeight,

                WorkspaceMiB =
                    workspaceMiB,

                Gpu =
                    gpu,

                CacheKey =
                    cacheKey,

                EnginePath =
                    enginePath,

                MetadataPath =
                    enginePath + ".json"
            };
        }

        internal static bool TryValidate(
            EngineCacheDescriptor descriptor,
            out string reason)
        {
            reason = string.Empty;

            if (!File.Exists(descriptor.EnginePath))
            {
                reason = "Engine 文件不存在";
                return false;
            }

            if (!File.Exists(descriptor.MetadataPath))
            {
                reason = "缓存元数据不存在";
                return false;
            }

            try
            {
                string json =
                    File.ReadAllText(
                        descriptor.MetadataPath);

                EngineCacheMetadata metadata =
                    Serializer.Deserialize<EngineCacheMetadata>(
                        json);

                if (metadata == null)
                {
                    reason = "缓存元数据无法解析";
                    return false;
                }

                if (metadata.SchemaVersion != CurrentSchemaVersion)
                {
                    reason = "缓存格式版本已变化";
                    return false;
                }

                if (!string.Equals(
                    metadata.OnnxSha256,
                    descriptor.OnnxSha256,
                    StringComparison.OrdinalIgnoreCase))
                {
                    reason = "ONNX 内容已变化";
                    return false;
                }

                if (!string.Equals(
                    metadata.Precision,
                    descriptor.Precision,
                    StringComparison.OrdinalIgnoreCase))
                {
                    reason = "精度配置不同";
                    return false;
                }

                if (metadata.InputWidth != descriptor.InputWidth ||
                    metadata.InputHeight != descriptor.InputHeight)
                {
                    reason = "输入尺寸不同";
                    return false;
                }

                if (metadata.WorkspaceMiB != descriptor.WorkspaceMiB)
                {
                    reason = "Workspace 配置不同";
                    return false;
                }

                GpuInfo gpu = descriptor.Gpu;

                if (!string.Equals(
                        metadata.GpuName,
                        gpu.Name,
                        StringComparison.OrdinalIgnoreCase) ||
                    metadata.ComputeCapabilityMajor !=
                        gpu.ComputeCapabilityMajor ||
                    metadata.ComputeCapabilityMinor !=
                        gpu.ComputeCapabilityMinor ||
                    metadata.MultiProcessorCount !=
                        gpu.MultiProcessorCount)
                {
                    reason = "GPU 配置不同";
                    return false;
                }

                if (metadata.TensorRtMajor != gpu.TensorRtMajor ||
                    metadata.TensorRtMinor != gpu.TensorRtMinor ||
                    metadata.TensorRtPatch != gpu.TensorRtPatch ||
                    metadata.TensorRtBuild != gpu.TensorRtBuild)
                {
                    reason = "TensorRT 版本不同";
                    return false;
                }

                long currentLength =
                    new FileInfo(
                        descriptor.EnginePath).Length;

                if (currentLength <= 0)
                {
                    reason = "Engine 文件为空";
                    return false;
                }

                if (metadata.EngineLengthBytes > 0 &&
                    metadata.EngineLengthBytes != currentLength)
                {
                    reason = "Engine 文件大小与元数据不一致";
                    return false;
                }

                reason = "缓存有效";
                return true;
            }
            catch (Exception ex)
            {
                reason =
                    "缓存检查失败：" + ex.Message;

                return false;
            }
        }

        internal static void WriteMetadata(
            EngineCacheDescriptor descriptor,
            string buildLog)
        {
            FileInfo engineFile =
                new FileInfo(
                    descriptor.EnginePath);

            if (!engineFile.Exists ||
                engineFile.Length <= 0)
            {
                throw new YoloSdkException(
                    "不能为不存在或为空的 Engine 写入缓存元数据。");
            }

            GpuInfo gpu = descriptor.Gpu;

            EngineCacheMetadata metadata =
                new EngineCacheMetadata
                {
                    SchemaVersion =
                        CurrentSchemaVersion,

                    SourceOnnxPath =
                        descriptor.OnnxPath,

                    OnnxSha256 =
                        descriptor.OnnxSha256,

                    Precision =
                        descriptor.Precision,

                    InputWidth =
                        descriptor.InputWidth,

                    InputHeight =
                        descriptor.InputHeight,

                    WorkspaceMiB =
                        descriptor.WorkspaceMiB,

                    GpuName =
                        gpu.Name,

                    ComputeCapabilityMajor =
                        gpu.ComputeCapabilityMajor,

                    ComputeCapabilityMinor =
                        gpu.ComputeCapabilityMinor,

                    MultiProcessorCount =
                        gpu.MultiProcessorCount,

                    TotalGlobalMemoryBytes =
                        gpu.TotalGlobalMemoryBytes,

                    CudaRuntimeVersion =
                        gpu.CudaRuntimeVersion,

                    CudaDriverVersion =
                        gpu.CudaDriverVersion,

                    TensorRtMajor =
                        gpu.TensorRtMajor,

                    TensorRtMinor =
                        gpu.TensorRtMinor,

                    TensorRtPatch =
                        gpu.TensorRtPatch,

                    TensorRtBuild =
                        gpu.TensorRtBuild,

                    EngineLengthBytes =
                        engineFile.Length,

                    CreatedUtc =
                        DateTime.UtcNow.ToString("o"),

                    BuildLog =
                        buildLog ?? string.Empty
                };

            string json =
                Serializer.Serialize(metadata);

            File.WriteAllText(
                descriptor.MetadataPath,
                json,
                new UTF8Encoding(false));
        }

        private static string SanitizeToken(
            string text)
        {
            string sanitized =
                Regex.Replace(
                    (text ?? string.Empty).Trim(),
                    @"[^A-Za-z0-9._-]+",
                    "_")
                .Trim('_', '.', '-');

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "model";

            if (sanitized.Length <= 48)
                return sanitized;

            return sanitized.Substring(0, 48);
        }
    }
}
