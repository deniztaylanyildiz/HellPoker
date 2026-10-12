namespace HellPoker.Presentation.Ui
{
    /// <summary>The bug report / idea box (Demo 2.0's tester feedback), in words.</summary>
    internal static partial class UiText
    {
        public static string FeedbackButton => L("BUG / IDEA", "HATA / ÖNERİ");
        public static string FeedbackTitle => L("A BUG, OR AN IDEA?", "HATA MI, ÖNERİ Mİ?");
        public static string FeedbackBug => L("BUG", "HATA");
        public static string FeedbackIdea => L("IDEA", "ÖNERİ");
        public static string FeedbackPlaceholder => L("What happened? What were you doing? (a few lines are plenty)",
            "Ne oldu? Ne yapıyordun? (birkaç satır yeter)");
        public static string FeedbackSend => L("SEND", "GÖNDER");
        public static string FeedbackEmpty => L("Write a few words first.", "Önce birkaç kelime yaz.");

        /// <summary>{0} the address.</summary>
        public static string FeedbackAddressFormat => L("It goes to {0} with your run's last lines.", "Koşunun son satırlarıyla {0} adresine gider.");

        /// <summary>{0} the address.</summary>
        public static string FeedbackSentFormat => L("Your mail app opens. It is also on the clipboard: {0}", "E-posta uygulaman açılıyor. Panoda da var: {0}");

        /// <summary>{0} the address.</summary>
        public static string FeedbackCopiedFormat => L("Copied to the clipboard: paste it into a mail to {0}", "Panoya kopyalandı: {0} adresine bir e-postayla yapıştır");
        public static string FeedbackLogOnClipboard => L("(The run's last lines are on the clipboard too.)", "(Koşunun son satırları panoda.)");
    }
}