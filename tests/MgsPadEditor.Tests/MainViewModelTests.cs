using System;
using System.IO;
using System.Threading;
using MgsPadEditor.ViewModels;
using Xunit;

namespace MgsPadEditor.Tests
{
    public class MainViewModelTests
    {
        [Fact]
        public void ViewModel_LoadsDixiIni_AndPopulatesRows()
        {
            string realDixi = @"o:\Desarrollos\MGS PAD Editor\dixi.ini";
            Assert.True(File.Exists(realDixi), $"dixi.ini not found at {realDixi}");

            Exception? threadEx = null;
            var t = new Thread(() =>
            {
                try
                {
                    var vm = new MainViewModel();
                    vm.LoadFile(realDixi);

                    Assert.NotEmpty(vm.Profiles);
                    Assert.NotNull(vm.SelectedProfile);
                    Assert.NotEmpty(vm.ControllerRows);
                    Assert.NotEmpty(vm.KeyboardMouseRows);

                    // Test filtering
                    vm.SearchFilter = "Disparo";
                    Assert.Contains(vm.ControllerRows, r => r.IsVisible);

                    vm.SearchFilter = "NONEXISTENT_QUERY_XYZ";
                    Assert.DoesNotContain(vm.ControllerRows, r => r.IsVisible);

                    // Reset filter
                    vm.SearchFilter = "";
                    Assert.All(vm.ControllerRows, r => Assert.True(r.IsVisible));
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (threadEx != null)
            {
                throw new AggregateException(threadEx);
            }
        }
    }
}
