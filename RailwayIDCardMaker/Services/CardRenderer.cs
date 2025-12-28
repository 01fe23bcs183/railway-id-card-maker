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
                // Set page unit to pixel for consistent rendering at 300 DPI
                g.PageUnit = GraphicsUnit.Pixel;
                SetQuality(g);
                g.Clear(YELLOW);
                g.DrawRectangle(new Pen(Color.Black, 3), 2, 2, W - 5, H - 5);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center };

                // === HEADER (Y = 15-100) ===
                // Logo on left, text on right
                DrawAshokChakra(g, 15, 15, 80);
                using (var f = new Font("Times New Roman", 28, FontStyle.Bold))
                    g.DrawString("Ministry of Railways", f, new SolidBrush(RED), 100, 18);
                using (var f = new Font("Times New Roman", 18))
                {
                    g.DrawString("Government of India", f, Brushes.Black, 100, 52);
                    g.DrawString("No " + (emp.IDCardNumber ?? "XXXXXX"), f, Brushes.Black, 100, 78);
                }

                // === TITLE (Y = 115) ===
                using (var f = new Font("Times New Roman", 24, FontStyle.Bold))
                    g.DrawString("Employee Identity Card", f, new SolidBrush(RED), W / 2, 115, sfCenter);

                // === PHOTO (Y = 160 to 660) ===
                // Photo size: 3.85cm x 4.35cm = 455 x 514 pixels at 300 DPI
                int pY = 165, pW = 380, pH = 450;
                int pX = (W - pW) / 2;
                using (var path = CreateRoundedRect(pX, pY, pW, pH, 12))
                {
                    g.FillPath(Brushes.White, path);
                    g.DrawPath(new Pen(Color.Gray, 2), path);
                }
                if (!string.IsNullOrEmpty(emp.PhotoPath))
                {
                    var img = ImageService.LoadImage(emp.PhotoPath);
                    if (img != null)
                    {
                        using (var clip = CreateRoundedRect(pX + 3, pY + 3, pW - 6, pH - 6, 10))
                        {
                            g.SetClip(clip);
                            g.DrawImage(img, pX + 3, pY + 3, pW - 6, pH - 6);
                            g.ResetClip();
                        }
                        img.Dispose();
                    }
                }

                // Validity (vertical, left of photo)
                if (emp.ValidityDate.HasValue)
                    DrawVerticalText(g, "Valid Upto: " + emp.ValidityDate.Value.ToString("dd-MM-yyyy"), pX - 15, pY + pH - 20, 12);

                // === SIGNATURE BOX (Y = 630-690) ===
                // Signature box: 4.2cm wide = 496 pixels at 300 DPI
                int sY = 635, sW = 380, sH = 50;
                int sX = (W - sW) / 2;
                g.FillRectangle(Brushes.White, sX, sY, sW, sH);
                g.DrawRectangle(new Pen(Color.Gray, 2), sX, sY, sW, sH);
                if (!string.IsNullOrEmpty(emp.SignaturePath))
                {
                    var sig = ImageService.LoadImage(emp.SignaturePath);
                    if (sig != null) { g.DrawImage(sig, sX + 5, sY + 3, sW - 10, sH - 6); sig.Dispose(); }
                }

                // === SIGNATURE LABEL (Y = 695) ===
                using (var f = new Font("Times New Roman", 14))
                    g.DrawString("Signature of Card Holder", f, Brushes.Black, W / 2, 695, sfCenter);

                // === NAME (Y = 740) ===
                using (var f = new Font("Times New Roman", 26, FontStyle.Bold))
                    g.DrawString("Name : " + (emp.Name ?? "XXXXX").ToUpper(), f, Brushes.Black, 20, 740);

                // === DESIGNATION (Y = 790) ===
                using (var f = new Font("Times New Roman", 20))
                    g.DrawString("Designation: " + (emp.Designation ?? "XXXXX"), f, Brushes.Black, 20, 790);

                // === AUTHORITY (right side, Y = 840-1000) ===
                int ax = W - 180;
                g.FillRectangle(Brushes.White, ax, 850, 160, 40);
                g.DrawRectangle(new Pen(Color.Black, 2), ax, 850, 160, 40);
                if (!string.IsNullOrEmpty(emp.AuthoritySignaturePath))
                {
                    var authSig = ImageService.LoadImage(emp.AuthoritySignaturePath);
                    if (authSig != null) { g.DrawImage(authSig, ax + 5, 853, 150, 34); authSig.Dispose(); }
                }
                using (var f = new Font("Times New Roman", 12))
                {
                    g.DrawString("(Signature)", f, Brushes.Black, ax + 45, 895);
                    g.DrawString("Issuing Authority", f, Brushes.Black, ax + 30, 915);
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
                // Set page unit to pixel for consistent rendering at 300 DPI
                g.PageUnit = GraphicsUnit.Pixel;
                SetQuality(g);
                g.Clear(YELLOW);
                g.DrawRectangle(new Pen(Color.Black, 3), 2, 2, W - 5, H - 5);

                var sfCenter = new StringFormat { Alignment = StringAlignment.Center };
                var sfCenterBoth = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                // QR Code size: 2.35cm = 278 pixels at 300 DPI
                int qrSz = 200;

                // === QR CODE (Y = 20) ===
                g.FillRectangle(Brushes.White, 25, 25, qrSz, qrSz);
                g.DrawRectangle(new Pen(Color.Black, 2), 25, 25, qrSz, qrSz);
                try
                {
                    var qr = QRCodeGenerator.GenerateEmployeeQRCode(emp, qrSz - 10);
                    if (qr != null) { g.DrawImage(qr, 30, 30); qr.Dispose(); }
                }
                catch { }

                // === BLOOD GROUP (right, Y = 25) ===
                int bx = W - 25 - qrSz;
                g.FillRectangle(Brushes.White, bx, 25, qrSz, qrSz);
                g.DrawRectangle(new Pen(Color.Black, 2), bx, 25, qrSz, qrSz);
                using (var f = new Font("Times New Roman", 72, FontStyle.Bold))
                    g.DrawString(emp.BloodGroup ?? "O+", f, new SolidBrush(RED), new RectangleF(bx, 25, qrSz, qrSz), sfCenterBoth);

                // === DEPARTMENT (Y = 250) ===
                // Department box: 4.95cm = 585 pixels at 300 DPI
                int dW = W - 50;
                g.FillRectangle(Brushes.White, 25, 250, dW, 70);
                g.DrawRectangle(new Pen(Color.Black, 3), 25, 250, dW, 70);
                using (var f = new Font("Times New Roman", 28, FontStyle.Bold))
                    g.DrawString((emp.Department ?? "DEPARTMENT").ToUpper(), f, Brushes.Black, new RectangleF(25, 250, dW, 70), sfCenterBoth);

                // === DESIGNATION (Y = 350) ===
                using (var f = new Font("Times New Roman", 22, FontStyle.Bold))
                    g.DrawString((emp.Designation ?? "DESIGNATION").ToUpper(), f, Brushes.Black, W / 2, 350, sfCenter);

                // === MOBILE (Y = 410) ===
                using (var f = new Font("Times New Roman", 36, FontStyle.Bold))
                    g.DrawString(emp.MobileNumber ?? "9999999999", f, Brushes.Black, W / 2, 410, sfCenter);

                // === AADHAAR (Y = 480) ===
                using (var f = new Font("Times New Roman", 22))
                    g.DrawString(emp.GetMaskedAadhaar() ?? "XXXX-XXXX-1234", f, Brushes.Black, W / 2, 480, sfCenter);

                // === DATE OF ISSUE (Y = 540) ===
                using (var f = new Font("Times New Roman", 18))
                {
                    string doi = emp.DateOfIssue.HasValue ? "Date of Issue: " + emp.DateOfIssue.Value.ToString("dd-MM-yyyy") : "";
                    g.DrawString(doi, f, Brushes.Black, W / 2, 540, sfCenter);
                }

                // === LINE (Y = 600) ===
                g.DrawLine(new Pen(Color.Goldenrod, 3), 80, 600, W - 80, 600);

                // === INSTRUCTION (Y = 640) ===
                using (var f = new Font("Times New Roman", 20, FontStyle.Bold))
                    g.DrawString("Instruction", f, new SolidBrush(RED), W / 2, 640, sfCenter);

                // === INSTRUCTION TEXT (Y = 700, 760, 820) ===
                using (var f = new Font("Times New Roman", 14))
                {
                    g.DrawString("Please surrender to issuing Authority on", f, Brushes.Black, W / 2, 700, sfCenter);
                    g.DrawString("transfer/promotion/completion/termination", f, Brushes.Black, W / 2, 750, sfCenter);
                    g.DrawString("of Railway service.", f, Brushes.Black, W / 2, 800, sfCenter);
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
