namespace CaptureRepro.Services
{
    using DirectN;
    using Microsoft.UI;
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Windows.Graphics.Capture;
    using Windows.Graphics.DirectX;
    using Windows.Graphics.DirectX.Direct3D11;
    using Windows.Graphics.Display;
    using WinRT;

    internal class CaptureService
    {
        public Task<bool> CapturePrimaryDisplay()
        {
            var displayId = DisplayServices.FindAll().First();
            var captureItem = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);
            return Capture(captureItem);
        }

        private async Task<bool> Capture(GraphicsCaptureItem captureItem)
        {
            using var d3D11Device = D3D11Functions.D3D11CreateDevice(
                null,
                D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT);

            using var dxgiDevice = ComObject.From(d3D11Device.As<IDXGIDevice1>(true));
            using var direct3DDevice = CreateDirect3DDeviceFromDXGIDevice(dxgiDevice);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            using Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                direct3DDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                captureItem.Size);

            framePool.FrameArrived += (s, e) =>
            {
                if (tcs.Task.IsCompleted)
                    return;

                using Direct3D11CaptureFrame frame = framePool.TryGetNextFrame();
                try
                {
                    tcs.SetResult(Process(frame));
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            };

            using GraphicsCaptureSession session = framePool.CreateCaptureSession(captureItem);

            session.StartCapture();

            return await tcs.Task;
        }

        private static bool Process(Direct3D11CaptureFrame frame)
        {
            using IComObject<ID3D11Texture2D> capturedTexture = CreateTexture2D(frame.Surface);

            // Can access description of texture without issue.
            D3D11_TEXTURE2D_DESC description = capturedTexture.Object.GetDesc();
            description.Usage = D3D11_USAGE.D3D11_USAGE_STAGING;
            description.CPUAccessFlags = (uint)D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ;
            description.BindFlags = 0;
            description.MiscFlags = 0;

            // This line throws an InvalidCastException.
            capturedTexture.Object.GetDevice(out var d3D11Device1);

            return true;
        }

        [DllImport(
            "d3d11.dll",
            EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
            SetLastError = true,
            CharSet = CharSet.Unicode,
            ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall
        )]
        private static extern HRESULT CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        public static Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice CreateDirect3DDeviceFromDXGIDevice(IComObject<IDXGIDevice> dxgiDevice)
        {
            IntPtr pDxgiDevice = dxgiDevice.GetInterfacePointer<IDXGIDevice>();

            CreateDirect3D11DeviceFromDXGIDevice(pDxgiDevice, out var pGraphicsDevice).ThrowOnError();

            var direct3DDevice = MarshalInterface<Windows.Graphics.DirectX.Direct3D11.IDirect3DDevice>.FromAbi(pGraphicsDevice);
            Marshal.Release(pGraphicsDevice);
            return direct3DDevice;
        }

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IDirect3DDxgiInterfaceAccess
        {
            ID3D11Texture2D GetInterface([In] ref Guid iid);
        };

        public static IComObject<ID3D11Texture2D> CreateTexture2D(IDirect3DSurface surface)
        {
            IDirect3DDxgiInterfaceAccess access = surface.As<IDirect3DDxgiInterfaceAccess>();
            var texture = access.GetInterface(typeof(ID3D11Texture2D).GUID);
            return new ComObject<ID3D11Texture2D>(texture);
        }
    }
}
