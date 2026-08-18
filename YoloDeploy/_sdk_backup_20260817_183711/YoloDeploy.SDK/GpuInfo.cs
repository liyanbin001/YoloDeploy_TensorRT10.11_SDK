using System.Text;
using System.Text.Json;

namespace YoloDeploy.SDK;

internal sealed class GpuInfo
{
    public int DeviceIndex { get; set; }
    public int DeviceCount { get; set; }
    public string Name { get; set; } = "";
    public int ComputeCapabilityMajor { get; set; }
    public int ComputeCapabilityMinor { get; set; }
    public ulong TotalGlobalMemoryBytes { get; set; }
    public int MultiProcessorCount { get; set; }
    public int CudaRuntimeVersion { get; set; }
    public int CudaDriverVersion { get; set; }
    public int TensorRtMajor { get; set; }
    public int TensorRtMinor { get; set; }
    public int TensorRtPatch { get; set; }
    public int TensorRtBuild { get; set; }

    public YoloRuntimeInfo ToPublic() => new()
    {
        GpuName = Name,
        ComputeCapabilityMajor = ComputeCapabilityMajor,
        ComputeCapabilityMinor = ComputeCapabilityMinor,
        TotalGlobalMemoryBytes = TotalGlobalMemoryBytes,
        MultiProcessorCount = MultiProcessorCount,
        CudaRuntimeVersion = CudaRuntimeVersion,
        CudaDriverVersion = CudaDriverVersion,
        TensorRtMajor = TensorRtMajor,
        TensorRtMinor = TensorRtMinor,
        TensorRtPatch = TensorRtPatch,
        TensorRtBuild = TensorRtBuild
    };
}

internal static class GpuInfoProvider
{
    internal static GpuInfo Query()
    {
        try
        {
            var json = new StringBuilder(8192);
            var error = new StringBuilder(4096);

            int code = NativeMethods.YoloGetGpuInfoJson(
                json, json.Capacity, error, error.Capacity);

            if (code != 0)
                throw new YoloSdkException(
                    $"读取 GPU / CUDA / TensorRT 信息失败：{error}");

            GpuInfo? info = JsonSerializer.Deserialize<GpuInfo>(
                json.ToString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return info ??
                throw new YoloSdkException("Native 返回的 GPU JSON 无法解析。");
        }
        catch (DllNotFoundException ex)
        {
            throw new YoloSdkException(
                "无法加载 YoloDeploy.Native.dll 或其 TensorRT/CUDA 依赖。请使用完整 Runtime 目录。",
                ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new YoloSdkException(
                "Native DLL 位数不匹配。SDK 仅支持 Windows x64。",
                ex);
        }
    }
}
