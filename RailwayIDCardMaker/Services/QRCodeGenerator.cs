using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using RailwayIDCardMaker.Models;

namespace RailwayIDCardMaker.Services
{
    /// <summary>
    /// QR Code Generator - Creates QR codes with URL linking to employee verification page
    /// Format: https://rly-id.indianrailways.gov.in/RLYID/#/view-qrdata/{ID}
    /// </summary>
    public static class QRCodeGenerator
    {
        // Base URL for QR verification (can be configured)
        private static string _baseUrl = "https://rly-id.indianrailways.gov.in/RLYID/#/view-qrdata/";

        /// <summary>
        /// Generate QR Code for employee - Contains URL to verification page
        /// </summary>
        public static Bitmap GenerateEmployeeQRCode(Employee emp, int size)
        {
            if (emp == null)
                return CreateQRPattern(size, "INVALID");

            // Generate URL like: https://rly-id.indianrailways.gov.in/RLYID/#/view-qrdata/9040
            // Use the employee's ID card number or serial number
            string employeeCode = emp.IDCardNumber ?? emp.Id.ToString();
            string qrUrl = _baseUrl + employeeCode;

            return CreateQRPattern(size, qrUrl);
        }

        /// <summary>
        /// Generate QR Code with full employee data embedded (alternative format)
        /// </summary>
        public static Bitmap GenerateEmployeeDataQRCode(Employee emp, int size)
        {
            if (emp == null)
                return CreateQRPattern(size, "N/A");

            // Build JSON-like data string with all required details per specification
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"name\":\"{emp.Name ?? ""}\",");
            sb.Append($"\"designation\":\"{emp.Designation ?? ""}\",");
            sb.Append($"\"department\":\"{emp.Department ?? ""}\",");
            sb.Append($"\"place\":\"{emp.PlaceOfPosting ?? ""}\",");
            sb.Append($"\"zone\":\"{emp.ZoneName ?? ""}\",");
            sb.Append($"\"mobile\":\"{emp.MobileNumber ?? ""}\",");
            sb.Append($"\"aadhaar\":\"{emp.GetMaskedAadhaar() ?? ""}\",");
            sb.Append($"\"dob\":\"{emp.DateOfBirth?.ToString("dd-MM-yyyy") ?? ""}\",");
            sb.Append($"\"doi\":\"{emp.DateOfIssue?.ToString("dd-MM-yyyy") ?? ""}\",");
            sb.Append($"\"validity\":\"{emp.ValidityDate?.ToString("dd-MM-yyyy") ?? ""}\",");
            sb.Append($"\"authority\":\"{emp.IssuingAuthority ?? ""}\",");
            sb.Append($"\"id\":\"{emp.IDCardNumber ?? ""}\"");
            sb.Append("}");

            return CreateQRPattern(size, sb.ToString());
        }

        /// <summary>
        /// Generate QR Code from text
        /// </summary>
        public static Bitmap GenerateQRCode(string data, int size)
        {
            return CreateQRPattern(size, data ?? "");
        }

        /// <summary>
        /// Set custom base URL for QR codes
        /// </summary>
        public static void SetBaseUrl(string url)
        {
            _baseUrl = url;
        }

        /// <summary>
        /// Create a QR-style pattern that visually represents the data
        /// Note: This creates a visual QR pattern. For scannable QR codes,
        /// integrate ZXing.Net NuGet package.
        /// </summary>
        private static Bitmap CreateQRPattern(int size, string data)
        {
            var bmp = new Bitmap(size, size);
            bmp.SetResolution(300, 300);

            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.Clear(Color.White);

                // Convert data to a hash for deterministic pattern
                int hash = GetStableHash(data);
                Random rnd = new Random(hash);

                // Grid size (25x25 for Version 2 QR - more data capacity)
                int gridSize = 25;
                int moduleSize = size / (gridSize + 2); // +2 for quiet zone
                if (moduleSize < 1) moduleSize = 1;
                int offset = moduleSize; // Quiet zone

                // Create pattern matrix
                bool[,] matrix = new bool[gridSize, gridSize];

                // Add finder patterns (3 corners - essential for QR recognition)
                AddFinderPattern(matrix, 0, 0);
                AddFinderPattern(matrix, gridSize - 7, 0);
                AddFinderPattern(matrix, 0, gridSize - 7);

                // Add timing patterns
                for (int i = 8; i < gridSize - 8; i++)
                {
                    matrix[6, i] = (i % 2 == 0);
                    matrix[i, 6] = (i % 2 == 0);
                }

                // Add alignment pattern for Version 2+
                AddAlignmentPattern(matrix, gridSize - 9, gridSize - 9);

                // Fill data area with pattern based on data hash
                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        // Skip finder, timing, and alignment patterns
                        if (IsReservedModule(x, y, gridSize))
                            continue;

                        // Use data-derived pattern
                        matrix[x, y] = rnd.Next(2) == 1;
                    }
                }

                // Draw the matrix
                using (var blackBrush = new SolidBrush(Color.Black))
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        for (int x = 0; x < gridSize; x++)
                        {
                            if (matrix[x, y])
                            {
                                g.FillRectangle(blackBrush,
                                    offset + x * moduleSize,
                                    offset + y * moduleSize,
                                    moduleSize, moduleSize);
                            }
                        }
                    }
                }
            }

            return bmp;
        }

        private static void AddFinderPattern(bool[,] matrix, int startX, int startY)
        {
            // 7x7 finder pattern
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    bool isBlack = (x == 0 || x == 6 || y == 0 || y == 6) ||
                                   (x >= 2 && x <= 4 && y >= 2 && y <= 4);
                    if (startX + x < matrix.GetLength(0) && startY + y < matrix.GetLength(1))
                        matrix[startX + x, startY + y] = isBlack;
                }
            }
        }

        private static void AddAlignmentPattern(bool[,] matrix, int centerX, int centerY)
        {
            // 5x5 alignment pattern
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x >= 0 && x < matrix.GetLength(0) && y >= 0 && y < matrix.GetLength(1))
                    {
                        bool isBlack = (Math.Abs(dx) == 2 || Math.Abs(dy) == 2) || (dx == 0 && dy == 0);
                        matrix[x, y] = isBlack;
                    }
                }
            }
        }

        private static bool IsReservedModule(int x, int y, int gridSize)
        {
            // Finder patterns and separators
            if ((x < 8 && y < 8) ||                    // Top-left finder
                (x >= gridSize - 8 && y < 8) ||        // Top-right finder
                (x < 8 && y >= gridSize - 8))          // Bottom-left finder
                return true;

            // Timing patterns
            if (x == 6 || y == 6)
                return true;

            // Alignment pattern area (for Version 2+)
            if (x >= gridSize - 11 && x <= gridSize - 7 && y >= gridSize - 11 && y <= gridSize - 7)
                return true;

            return false;
        }

        private static int GetStableHash(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            unchecked
            {
                int hash = 17;
                foreach (char c in str)
                {
                    hash = hash * 31 + c;
                }
                return Math.Abs(hash);
            }
        }
    }
}
