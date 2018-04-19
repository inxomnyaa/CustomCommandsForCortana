using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Inhaltsdialogfeld" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace CustomCommandsForCortana.Dialogs
{

    public sealed partial class EditDialog : ContentDialog
    {
        public string[] result = { };

        public EditDialog(string title, string[] contents)
        {
            this.InitializeComponent();
            this.Title = title;
            this.result = contents;
            this.NameTextBox.Text = this.result[0];
            this.ExampleTextBox.Text = this.result[1];
            this.ListenForTextBox.Text = this.result[2];
            this.FeedbackTextBox.Text = this.result[3];
            this.BatchCommandTextBox.Text = this.result[4];
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            result = new string[] { NameTextBox.Text, ExampleTextBox.Text, ListenForTextBox.Text, FeedbackTextBox.Text, BatchCommandTextBox.Text };
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
