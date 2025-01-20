/*
 * Author: Blazej Sztefka
 * Date: January 7, 2025, Year 2024/2025, Semester 1
 * Project: Laplace Filter Implementation in C++
 * Description: This program applies a Laplace filter to emphasize edges in an image.
 *              The algorithm processes an image index by index, ignoring edge pixels on the left and right sides of each row.
 * Version: 1.2
 */
#include "pch.h"
#include <cstdint>
#include <cstring>

 // Function: ApplyLaplaceFilter
 // Description: Applies a Laplace filter to emphasize edges in an image.
 // Parameters:
 //   - input: Pointer to the input array. Points to the first pixel of the first row (leftmost pixel).
 //   - output: Pointer to the output array. Points to the first pixel of the first row (leftmost pixel).
 //   - width: The number of pixels per row.
 //   - end: Total number of pixels to process.
 // Note: Processes the image index by index, skipping the edge pixels on the left and right sides of rows.
extern "C" __declspec(dllexport)
void ApplyLaplaceFilter(uint8_t* input, uint8_t* output, int width, int end) {
    int extendedWidth = width * 3; // Total bytes per row (RGB values for width pixels)

    for (int i = 0; i < end; ++i) {
        // Skip edge pixels on the left and right
        if (i % extendedWidth <= 2 || (i + 3) % extendedWidth <= 2) {
            continue;
        }

        // Initialize result value to 4 times the current pixel value
        int result = 4 * input[i];

        // Subtract values from the neighbors (left, right, top, bottom)
        result -= input[i - 3];     // Left neighbor
        result -= input[i + 3];     // Right neighbor
        result -= input[i - extendedWidth]; // Top neighbor
        result -= input[i + extendedWidth]; // Bottom neighbor

        // Clamp the result to the range [0, 255]
        if (result < 0) result = 0;
        if (result > 255) result = 255;

        // Store the result in the output array
        output[i] = static_cast<uint8_t>(result);
    }
}