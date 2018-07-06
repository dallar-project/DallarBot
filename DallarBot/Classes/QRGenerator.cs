using System.IO;
using QRCoder;

namespace DallarBot.Classes
{
    public class QRGenerator
    {
        public MemoryStream GenerateQRBitmap(string key)
        {
            var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(key, QRCodeGenerator.ECCLevel.L);
            var code = new QRCode(data);
            var bitmap = code.GetGraphic(5);

            var stream = new MemoryStream();
            bitmap.Save(stream, bitmap.RawFormat);
            stream.Position = 0;
            return stream;
        }
    }
}
