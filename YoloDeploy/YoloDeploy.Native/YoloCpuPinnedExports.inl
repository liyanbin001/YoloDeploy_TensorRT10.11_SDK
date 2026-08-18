
int32_t __cdecl YoloDetectImage(
    void* handle,
    const uint8_t* pixels,
    int32_t width,
    int32_t height,
    int32_t stride,
    int32_t pixelFormat,
    float confidenceThreshold,
    float nmsThreshold,
    YoloDetection* results,
    int32_t resultCapacity,
    float* inferenceMilliseconds,
    wchar_t* errorBuffer,
    int32_t errorCapacity)
{
    try
    {
        if (!handle)
            throw std::runtime_error("Detector handle is null.");

        if (!pixels)
            throw std::runtime_error("Image data is null.");

        if (!results || resultCapacity <= 0)
            throw std::runtime_error(
                "Detection result buffer is invalid.");

        confidenceThreshold =
            clampf(confidenceThreshold, 0.0f, 1.0f);

        nmsThreshold =
            clampf(nmsThreshold, 0.0f, 1.0f);

        auto* detector =
            static_cast<Detector*>(handle);

        float ms = 0.0f;

        auto detections =
            detector->detectCpuPinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                confidenceThreshold,
                nmsThreshold,
                ms);

        if (inferenceMilliseconds)
            *inferenceMilliseconds = ms;

        const int32_t count =
            static_cast<int32_t>(
                std::min<size_t>(
                    detections.size(),
                    static_cast<size_t>(resultCapacity)));

        for (int32_t i = 0; i < count; ++i)
        {
            const auto& d =
                detections[static_cast<size_t>(i)];

            results[i] = YoloDetection{
                d.x1,
                d.y1,
                d.x2,
                d.y2,
                d.score,
                d.classId
            };
        }

        setError(L"", errorBuffer, errorCapacity);
        return count;
    }
    catch (const std::exception& ex)
    {
        setError(widen(ex.what()), errorBuffer, errorCapacity);
        return -1;
    }
    catch (...)
    {
        setError(
            L"Unknown CPU-pinned Detect inference error.",
            errorBuffer,
            errorCapacity);

        return -1;
    }
}

int32_t __cdecl YoloDetectObbImage(
    void* handle,
    const uint8_t* pixels,
    int32_t width,
    int32_t height,
    int32_t stride,
    int32_t pixelFormat,
    float confidenceThreshold,
    float nmsThreshold,
    int32_t expectedClassCount,
    YoloObbDetection* results,
    int32_t resultCapacity,
    float* inferenceMilliseconds,
    wchar_t* errorBuffer,
    int32_t errorCapacity)
{
    try
    {
        if (!handle)
            throw std::runtime_error("Detector handle is null.");

        if (!pixels)
            throw std::runtime_error("Image data is null.");

        if (!results || resultCapacity <= 0)
            throw std::runtime_error(
                "OBB result buffer is invalid.");

        confidenceThreshold =
            clampf(confidenceThreshold, 0.0f, 1.0f);

        nmsThreshold =
            clampf(nmsThreshold, 0.0f, 1.0f);

        auto* detector =
            static_cast<Detector*>(handle);

        float ms = 0.0f;

        auto detections =
            detector->detectObbCpuPinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                confidenceThreshold,
                nmsThreshold,
                expectedClassCount,
                ms);

        if (inferenceMilliseconds)
            *inferenceMilliseconds = ms;

        const int32_t count =
            static_cast<int32_t>(
                std::min<size_t>(
                    detections.size(),
                    static_cast<size_t>(resultCapacity)));

        for (int32_t i = 0; i < count; ++i)
        {
            const auto& d =
                detections[static_cast<size_t>(i)];

            const ObbCorners corners =
                obbToCorners(d);

            results[i] = YoloObbDetection{
                d.centerX,
                d.centerY,
                d.width,
                d.height,
                d.angle,
                d.score,
                d.classId,
                corners.p1x,
                corners.p1y,
                corners.p2x,
                corners.p2y,
                corners.p3x,
                corners.p3y,
                corners.p4x,
                corners.p4y
            };
        }

        setError(L"", errorBuffer, errorCapacity);
        return count;
    }
    catch (const std::exception& ex)
    {
        setError(widen(ex.what()), errorBuffer, errorCapacity);
        return -1;
    }
    catch (...)
    {
        setError(
            L"Unknown CPU-pinned OBB inference error.",
            errorBuffer,
            errorCapacity);

        return -1;
    }
}

int32_t __cdecl YoloDetectSegImage(
    void* handle,
    const uint8_t* pixels,
    int32_t width,
    int32_t height,
    int32_t stride,
    int32_t pixelFormat,
    float confidenceThreshold,
    float nmsThreshold,
    float maskThreshold,
    int32_t expectedClassCount,
    YoloSegDetection* results,
    int32_t resultCapacity,
    uint16_t* instanceMask,
    int32_t maskStride,
    float* inferenceMilliseconds,
    wchar_t* errorBuffer,
    int32_t errorCapacity)
{
    try
    {
        if (!handle)
            throw std::runtime_error("Detector handle is null.");

        if (!pixels)
            throw std::runtime_error("Image data is null.");

        if (!results || resultCapacity <= 0)
            throw std::runtime_error(
                "Segmentation result buffer is invalid.");

        if (!instanceMask || maskStride < width)
            throw std::runtime_error(
                "Segmentation instance mask buffer/stride is invalid.");

        confidenceThreshold =
            clampf(confidenceThreshold, 0.0f, 1.0f);

        nmsThreshold =
            clampf(nmsThreshold, 0.0f, 1.0f);

        maskThreshold =
            clampf(maskThreshold, 0.001f, 0.999f);

        auto* detector =
            static_cast<Detector*>(handle);

        float ms = 0.0f;

        auto detections =
            detector->detectSegCpuPinned(
                pixels,
                width,
                height,
                stride,
                pixelFormat,
                confidenceThreshold,
                nmsThreshold,
                maskThreshold,
                expectedClassCount,
                instanceMask,
                maskStride,
                resultCapacity,
                ms);

        if (inferenceMilliseconds)
            *inferenceMilliseconds = ms;

        const int32_t count =
            static_cast<int32_t>(
                std::min<size_t>(
                    detections.size(),
                    static_cast<size_t>(resultCapacity)));

        for (int32_t i = 0; i < count; ++i)
        {
            results[i] =
                detections[static_cast<size_t>(i)];
        }

        setError(L"", errorBuffer, errorCapacity);
        return count;
    }
    catch (const std::exception& ex)
    {
        setError(widen(ex.what()), errorBuffer, errorCapacity);
        return -1;
    }
    catch (...)
    {
        setError(
            L"Unknown CPU-pinned Seg inference error.",
            errorBuffer,
            errorCapacity);

        return -1;
    }
}
