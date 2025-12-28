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
                var sfTrim = new StringFormat { Alignment = StringAlignment.Near, Trimming = StringTrimming.EllipsisCharacter };
                var sfCenterTrim = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

                // === HEADER (Y = 15-90) - Font 12 & 8 per spec ===
                int logoSize = 70;
                DrawAshokChakra(g, 15, 15, logoSize);
                using (var f = new Font("Times New Roman", 12, FontStyle.Bold))
                    g.DrawString("Ministry of Railways", f, new SolidBrush(RED), logoSize + 25, 18);
                using (var f = new Font("Times New Roman", 8))
                {
                    g.DrawString("Government of India", f, Brushes.Black, logoSize + 25, 40);
                    g.DrawString("No." + (emp.IDCardNumber ?? "251001XXXXXX"), f, Brushes.Black, logoSize + 25, 58);
                }

                // === PHOTO AREA - Spec: centered, with "Employee Photo" label ===
                // Photo box: approximately 3.85cm x 5.8cm = 455 x 685 pixels at 300 DPI
                // But we need to fit within card, so scale proportionally
                int pW = 400, pH = 500;
                int pX = (W - pW) / 2;
                int pY = 100;
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
                else
                {
                    using (var f = new Font("Times New Roman", 10))
                        g.DrawString("Employee Photo", f, Brushes.Gray, new RectangleF(pX, pY, pW, pH), sfCenterTrim);
                }

                // Validity (vertical, left of photo) - Font 8
                if (emp.ValidityDate.HasValue)
                    DrawVerticalText(g, "Valid Upto: " + emp.ValidityDate.Value.ToString("dd/MM/yyyy"), pX - 18, pY + pH - 20, 8);

                // Employee photo label on right side (vertical) - Font 8
                DrawVerticalText(g, "um 5.8 P", pX + pW + 5, pY + pH / 2, 6);

                // === SIGNATURE BOX - Spec: 4.2cm wide = 496 pixels, 0.75cm tall = 89 pixels ===
                int sW = 496, sH = 70;
                int sX = (W - sW) / 2;
                int sY = pY + pH + 15;
                g.FillRectangle(Brushes.White, sX, sY, sW, sH);
                g.DrawRectangle(new Pen(Color.Gray, 1), sX, sY, sW, sH);
                if (!string.IsNullOrEmpty(emp.SignaturePath))
                {
                    var sig = ImageService.LoadImage(emp.SignaturePath);
                    if (sig != null) { g.DrawImage(sig, sX + 5, sY + 3, sW - 10, sH - 6); sig.Dispose(); }
                }

                // === SIGNATURE LABEL - Font 9 ===
                using (var f = new Font("Times New Roman", 9))
                    g.DrawString("Signature of card Holder", f, Brushes.Black, W / 2, sY + sH + 5, sfCenter);

                // === NAME - Font 12 Bold ===
                int nameY = sY + sH + 30;
                string nameText = "Name : " + (emp.Name ?? "XXXXX").ToUpper();
                using (var f = new Font("Times New Roman", 12, FontStyle.Bold))
                    g.DrawString(nameText, f, Brushes.Black, new RectangleF(15, nameY, W - 30, 25), sfTrim);

                // === DESIGNATION - Font 9 ===
                int desigY = nameY + 28;
                string desigText = "Designation: " + (emp.Designation ?? "XXXXX/DDDD");
                using (var f = new Font("Times New Roman", 9))
                    g.DrawString(desigText, f, Brushes.Black, new RectangleF(15, desigY, W / 2, 22), sfTrim);

                // === AUTHORITY SIGNATURE (right side) ===
                int ax = W - 180;
                int ay = desigY - 10;
                g.FillRectangle(Brushes.White, ax, ay, 160, 35);
                g.DrawRectangle(new Pen(Color.Black, 1), ax, ay, 160, 35);
                if (!string.IsNullOrEmpty(emp.AuthoritySignaturePath))
                {
                    var authSig = ImageService.LoadImage(emp.AuthoritySignaturePath);
                    if (authSig != null) { g.DrawImage(authSig, ax + 3, ay + 2, 154, 31); authSig.Dispose(); }
                }
                using (var f = new Font("Times New Roman", 7))
                {
                    g.DrawString("(Signature)", f, Brushes.Black, ax + 50, ay + 38);
                    g.DrawString("Designation of Issuing Authority", f, Brushes.Black, ax + 10, ay + 52);
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
                var sfCenterBoth = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

                // Spec: QR Code and Blood Group boxes are 2.35cm x 2.5cm = 278 x 295 pixels at 300 DPI
                // Scale down to fit card width with margins
                int boxW = 200, boxH = 210;
                int margin = 25;
                int topY = 20;

                // === QR CODE (top left) ===
                g.FillRectangle(Brushes.White, margin, topY, boxW, boxH);
                g.DrawRectangle(new Pen(Color.Black, 1), margin, topY, boxW, boxH);
                using (var f = new Font("Times New Roman", 10))
                    g.DrawString("QR Code", f, Brushes.Gray, new RectangleF(margin, topY, boxW, boxH), sfCenterBoth);
                try
                {
                    var qr = QRCodeGenerator.GenerateEmployeeQRCode(emp, boxW - 10);
                    if (qr != null) { g.DrawImage(qr, margin + 5, topY + 5); qr.Dispose(); }
                }
                catch { }

                // === BLOOD GROUP (top right) ===
                int bx = W - margin - boxW;
                g.FillRectangle(Brushes.White, bx, topY, boxW, boxH);
                g.DrawRectangle(new Pen(Color.Black, 1), bx, topY, boxW, boxH);
                using (var f = new Font("Times New Roman", 48, FontStyle.Bold))
                    g.DrawString(emp.BloodGroup ?? "B+", f, new SolidBrush(RED), new RectangleF(bx, topY, boxW, boxH), sfCenterBoth);

                // === DEPARTMENT BOX - Spec: 4.95cm x 1.5cm = 585 x 177 pixels ===
                // Scale to fit: use full width minus margins
                int deptY = topY + boxH + 15;
                int deptW = W - 2 * margin;
                int deptH = 60;
                g.FillRectangle(Brushes.White, margin, deptY, deptW, deptH);
                g.DrawRectangle(new Pen(Color.Black, 2), margin, deptY, deptW, deptH);
                using (var f = new Font("Times New Roman", 18, FontStyle.Bold))
                    g.DrawString((emp.Department ?? "DEPARTMENT").ToUpper(), f, Brushes.Black, new RectangleF(margin, deptY, deptW, deptH), sfCenterBoth);

                // === DESIGNATION - Font 18 ===
                int desigY = deptY + deptH + 20;
                using (var f = new Font("Times New Roman", 14, FontStyle.Bold))
                    g.DrawString((emp.Designation ?? "DESIGNATION").ToUpper(), f, Brushes.Black, W / 2, desigY, sfCenter);

                // === MOBILE NUMBER - Font 48 (largest per spec) ===
                int mobileY = desigY + 35;
                using (var f = new Font("Times New Roman", 36, FontStyle.Bold))
                    g.DrawString(emp.MobileNumber ?? "9989999999", f, Brushes.Black, W / 2, mobileY, sfCenter);

                // === AADHAAR (masked) - Font 14 ===
                int aadhaarY = mobileY + 55;
                using (var f = new Font("Times New Roman", 12))
                    g.DrawString(emp.GetMaskedAadhaar() ?? "XXXX-XXXX-4545", f, Brushes.Black, W / 2, aadhaarY, sfCenter);

                // === DATE OF ISSUE - Font 8 ===
                int doiY = aadhaarY + 30;
                using (var f = new Font("Times New Roman", 10))
                {
                    string doi = emp.DateOfIssue.HasValue ? "Date of Issue: " + emp.DateOfIssue.Value.ToString("dd-MM-yyyy") : "Date of Issue:";
                    g.DrawString(doi, f, Brushes.Black, W / 2, doiY, sfCenter);
                }

                // === SEPARATOR LINE ===
                int lineY = doiY + 35;
                g.DrawLine(new Pen(Color.Goldenrod, 2), 60, lineY, W - 60, lineY);

                // === INSTRUCTION HEADER - Font 8 Bold ===
                int instrY = lineY + 15;
                using (var f = new Font("Times New Roman", 10, FontStyle.Bold))
                    g.DrawString("Instruction", f, new SolidBrush(RED), W / 2, instrY, sfCenter);

                // === INSTRUCTION TEXT - Font 6 ===
                using (var f = new Font("Times New Roman", 8))
                {
                    g.DrawString("Please surrender to issuing authority on transfer/", f, Brushes.Black, W / 2, instrY + 22, sfCenter);
                    g.DrawString("promotion/completion/termination of Railway service.", f, Brushes.Black, W / 2, instrY + 38, sfCenter);
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
