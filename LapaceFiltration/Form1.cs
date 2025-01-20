using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LaplaceFilter
{
    public partial class Form1 : Form
    {
#if DEBUG
        [DllImport("JAAsm.dll")]
#else
        [DllImport("JAAsm.dll")]
#endif
        static extern void ApplyLaplaceFilterAsm(IntPtr input, IntPtr output, int width, int end);

#if DEBUG
        [DllImport("JAcpp.dll")]
#else
        [DllImport("JAcpp.dll")]
#endif
        static extern void ApplyLaplaceFilter(IntPtr input, IntPtr output, int width, int end);

        private Bitmap selectedBitmap; // Bitmap to store the selected image
        private Bitmap modifiedBitmap; // Bitmap to store the modified image
        private readonly int[] allowedValues = { 1, 2, 4, 8, 16, 32, 64 }; // Allowed values for the number of threads

        public Form1()
        {
            InitializeComponent();
            trackBar1.ValueChanged += TrackBar1_ValueChanged; // Attach event handler for trackBar1 value change
        }

        // Event handler for trackBar1 value change
        private void TrackBar1_ValueChanged(object sender, EventArgs e)
        {
            int newValue = FindClosestAllowedValue(trackBar1.Value); // Find the closest allowed value
            if (trackBar1.Value != newValue)
            {
                trackBar1.Value = newValue; // Update trackBar1 value if it is not an allowed value
            }
            label3.Text = $"Selected threads: {trackBar1.Value}"; // Update label to show the selected number of threads
        }

        // Finds the closest allowed value to the given value
        private int FindClosestAllowedValue(int value)
        {
            return allowedValues.OrderBy(v => Math.Abs(v - value)).First(); // Return the closest allowed value
        }

        // Event handler for button1 click (Open Image)
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.bmp;*.jpg;*.png)|*.bmp;*.jpg;*.png" // Filter to show only image files
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                selectedBitmap = new Bitmap(openFileDialog.FileName); // Load the selected image
                pictureBox1.Image = selectedBitmap; // Display the selected image in pictureBox1
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage; // Set the size mode to stretch the image
            }
        }

        // Event handler for button2 click (Apply Filter)
        private void button2_Click(object sender, EventArgs e)
        {
            if (selectedBitmap == null)
            {
                MessageBox.Show("Please select an image using the Open Image button first."); // Show message if no image is selected
                return;
            }

            int numberOfThreads = trackBar1.Value; // Get the number of threads from trackBar1

            Stopwatch stopwatch = new Stopwatch(); // Create a stopwatch to measure processing time
            stopwatch.Start(); // Start the stopwatch

            try
            {
                modifiedBitmap = ModifyImage(selectedBitmap, numberOfThreads); // Apply the Laplace filter to the selected image
                pictureBox2.Image = modifiedBitmap; // Display the modified image in pictureBox2
                pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage; // Set the size mode to stretch the image
                label2.Text = $"Processing time: {stopwatch.ElapsedMilliseconds} ms"; // Update label2 to show the processing time
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}"); // Show message if an error occurs
            }
            finally
            {
                stopwatch.Stop(); // Stop the stopwatch
            }
        }

        // Modifies the image using the Laplace filter
        public unsafe Bitmap ModifyImage(Bitmap bitmap, int threadCount)
        {
            int width = bitmap.Width; // Get the width of the image
            int height = bitmap.Height; // Get the height of the image
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb); // Lock the bitmap data for reading
            int stride = bitmapData.Stride; // Get the stride (number of bytes per row)
            int byteCount = stride * height; // Calculate the total number of bytes in the image

            int unpaddedRowSize = width * 3; // Calculate the size of a row without padding (3 bytes per pixel)
            byte[] inputBytes = new byte[unpaddedRowSize * height]; // Create an array to store the input bytes without padding
            byte[] outputBytes = new byte[unpaddedRowSize * height]; // Create an array to store the output bytes without padding

            // Copy each row separately to remove padding
            for (int y = 0; y < height; ++y)
            {
                IntPtr mem = (IntPtr)((long)bitmapData.Scan0 + y * stride); // Get the pointer to the start of the row
                Marshal.Copy(mem, inputBytes, y * unpaddedRowSize, unpaddedRowSize); // Copy the row to the inputBytes array
            }
            bitmap.UnlockBits(bitmapData); // Unlock the bitmap data

            fixed (byte* inputPtr = inputBytes)
            fixed (byte* outputPtr = outputBytes)
            {
                IntPtr inputIntPtr = (IntPtr)inputPtr; // Get the IntPtr for the input byte
                IntPtr outputIntPtr = (IntPtr)outputPtr; // Get the IntPtr for the output bytes

                int rowsPerThread2 = ((height - 2) / threadCount) * width * 3; // Calculate the number of rows per thread (excluding the first and last row)
                int remainingRows2 = ((height - 2) % threadCount) * width * 3; // Calculate the remaining rows

                if (asmCheckBox.Checked)
                {
                    Parallel.For(0, threadCount, i =>
                    {
                        int startPrzes = i * rowsPerThread2 + 3 * width; // Calculate the start position for the current thread
                        int ilerobic = (i == threadCount - 1) ? (rowsPerThread2 + remainingRows2) : rowsPerThread2; // Calculate the number of bytes to process for the current thread
                        ApplyLaplaceFilterAsm(inputIntPtr + startPrzes, outputIntPtr + startPrzes, width, ilerobic); // Call the assembly function to apply the Laplace filter
                    });
                }
                else
                {
                    Parallel.For(0, threadCount, i =>
                    {
                        int startPrzes = i * rowsPerThread2 + 3 * width; // Calculate the start position for the current thread
                        int ilerobic = (i == threadCount - 1) ? (rowsPerThread2 + remainingRows2) : rowsPerThread2; // Calculate the number of bytes to process for the current thread
                        ApplyLaplaceFilter(inputIntPtr + startPrzes, outputIntPtr + startPrzes, width, ilerobic); // Call the C++ function to apply the Laplace filter
                    });
                }
            }

            Bitmap resultBitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb); // Create a new bitmap to store the result
            BitmapData resultData = resultBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb); // Lock the result bitmap data for writing

            // Copy output bytes back to the result bitmap
            for (int y = 0; y < height; ++y)
            {
                IntPtr mem = (IntPtr)((long)resultData.Scan0 + y * stride); // Get the pointer to the start of the row
                Marshal.Copy(outputBytes, y * unpaddedRowSize, mem, unpaddedRowSize); // Copy the row from the outputBytes array to the result bitmap
            }
            resultBitmap.UnlockBits(resultData); // Unlock the result bitmap data

            return resultBitmap; // Return the result bitmap
        }

        // Event handler for asmCheckBox checked change
        private void asmCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (asmCheckBox.Checked)
            {
                cppCheckBox.Checked = false; // Uncheck cppCheckBox if asmCheckBox is checked
            }
        }

        // Event handler for cppCheckBox checked change
        private void cppCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (cppCheckBox.Checked)
            {
                asmCheckBox.Checked = false; // Uncheck asmCheckBox if cppCheckBox is checked
            }
        }

        // Event handler for button3 click (Save Image)
        private void button3_Click(object sender, EventArgs e)
        {
            if (modifiedBitmap == null)
            {
                MessageBox.Show("Please process the image using the Apply Filter button first."); // Show message if no image is processed
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Image files (*.bmp;*.jpg)|*.bmp;*.jpg", // Filter to show only image files
                DefaultExt = "bmp", // Set the default extension to bmp
                AddExtension = true // Add the extension if the user does not provide one
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                modifiedBitmap.Save(saveFileDialog.FileName, ImageFormat.Bmp); // Save the modified image
                MessageBox.Show("Image has been saved."); // Show message when the image is saved
            }
        }
    }
}
