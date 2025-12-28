using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using RailwayIDCardMaker.Models;
using RailwayIDCardMaker.Utils;

namespace RailwayIDCardMaker.Services
{
    public static class CardRenderer
    {
        private const int W = 638;
        private const int H = 1028;

        private static readonly Color YELLOW = Color.FromArgb(255, 255, 0);
        private static readonly Color RED = Color.FromArgb(180, 0, 0);

        public static Bitmap RenderCardFront(Employee emp, Image logo = null)
        {
            var bmp = new Bitmap(W, H);
            bmp.SetResolution(300, 300);

            using (var g = Graphics.FromImage(bmp))
            {
                SetQuality(g);
                g.Clear(YELLOW);
                g.DrawRectangle(new Pen(Color.Black, 2), 1, 1, W - 3, H - 3);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center };

                // === HEADER ===
                // Y=8: Ministry of Railways
                // Y=24: Government of India  
                // Y=38: No XXXXXX
                DrawAshokChakra(g, 8, 8, 50);
                using (var f = new Font("Times New Roman", 9, FontStyle.Bold))
                    g.DrawString("Ministry of Railways", f, new SolidBrush(RED), 62, 8);
                using (var f = new Font("Times New Roman", 5))
                {
                    g.DrawString("Government of India", f, Brushes.Black, 62, 24);
                    g.DrawString("No " + (emp.IDCardNumber ?? "XXXXXX"), f, Brushes.Black, 62, 36);
                }

                // === TITLE (Y = 55) ===
                using (var f = new Font("Times New Roman", 8, FontStyle.Bold))
                    g.DrawString("Employee Identity Card", f, new SolidBrush(RED), W / 2, 55, sfCenter);

                // === PHOTO (Y = 75 to 380) ===
                int pY = 75, pW = 280, pH = 300;
                int pX = (W - pW) / 2;
                using (var path = CreateRoundedRect(pX, pY, pW, pH, 8))
                {
                    g.FillPath(Brushes.White, path);
                    g.DrawPath(new Pen(Color.Gray, 1), path);
                }
                if (!string.IsNullOrEmpty(emp.PhotoPath))
                {
                    var img = ImageService.LoadImage(emp.PhotoPath);
                    if (img != null)
                    {
                        using (var clip = CreateRoundedRect(pX + 2, pY + 2, pW - 4, pH - 4, 6))
                        {
                            g.SetClip(clip);
                            g.DrawImage(img, pX + 2, pY + 2, pW - 4, pH - 4);
                            g.ResetClip();
                        }
                        img.Dispose();
                    }
                }

                // Validity (vertical, left of photo)
                if (emp.ValidityDate.HasValue)
                    DrawVerticalText(g, "Valid Upto: " + emp.ValidityDate.Value.ToString("dd-MM-yyyy"), pX - 8, pY + pH - 10, 4);

                // === SIGNATURE BOX (Y = 385-415) ===
                int sY = 385, sW = 280, sH = 30;
                int sX = (W - sW) / 2;
                g.FillRectangle(Brushes.White, sX, sY, sW, sH);
                g.DrawRectangle(new Pen(Color.Gray, 1), sX, sY, sW, sH);
                if (!string.IsNullOrEmpty(emp.SignaturePath))
                {
                    var sig = ImageService.LoadImage(emp.SignaturePath);
                    if (sig != null) { g.DrawImage(sig, sX + 3, sY + 2, sW - 6, sH - 4); sig.Dispose(); }
                }

                // === SIGNATURE LABEL (Y = 420) ===
                using (var f = new Font("Times New Roman", 5))
                    g.DrawString("Signature of Card Holder", f, Brushes.Black, W / 2, 420, sfCenter);

                // === NAME (Y = 438) ===
                using (var f = new Font("Times New Roman", 8, FontStyle.Bold))
                    g.DrawString("Name : " + (emp.Name ?? "XXXXX").ToUpper(), f, Brushes.Black, 15, 438);

                // === DESIGNATION (Y = 458) ===
                using (var f = new Font("Times New Roman", 6))
                    g.DrawString("Designation: " + (emp.Designation ?? "XXXXX"), f, Brushes.Black, 15, 458);

                // === AUTHORITY (right side, Y = 440-475) ===
                int ax = W - 115;
                g.DrawRectangle(new Pen(Color.Black, 1), ax, 440, 100, 20);
                using (var f = new Font("Times New Roman", 4))
                {
                    g.DrawString("(Signature)", f, Brushes.Black, ax + 32, 462);
                    g.DrawString("Issuing Authority", f, Brushes.Black, ax + 22, 472);
                }
            }
            return bmp;
        }

        public static Bitmap RenderCardBack(Employee emp)
        {
            var bmp = new Bitmap(W, H);
            bmp.SetResolution(300, 300);

            using (var g = Graphics.FromImage(bmp))
            {
                SetQuality(g);
                g.Clear(YELLOW);
                g.DrawRectangle(new Pen(Color.Black, 2), 1, 1, W - 3, H - 3);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                var sfCenterBoth = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                int qrSz = 70;

                // === QR CODE (Y = 10) ===
                g.FillRectangle(Brushes.White, 15, 10, qrSz, qrSz);
                g.DrawRectangle(new Pen(Color.Black, 1), 15, 10, qrSz, qrSz);
                try
                {
                    var qr = QRCodeGenerator.GenerateEmployeeQRCode(emp, qrSz - 6);
                    if (qr != null) { g.DrawImage(qr, 18, 13); qr.Dispose(); }
                }
                catch { }

                // === BLOOD GROUP (right, Y = 10) ===
                int bx = W - 15 - qrSz;
                g.FillRectangle(Brushes.White, bx, 10, qrSz, qrSz);
                g.DrawRectangle(new Pen(Color.Black, 1), bx, 10, qrSz, qrSz);
                using (var f = new Font("Times New Roman", 22, FontStyle.Bold))
                    g.DrawString(emp.BloodGroup ?? "O+", f, new SolidBrush(RED), new RectangleF(bx, 10, qrSz, qrSz), sfCenterBoth);

                // === DEPARTMENT (Y = 90) ===
                int dW = W - 30;
                g.FillRectangle(Brushes.White, 15, 90, dW, 25);
                g.DrawRectangle(new Pen(Color.Black, 2), 15, 90, dW, 25);
                using (var f = new Font("Times New Roman", 8, FontStyle.Bold))
                    g.DrawString((emp.Department ?? "DEPARTMENT").ToUpper(), f, Brushes.Black, new RectangleF(15, 90, dW, 25), sfCenterBoth);

                // === DESIGNATION (Y = 125) ===
                using (var f = new Font("Times New Roman", 7, FontStyle.Bold))
                    g.DrawString((emp.Designation ?? "DESIGNATION").ToUpper(), f, Brushes.Black, W / 2, 125, sfCenter);

                // === MOBILE (Y = 145) ===
                using (var f = new Font("Times New Roman", 8, FontStyle.Bold))
                    g.DrawString(emp.MobileNumber ?? "9999999999", f, Brushes.Black, W / 2, 145, sfCenter);

                // === AADHAAR (Y = 165) ===
                using (var f = new Font("Times New Roman", 6))
                    g.DrawString(emp.GetMaskedAadhaar() ?? "XXXX-XXXX-1234", f, Brushes.Black, W / 2, 165, sfCenter);

                // === DATE OF ISSUE (Y = 183) ===
                using (var f = new Font("Times New Roman", 5))
                {
                    string doi = emp.DateOfIssue.HasValue ? "Date of Issue: " + emp.DateOfIssue.Value.ToString("dd-MM-yyyy") : "";
                    g.DrawString(doi, f, Brushes.Black, W / 2, 183, sfCenter);
                }

                // === LINE (Y = 200) ===
                g.DrawLine(new Pen(Color.Goldenrod, 2), 50, 200, W - 50, 200);

                // === INSTRUCTION (Y = 212) ===
                using (var f = new Font("Times New Roman", 6, FontStyle.Bold))
                    g.DrawString("Instruction", f, new SolidBrush(RED), W / 2, 212, sfCenter);

                // === INSTRUCTION TEXT (Y = 228, 242, 256) ===
                using (var f = new Font("Times New Roman", 4))
                {
                    g.DrawString("Please surrender to issuing Authority on", f, Brushes.Black, W / 2, 228, sfCenter);
                    g.DrawString("transfer/promotion/completion/termination", f, Brushes.Black, W / 2, 242, sfCenter);
                    g.DrawString("of Railway service.", f, Brushes.Black, W / 2, 256, sfCenter);
                }
            }
            return bmp;
        }

        #region Helpers

        private static void SetQuality(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        }

        private static void DrawAshokChakra(Graphics g, int x, int y, int sz)
        {
            using (var p = new Pen(Color.DarkRed, 2)) g.DrawEllipse(p, x, y, sz, sz);
            using (var p = new Pen(Color.DarkRed, 1)) g.DrawEllipse(p, x + 3, y + 3, sz - 6, sz - 6);
            int cx = x + sz / 2, cy = y + sz / 2;
            using (var p = new Pen(Color.DarkRed, 1))
            {
                for (int i = 0; i < 24; i++)
                {
                    double a = i * Math.PI / 12;
                    g.DrawLine(p, cx + (int)(sz / 8 * Math.Cos(a)), cy + (int)(sz / 8 * Math.Sin(a)),
                                  cx + (int)((sz / 2 - 4) * Math.Cos(a)), cy + (int)((sz / 2 - 4) * Math.Sin(a)));
                }
            }
            using (var b = new SolidBrush(Color.DarkRed))
                g.FillEllipse(b, cx - sz / 8, cy - sz / 8, sz / 4, sz / 4);
        }

        private static void DrawVerticalText(Graphics g, string text, int x, int y, int fontSize)
        {
            var st = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(-90);
            using (var f = new Font("Times New Roman", fontSize))
                g.DrawString(text, f, Brushes.Black, 0, 0);
            g.Restore(st);
        }

        private static GraphicsPath CreateRoundedRect(int x, int y, int w, int h, int r)
        {
            var p = new GraphicsPath();
            int d = r * 2;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        #endregion

        #region Public API

        public static Bitmap RenderCardPreview(Employee emp, Image logo = null, bool both = true)
        {
            using (var front = RenderCardFront(emp, logo))
            {
                if (!both) return new Bitmap(front);
                using (var back = RenderCardBack(emp))
                {
                    var combined = new Bitmap(W * 2 + 30, H + 25);
                    combined.SetResolution(300, 300);
                    using (var g = Graphics.FromImage(combined))
                    {
                        g.Clear(Color.White);
                        g.DrawImage(front, 0, 0);
                        g.DrawImage(back, W + 30, 0);
                        using (var f = new Font("Arial", 7, FontStyle.Bold))
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center };
                            g.DrawString("FRONT", f, Brushes.Black, W / 2, H + 3, sf);
                            g.DrawString("BACK", f, Brushes.Black, W + 30 + W / 2, H + 3, sf);
                        }
                    }
                    return combined;
                }
            }
        }

        public static Bitmap GetScaledPreview(Bitmap orig, float scale)
        {
            int nw = (int)(orig.Width * scale), nh = (int)(orig.Height * scale);
            var scaled = new Bitmap(nw, nh);
            using (var g = Graphics.FromImage(scaled))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(orig, 0, 0, nw, nh);
            }
            return scaled;
        }

        public static void ExportToImage(Employee emp, string frontPath, string backPath, Image logo = null)
        {
            using (var f = RenderCardFront(emp, logo)) f.Save(frontPath, System.Drawing.Imaging.ImageFormat.Png);
            using (var b = RenderCardBack(emp)) b.Save(backPath, System.Drawing.Imaging.ImageFormat.Png);
        }

        #endregion
    }
}
