
        // =============================================================
        // CPU preprocess + persistent pinned TensorRT input.
        // No custom CUDA kernel is used.
        // =============================================================

        int preprocessMapSourceW = -1;
        int preprocessMapSourceH = -1;
        int preprocessMapDestinationW = -1;
        int preprocessMapDestinationH = -1;
        int preprocessMapResizedW = -1;
        int preprocessMapResizedH = -1;

        std::vector<int> preprocessX0;
        std::vector<int> preprocessX1;
        std::vector<float> preprocessFx;
        std::vector<int> preprocessY0;
        std::vector<int> preprocessY1;
        std::vector<float> preprocessFy;

        static int imageBytesPerPixel(int pixelFormat)
        {
            switch (pixelFormat)
            {
            case YoloPixelFormat_Bgra32:
                return 4;
            case YoloPixelFormat_Bgr24:
            case YoloPixelFormat_Rgb24:
                return 3;
            case YoloPixelFormat_Gray8:
                return 1;
            default:
                throw std::runtime_error("Unsupported image pixel format.");
            }
        }

        void ensureResizeMaps(
            int sourceW,
            int sourceH,
            int destinationW,
            int destinationH,
            float scale,
            int resizedW,
            int resizedH)
        {
            if (preprocessMapSourceW == sourceW &&
                preprocessMapSourceH == sourceH &&
                preprocessMapDestinationW == destinationW &&
                preprocessMapDestinationH == destinationH &&
                preprocessMapResizedW == resizedW &&
                preprocessMapResizedH == resizedH)
            {
                return;
            }

            preprocessX0.resize(static_cast<size_t>(resizedW));
            preprocessX1.resize(static_cast<size_t>(resizedW));
            preprocessFx.resize(static_cast<size_t>(resizedW));
            preprocessY0.resize(static_cast<size_t>(resizedH));
            preprocessY1.resize(static_cast<size_t>(resizedH));
            preprocessFy.resize(static_cast<size_t>(resizedH));

            for (int x = 0; x < resizedW; ++x)
            {
                float sx =
                    (static_cast<float>(x) + 0.5f) / scale - 0.5f;

                sx = clampf(
                    sx,
                    0.0f,
                    static_cast<float>(sourceW - 1));

                const int x0 =
                    static_cast<int>(std::floor(sx));

                const int x1 =
                    std::min(x0 + 1, sourceW - 1);

                preprocessX0[static_cast<size_t>(x)] = x0;
                preprocessX1[static_cast<size_t>(x)] = x1;
                preprocessFx[static_cast<size_t>(x)] =
                    sx - static_cast<float>(x0);
            }

            for (int y = 0; y < resizedH; ++y)
            {
                float sy =
                    (static_cast<float>(y) + 0.5f) / scale - 0.5f;

                sy = clampf(
                    sy,
                    0.0f,
                    static_cast<float>(sourceH - 1));

                const int y0 =
                    static_cast<int>(std::floor(sy));

                const int y1 =
                    std::min(y0 + 1, sourceH - 1);

                preprocessY0[static_cast<size_t>(y)] = y0;
                preprocessY1[static_cast<size_t>(y)] = y1;
                preprocessFy[static_cast<size_t>(y)] =
                    sy - static_cast<float>(y0);
            }

            preprocessMapSourceW = sourceW;
            preprocessMapSourceH = sourceH;
            preprocessMapDestinationW = destinationW;
            preprocessMapDestinationH = destinationH;
            preprocessMapResizedW = resizedW;
            preprocessMapResizedH = resizedH;
        }

        template <int PixelFormat>
        static void loadRgbPixel(
            const uint8_t* pixels,
            int stride,
            int x,
            int y,
            float& red,
            float& green,
            float& blue)
        {
            const uint8_t* row =
                pixels
                + static_cast<size_t>(y)
                * static_cast<size_t>(stride);

            if constexpr (PixelFormat == YoloPixelFormat_Bgra32)
            {
                const uint8_t* value =
                    row + static_cast<size_t>(x) * 4;

                blue = static_cast<float>(value[0]);
                green = static_cast<float>(value[1]);
                red = static_cast<float>(value[2]);
            }
            else if constexpr (PixelFormat == YoloPixelFormat_Bgr24)
            {
                const uint8_t* value =
                    row + static_cast<size_t>(x) * 3;

                blue = static_cast<float>(value[0]);
                green = static_cast<float>(value[1]);
                red = static_cast<float>(value[2]);
            }
            else if constexpr (PixelFormat == YoloPixelFormat_Rgb24)
            {
                const uint8_t* value =
                    row + static_cast<size_t>(x) * 3;

                red = static_cast<float>(value[0]);
                green = static_cast<float>(value[1]);
                blue = static_cast<float>(value[2]);
            }
            else
            {
                const float gray =
                    static_cast<float>(row[x]);

                red = gray;
                green = gray;
                blue = gray;
            }
        }

        template <int PixelFormat>
        static void bilinearRgbCached(
            const uint8_t* pixels,
            int stride,
            int x0,
            int x1,
            int y0,
            int y1,
            float fx,
            float fy,
            float& red,
            float& green,
            float& blue)
        {
            float r00 = 0.0f, g00 = 0.0f, b00 = 0.0f;
            float r10 = 0.0f, g10 = 0.0f, b10 = 0.0f;
            float r01 = 0.0f, g01 = 0.0f, b01 = 0.0f;
            float r11 = 0.0f, g11 = 0.0f, b11 = 0.0f;

            loadRgbPixel<PixelFormat>(
                pixels, stride, x0, y0, r00, g00, b00);
            loadRgbPixel<PixelFormat>(
                pixels, stride, x1, y0, r10, g10, b10);
            loadRgbPixel<PixelFormat>(
                pixels, stride, x0, y1, r01, g01, b01);
            loadRgbPixel<PixelFormat>(
                pixels, stride, x1, y1, r11, g11, b11);

            const float topR = r00 + (r10 - r00) * fx;
            const float topG = g00 + (g10 - g00) * fx;
            const float topB = b00 + (b10 - b00) * fx;

            const float bottomR = r01 + (r11 - r01) * fx;
            const float bottomG = g01 + (g11 - g01) * fx;
            const float bottomB = b01 + (b11 - b01) * fx;

            red = topR + (bottomR - topR) * fy;
            green = topG + (bottomG - topG) * fy;
            blue = topB + (bottomB - topB) * fy;
        }

        template <typename TensorType>
        static TensorType inputTensorValue(float value)
        {
            if constexpr (std::is_same_v<TensorType, float>)
                return value;
            else
                return __float2half(value);
        }

        template <int PixelFormat, typename TensorType>
        LetterboxInfo preprocessImageTyped(
            const uint8_t* pixels,
            int sourceW,
            int sourceH,
            int stride,
            TensorType* chw)
        {
            const float scale =
                std::min(
                    static_cast<float>(inputW)
                        / static_cast<float>(sourceW),
                    static_cast<float>(inputH)
                        / static_cast<float>(sourceH));

            const int resizedW =
                static_cast<int>(
                    std::round(
                        static_cast<float>(sourceW) * scale));

            const int resizedH =
                static_cast<int>(
                    std::round(
                        static_cast<float>(sourceH) * scale));

            const float dw =
                static_cast<float>(inputW - resizedW) / 2.0f;

            const float dh =
                static_cast<float>(inputH - resizedH) / 2.0f;

            const int left =
                static_cast<int>(std::round(dw - 0.1f));

            const int top =
                static_cast<int>(std::round(dh - 0.1f));

            ensureResizeMaps(
                sourceW,
                sourceH,
                inputW,
                inputH,
                scale,
                resizedW,
                resizedH);

            const size_t plane =
                static_cast<size_t>(inputW)
                * static_cast<size_t>(inputH);

            const TensorType padding =
                inputTensorValue<TensorType>(
                    114.0f / 255.0f);

            std::fill_n(
                chw,
                plane * 3,
                padding);

            constexpr float inverse255 =
                1.0f / 255.0f;

            for (int y = 0; y < resizedH; ++y)
            {
                const int destinationY = top + y;

                if (destinationY < 0 ||
                    destinationY >= inputH)
                {
                    continue;
                }

                const int y0 =
                    preprocessY0[static_cast<size_t>(y)];
                const int y1 =
                    preprocessY1[static_cast<size_t>(y)];
                const float fy =
                    preprocessFy[static_cast<size_t>(y)];

                for (int x = 0; x < resizedW; ++x)
                {
                    const int destinationX = left + x;

                    if (destinationX < 0 ||
                        destinationX >= inputW)
                    {
                        continue;
                    }

                    const int x0 =
                        preprocessX0[static_cast<size_t>(x)];
                    const int x1 =
                        preprocessX1[static_cast<size_t>(x)];
                    const float fx =
                        preprocessFx[static_cast<size_t>(x)];

                    float red = 0.0f;
                    float green = 0.0f;
                    float blue = 0.0f;

                    bilinearRgbCached<PixelFormat>(
                        pixels,
                        stride,
                        x0,
                        x1,
                        y0,
                        y1,
                        fx,
                        fy,
                        red,
                        green,
                        blue);

                    const size_t index =
                        static_cast<size_t>(destinationY)
                        * static_cast<size_t>(inputW)
                        + static_cast<size_t>(destinationX);

                    chw[index] =
                        inputTensorValue<TensorType>(
                            red * inverse255);

                    chw[plane + index] =
                        inputTensorValue<TensorType>(
                            green * inverse255);

                    chw[plane * 2 + index] =
                        inputTensorValue<TensorType>(
                            blue * inverse255);
                }
            }

            return LetterboxInfo{
                scale,
                left,
                top,
                resizedW,
                resizedH
            };
        }

        template <typename TensorType>
        LetterboxInfo dispatchPixelFormat(
            const uint8_t* pixels,
            int sourceW,
            int sourceH,
            int stride,
            int pixelFormat,
            TensorType* chw)
        {
            switch (pixelFormat)
            {
            case YoloPixelFormat_Bgra32:
                return preprocessImageTyped<
                    YoloPixelFormat_Bgra32>(
                        pixels,
                        sourceW,
                        sourceH,
                        stride,
                        chw);

            case YoloPixelFormat_Bgr24:
                return preprocessImageTyped<
                    YoloPixelFormat_Bgr24>(
                        pixels,
                        sourceW,
                        sourceH,
                        stride,
                        chw);

            case YoloPixelFormat_Rgb24:
                return preprocessImageTyped<
                    YoloPixelFormat_Rgb24>(
                        pixels,
                        sourceW,
                        sourceH,
                        stride,
                        chw);

            case YoloPixelFormat_Gray8:
                return preprocessImageTyped<
                    YoloPixelFormat_Gray8>(
                        pixels,
                        sourceW,
                        sourceH,
                        stride,
                        chw);

            default:
                throw std::runtime_error(
                    "Unsupported image pixel format.");
            }
        }

        LetterboxInfo preparePinnedInput(
            const uint8_t* pixels,
            int width,
            int height,
            int stride,
            int pixelFormat)
        {
            if (!pixels ||
                width <= 0 ||
                height <= 0)
            {
                throw std::runtime_error(
                    "Invalid image.");
            }

            const int bytesPerPixel =
                imageBytesPerPixel(pixelFormat);

            if (stride < width * bytesPerPixel)
            {
                throw std::runtime_error(
                    "Image stride is too small for the selected pixel format.");
            }

            if (!inputHost.ptr ||
                inputHost.bytes != inputDevice.bytes)
            {
                throw std::runtime_error(
                    "Pinned input host buffer is not initialized.");
            }

            LetterboxInfo letterbox;

            if (inputType == nvinfer1::DataType::kFLOAT)
            {
                letterbox =
                    dispatchPixelFormat(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        static_cast<float*>(inputHost.ptr));
            }
            else if (inputType == nvinfer1::DataType::kHALF)
            {
                letterbox =
                    dispatchPixelFormat(
                        pixels,
                        width,
                        height,
                        stride,
                        pixelFormat,
                        static_cast<__half*>(inputHost.ptr));
            }
            else
            {
                throw std::runtime_error(
                    "Only FP32/FP16 input tensors are supported.");
            }

            checkCuda(
                cudaMemcpyAsync(
                    inputDevice.ptr,
                    inputHost.ptr,
                    inputDevice.bytes,
                    cudaMemcpyHostToDevice,
                    stream.stream),
                "cudaMemcpyAsync(pinned input)");

            return letterbox;
        }

        std::vector<Candidate> detectCpuPinned(
            const uint8_t* pixels,
            int width,
            int height,
            int stride,
            int pixelFormat,
            float confidence,
            float nmsThreshold,
            float& inferenceMs)
        {
            std::lock_guard<std::mutex> lock(mutex);

            const LetterboxInfo letterbox =
                preparePinnedInput(
                    pixels,
                    width,
                    height,
                    stride,
                    pixelFormat);

            cudaEvent_t start = nullptr;
            cudaEvent_t stop = nullptr;

            checkCuda(
                cudaEventCreate(&start),
                "cudaEventCreate(start)");

            checkCuda(
                cudaEventCreate(&stop),
                "cudaEventCreate(stop)");

            try
            {
                checkCuda(
                    cudaEventRecord(start, stream.stream),
                    "cudaEventRecord(start)");

                if (!context->enqueueV3(stream.stream))
                    throw std::runtime_error(
                        "TensorRT enqueueV3 failed.");

                checkCuda(
                    cudaEventRecord(stop, stream.stream),
                    "cudaEventRecord(stop)");

                checkCuda(
                    cudaEventSynchronize(stop),
                    "cudaEventSynchronize(stop)");

                checkCuda(
                    cudaEventElapsedTime(
                        &inferenceMs,
                        start,
                        stop),
                    "cudaEventElapsedTime");

                std::vector<float> output =
                    copyOutputToFloat();

                auto decoded =
                    decodeOutput(
                        output,
                        confidence,
                        letterbox,
                        width,
                        height);

                auto finalBoxes =
                    classAwareNms(
                        std::move(decoded),
                        nmsThreshold);

                cudaEventDestroy(start);
                cudaEventDestroy(stop);

                return finalBoxes;
            }
            catch (...)
            {
                if (start)
                    cudaEventDestroy(start);

                if (stop)
                    cudaEventDestroy(stop);

                throw;
            }
        }

        std::vector<ObbCandidate> detectObbCpuPinned(
            const uint8_t* pixels,
            int width,
            int height,
            int stride,
            int pixelFormat,
            float confidence,
            float nmsThreshold,
            int expectedClassCount,
            float& inferenceMs)
        {
            std::lock_guard<std::mutex> lock(mutex);

            const LetterboxInfo letterbox =
                preparePinnedInput(
                    pixels,
                    width,
                    height,
                    stride,
                    pixelFormat);

            cudaEvent_t start = nullptr;
            cudaEvent_t stop = nullptr;

            checkCuda(
                cudaEventCreate(&start),
                "cudaEventCreate(start)");

            checkCuda(
                cudaEventCreate(&stop),
                "cudaEventCreate(stop)");

            try
            {
                checkCuda(
                    cudaEventRecord(start, stream.stream),
                    "cudaEventRecord(start)");

                if (!context->enqueueV3(stream.stream))
                    throw std::runtime_error(
                        "TensorRT enqueueV3 failed.");

                checkCuda(
                    cudaEventRecord(stop, stream.stream),
                    "cudaEventRecord(stop)");

                checkCuda(
                    cudaEventSynchronize(stop),
                    "cudaEventSynchronize(stop)");

                checkCuda(
                    cudaEventElapsedTime(
                        &inferenceMs,
                        start,
                        stop),
                    "cudaEventElapsedTime");

                std::vector<float> output =
                    copyOutputToFloat();

                auto decoded =
                    decodeObbOutput(
                        output,
                        confidence,
                        letterbox,
                        width,
                        height,
                        expectedClassCount);

                auto finalBoxes =
                    classAwareRotatedNms(
                        std::move(decoded),
                        nmsThreshold);

                cudaEventDestroy(start);
                cudaEventDestroy(stop);

                return finalBoxes;
            }
            catch (...)
            {
                if (start)
                    cudaEventDestroy(start);

                if (stop)
                    cudaEventDestroy(stop);

                throw;
            }
        }

        std::vector<YoloSegDetection> detectSegCpuPinned(
            const uint8_t* pixels,
            int width,
            int height,
            int stride,
            int pixelFormat,
            float confidence,
            float nmsThreshold,
            float maskThreshold,
            int expectedClassCount,
            uint16_t* instanceMask,
            int maskStride,
            int resultCapacity,
            float& inferenceMs)
        {
            std::lock_guard<std::mutex> lock(mutex);

            if (!hasProto)
            {
                throw std::runtime_error(
                    "This Engine has no segmentation proto output. "
                    "Use a YOLO26-seg ONNX/Engine with prediction + proto outputs.");
            }

            const LetterboxInfo letterbox =
                preparePinnedInput(
                    pixels,
                    width,
                    height,
                    stride,
                    pixelFormat);

            cudaEvent_t start = nullptr;
            cudaEvent_t stop = nullptr;

            checkCuda(
                cudaEventCreate(&start),
                "cudaEventCreate(start)");

            checkCuda(
                cudaEventCreate(&stop),
                "cudaEventCreate(stop)");

            try
            {
                checkCuda(
                    cudaEventRecord(start, stream.stream),
                    "cudaEventRecord(start)");

                if (!context->enqueueV3(stream.stream))
                    throw std::runtime_error(
                        "TensorRT enqueueV3 failed.");

                checkCuda(
                    cudaEventRecord(stop, stream.stream),
                    "cudaEventRecord(stop)");

                checkCuda(
                    cudaEventSynchronize(stop),
                    "cudaEventSynchronize(stop)");

                checkCuda(
                    cudaEventElapsedTime(
                        &inferenceMs,
                        start,
                        stop),
                    "cudaEventElapsedTime");

                std::vector<float> prediction =
                    copyOutputToFloat();

                std::vector<float> proto =
                    copyProtoToFloat();

                auto candidates =
                    decodeSegCandidates(
                        prediction,
                        confidence,
                        nmsThreshold,
                        expectedClassCount);

                auto results =
                    buildSegmentationResults(
                        candidates,
                        proto,
                        letterbox,
                        width,
                        height,
                        maskThreshold,
                        instanceMask,
                        maskStride,
                        resultCapacity);

                cudaEventDestroy(start);
                cudaEventDestroy(stop);

                return results;
            }
            catch (...)
            {
                if (start)
                    cudaEventDestroy(start);

                if (stop)
                    cudaEventDestroy(stop);

                throw;
            }
        }
