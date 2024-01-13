using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Threading;
using System.Threading;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Current {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            await Task.Factory.StartNew(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Task Factory {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            }, TaskCreationOptions.LongRunning);
            System.Diagnostics.Debug.WriteLine($"After Task Factory {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            await Task.Factory.StartNew(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Task Factory Await(false) {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"After Task Factory Await(false) {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");

            var scheduler = TaskScheduler.Current;

            // await Avalonia.Threading.Dispatcher.UIThread.AwaitWithPriority(LocalTaks(), Avalonia.Threading.DispatcherPriority.Send);

            await Avalonia.Threading.Dispatcher.UIThread.AwaitWithPriority(Avalonia.Threading.DispatcherPriority.Background);

            System.Diagnostics.Debug.WriteLine($"After AwaitWithPriority {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            System.Diagnostics.Debug.WriteLine($"Priority {((AvaloniaSynchronizationContext)SynchronizationContext.Current!).Priority}");

            await Avalonia.Threading.Dispatcher.UIThread.AwaitWithPriority(Avalonia.Threading.DispatcherPriority.Render);

            System.Diagnostics.Debug.WriteLine($"After AwaitWithPriority {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            System.Diagnostics.Debug.WriteLine($"Priority {((AvaloniaSynchronizationContext)SynchronizationContext.Current!).Priority}");

            await Avalonia.Threading.Dispatcher.UIThread.AwaitWithPriority(Avalonia.Threading.DispatcherPriority.Input);

            System.Diagnostics.Debug.WriteLine($"After AwaitWithPriority {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
            System.Diagnostics.Debug.WriteLine($"Priority {((AvaloniaSynchronizationContext)SynchronizationContext.Current!).Priority}");

            await TaskScheduler.Default;

            System.Diagnostics.Debug.WriteLine($"after await TaskScheduler.Default {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");

            await StaTaskScheduler.Default;

            System.Diagnostics.Debug.WriteLine($"after aStaTaskScheduler.Default {System.Threading.Thread.CurrentThread.ManagedThreadId} - UI: {Avalonia.Threading.Dispatcher.UIThread.CheckAccess()} AS {System.Threading.Thread.CurrentThread.GetApartmentState()}");
        }
    }
}
