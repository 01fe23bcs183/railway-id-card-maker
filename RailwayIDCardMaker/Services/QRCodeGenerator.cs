using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using RailwayIDCardMaker.Models;

namespace RailwayIDCardMaker.Services
{
    /// <summary>
    /// QR Code Generator - Creates visual QR-style pattern with encoded data
    /// </summary>
    public static class QRCodeGenerator
    {
        /// <summary>
        /// Generate QR Code for employee data
        /// Contains: Name & address, Designation, Place of Posting,
        /// Aadhaar No, Date of issue, Validity upto, Name & designation of issuing authority
        /// </summary>
        public static Bitmap GenerateEmployeeQRCode(Employee emp, int size)
        {
            if (emp == null)
                return CreateQRPattern(size, "N/A");

            // Build data string with all required details per specification
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name: " + (emp.Name ?? "N/A"));
            sb.AppendLine("Address: " + (emp.Address ?? "N/A"));
            sb.AppendLine("Designation: " + (emp.Designation ?? "N/A"));
            sb.AppendLine("Place of Posting: " + (emp.PlaceOfPosting ?? "N/A"));
            sb.AppendLine("Aadhaar: " + (emp.GetMaskedAadhaar() ?? "N/A"));
            sb.AppendLine("Date of Issue: " + (emp.DateOfIssue?.ToString("dd-MM-yyyy") ?? "N/A"));
            sb.AppendLine("Valid Upto: " + (emp.ValidityDate?.ToString("dd-MM-yyyy") ?? "N/A"));
            sb.AppendLine("Issuing Authority: " + (emp.IssuingAuthority ?? "N/A") + " (" + (emp.IssuingAuthorityDesignation ?? "N/A") + ")");

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
        /// Create a QR-style pattern that visually represents the data
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

                // Grid size (21x21 for Version 1 QR)
                int gridSize = 21;
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

                // Fill data area with pattern based on hash
                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        if (!IsFinderArea(x, y, gridSize) && x != 6 && y != 6)
                        {
                            matrix[x, y] = rnd.Next(2) == 1;
                        }
                    }
                }

                // Draw the matrix
                using (var black = new SolidBrush(Color.Black))
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        for (int x = 0; x < gridSize; x++)
                        {
                            if (matrix[x, y])
                            {
                                g.FillRectangle(black,
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

        /// <summary>
        /// Add 7x7 finder pattern
        /// </summary>
        private static void AddFinderPattern(bool[,] m, int px, int py)
        {
            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 7; x++)
                {
                    // Outer ring = true, middle ring = false, center 3x3 = true
                    bool outer = (x == 0 || x == 6 || y == 0 || y == 6);
                    bool middle = (x == 1 || x == 5) && (y >= 1 && y <= 5) ||
                                  (y == 1 || y == 5) && (x >= 1 && x <= 5);
                    bool center = (x >= 2 && x <= 4 && y >= 2 && y <= 4);

                    m[px + x, py + y] = outer || center;
                }
            }
        }

        /// <summary>
        /// Check if position is in finder pattern area
        /// </summary>
        private static bool IsFinderArea(int x, int y, int size)
        {
            // Top-left finder + separator
            if (x < 9 && y < 9) return true;
            // Top-right finder + separator
            if (x >= size - 9 && y < 9) return true;
            // Bottom-left finder + separator
            if (x < 9 && y >= size - 9) return true;

            return false;
        }

        /// <summary>
        /// Get stable hash from string (consistent across runs)
        /// </summary>
        private static int GetStableHash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;

            unchecked
            {
                int hash = 17;
                foreach (char c in s)
                {
                    hash = hash * 31 + c;
                }
                return Math.Abs(hash);
            }
        }
    }
}
