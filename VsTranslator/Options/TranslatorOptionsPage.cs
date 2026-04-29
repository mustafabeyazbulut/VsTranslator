using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace VsTranslator.Options
{
    [Guid("7f3e2a91-4c5d-4e6f-8a9b-1c2d3e4f5a6b")]
    public class TranslatorOptionsPage : DialogPage
    {
        [Category("MyMemory")]
        [DisplayName("E-posta (kota icin)")]
        [Description("Bos birakirsan gunluk 5000 karakter ceviri hakkin olur. " +
                     "Bir e-posta adresi girersen MyMemory bu limiti 50000 karaktere cikarir. " +
                     "Adres dogrulanmaz, sadece tutarli olmasi yeterlidir; ancak gercek " +
                     "adresini kullanmak hizmet sahibinin politikasina uygunlugu acisindan tavsiye edilir.")]
        public string Email { get; set; } = string.Empty;
    }
}
