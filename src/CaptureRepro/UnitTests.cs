namespace CaptureRepro
{
    using CaptureRepro.Services;
    using CommunityToolkit.WinUI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Threading.Tasks;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task GetDeviceRepro()
        {
            await UnitTestApp.DispatcherQueue.EnqueueAsync(async () =>
            {
                // Arrange
                var captureService = new CaptureService();

                // Act
                // Assert
                await Assert.ThrowsExceptionAsync<InvalidCastException>(captureService.CapturePrimaryDisplay);
            });
        }
    }
}
