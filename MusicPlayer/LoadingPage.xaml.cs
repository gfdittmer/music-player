using MusicPlayer.Common;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoadingPage : Page, IProgress<InitializeLibraryTaskProgress>
    {
        public LoadingPage()
        {
            this.InitializeComponent();
            this.Loaded += LoadingPage_Loaded;
        }

        void LoadingPage_Loaded(object sender, RoutedEventArgs e)
        {
            LibraryManager.InitializeLibrary(this);
        }

        public void Report(InitializeLibraryTaskProgress value)
        {
            this.message.Text = value.Message;
            this.progress.Value = value.Progress;
        }
    }
}
