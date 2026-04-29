using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace VsTranslator.Commands
{
    internal sealed class TranslateCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("2b3c4d5e-6f7a-8b9c-0d1e-2f3a4b5c6d7e");

        private static readonly Guid OutputPaneGuid = new Guid("c4f1e2a3-b5d6-4708-9a1b-2c3d4e5f6a7b");
        private const string OutputPaneName = "VS Translator";

        private readonly AsyncPackage _package;
        private readonly Services.TranslationService _service;

        private TranslateCommand(AsyncPackage package, OleMenuCommandService cmdService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _service = new Services.TranslationService();

            var commandId = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(Execute, commandId);
            cmdService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var cmdService = await package.GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true) as OleMenuCommandService;
            if (cmdService == null)
            {
                throw new InvalidOperationException("IMenuCommandService alinamadi.");
            }
            _ = new TranslateCommand(package, cmdService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _package.JoinableTaskFactory.RunAsync(async delegate
            {
                try
                {
                    await ExecuteAsync();
                }
                catch (OperationCanceledException)
                {
                    await SetStatusAsync("Ceviri iptal edildi.");
                }
                catch (Exception ex)
                {
                    await WriteOutputAsync("HATA: " + ex.Message);
                    await SetStatusAsync("Ceviri basarisiz: " + ex.Message);
                }
            }).FileAndForget("VsTranslator/Execute");
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

            var selected = GetSelectedText();
            if (string.IsNullOrWhiteSpace(selected))
            {
                await SetStatusAsync("Cevrilecek metin secilmedi.");
                return;
            }

            var email = GetEmail();

            await SetStatusAsync("Ceviriliyor...");

            var translated = await _service.TranslateAsync(selected, "en", "tr", email, _package.DisposalToken)
                .ConfigureAwait(true);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);

            TryCopyToClipboard(translated);
            await WriteOutputAsync(
                "[Kaynak]" + Environment.NewLine + selected + Environment.NewLine + Environment.NewLine +
                "[Ceviri]" + Environment.NewLine + translated + Environment.NewLine +
                new string('-', 60));
            await SetStatusAsync("Ceviri tamamlandi (panoya kopyalandi).");
        }

        private string GetSelectedText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;
            var selection = dte?.ActiveDocument?.Selection as TextSelection;
            return selection?.Text;
        }

        private string GetEmail()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var page = _package.GetDialogPage(typeof(Options.TranslatorOptionsPage)) as Options.TranslatorOptionsPage;
            return page?.Email;
        }

        private static void TryCopyToClipboard(string text)
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
            }
            catch
            {
                // Clipboard kilitliyse sessizce gec.
            }
        }

        private async Task WriteOutputAsync(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
            {
                return;
            }

            var paneGuid = OutputPaneGuid;
            IVsOutputWindowPane pane;
            if (outputWindow.GetPane(ref paneGuid, out pane) != VSConstants.S_OK || pane == null)
            {
                outputWindow.CreatePane(ref paneGuid, OutputPaneName, 1, 1);
                outputWindow.GetPane(ref paneGuid, out pane);
            }

            if (pane != null)
            {
                pane.Activate();
                pane.OutputStringThreadSafe(text + Environment.NewLine);
            }
        }

        private async Task SetStatusAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_package.DisposalToken);
            var statusBar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusBar?.SetText(message);
        }
    }
}
