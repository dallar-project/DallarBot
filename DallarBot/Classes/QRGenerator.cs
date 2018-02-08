using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.DrawingCore;
using QRCoder;

namespace DallarBot.Classes
{
    public class QRGenerator
    {
        public MemoryStream GenerateQRBitmap(string key)
        {
            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(key, QRCodeGenerator.ECCLevel.Q);
            var code = new QRCode(data);
            var bitmap = code.GetGraphic(20);

            var stream = new MemoryStream();
            bitmap.Save(stream, bitmap.RawFormat);
            stream.Position = 0;
            return stream;
        }
    }
}
