using Basler.Pylon;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bonsai.Pylon
{
    [Description("Acquires a sequence of images from a Basler camera using the pylon software.")]
    public class PylonCapture : Source<PylonDataFrame>
    {
        readonly object captureLock = new object();

        [TypeConverter(typeof(SerialNumberConverter))]
        [Description("The serial number of the camera from which to acquire images.")]
        public string SerialNumber { get; set; }

        [FileNameFilter("Pylon Feature Stream (*.pfs)|*.pfs|All Files (*.*)|*.*")]
        [Editor("Bonsai.Design.OpenFileNameEditor, Bonsai.Design", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        [Description("The name of the file containing the camera configuration parameters.")]
        public string ParameterFile { get; set; }

        [Description("The grab strategy used to acquire images. Images can be processed either by order of arrival (default) or by grabbing the latest frames first (low-latency, but risk frame drop).")]
        public GrabStrategy GrabStrategy { get; set; }

        static void GetImageDepth(PixelType pixelType, out IplDepth depth, out int channels, out PixelType outputFormat)
        {
            switch (pixelType)
            {
                case PixelType.BGR10V1packed:
                case PixelType.BGR10V2packed:
                case PixelType.BGR10packed:
                case PixelType.BGR12packed:
                case PixelType.BGR8packed:
                case PixelType.BGRA8packed:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.BayerBG10:
                case PixelType.BayerBG10pp:
                case PixelType.BayerBG12:
                case PixelType.BayerBG12Packed:
                case PixelType.BayerBG12p:
                case PixelType.BayerBG16:
                case PixelType.BayerBG8:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.BayerGB10:
                case PixelType.BayerGB10pp:
                case PixelType.BayerGB12:
                case PixelType.BayerGB12Packed:
                case PixelType.BayerGB12p:
                case PixelType.BayerGB16:
                case PixelType.BayerGB8:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.BayerGR10:
                case PixelType.BayerGR10pp:
                case PixelType.BayerGR12:
                case PixelType.BayerGR12Packed:
                case PixelType.BayerGR12p:
                case PixelType.BayerGR16:
                case PixelType.BayerGR8:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.BayerRG10:
                case PixelType.BayerRG10pp:
                case PixelType.BayerRG12:
                case PixelType.BayerRG12Packed:
                case PixelType.BayerRG12p:
                case PixelType.BayerRG16:
                case PixelType.BayerRG8:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.Double:
                    outputFormat = PixelType.Double;
                    depth = IplDepth.F64;
                    channels = 1;
                    break;
                case PixelType.Mono10:
                case PixelType.Mono10p:
                case PixelType.Mono10packed:
                case PixelType.Mono12:
                case PixelType.Mono12p:
                case PixelType.Mono12packed:
                case PixelType.Mono16:
                    outputFormat = PixelType.Mono16;
                    depth = IplDepth.U16;
                    channels = 1;
                    break;
                case PixelType.Mono1packed:
                case PixelType.Mono2packed:
                case PixelType.Mono4packed:
                case PixelType.Mono8:
                case PixelType.Mono8signed:
                    outputFormat = PixelType.Mono8;
                    depth = IplDepth.U8;
                    channels = 1;
                    break;
                case PixelType.RGB10packed:
                case PixelType.RGB10planar:
                case PixelType.RGB12V1packed:
                case PixelType.RGB12packed:
                case PixelType.RGB12planar:
                case PixelType.RGB16packed:
                case PixelType.RGB16planar:
                case PixelType.RGB8packed:
                case PixelType.RGB8planar:
                case PixelType.RGBA8packed:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.YUV411packed:
                case PixelType.YUV422_YUYV_Packed:
                case PixelType.YUV422packed:
                case PixelType.YUV444packed:
                    outputFormat = PixelType.BGR8packed;
                    depth = IplDepth.U8;
                    channels = 3;
                    break;
                case PixelType.Undefined:
                default: throw new CaptureException("Undefined pixel type.");
            }
        }

        public override IObservable<PylonDataFrame> Generate()
        {
            return Observable.Create<PylonDataFrame>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        var configFile = ParameterFile;
                        using (var camera = new Camera(SerialNumber))
                        using (var converter = new PixelDataConverter())
                        {
                            camera.Open();
                            if (!string.IsNullOrEmpty(configFile))
                            {
                                camera.Parameters.Load(configFile, ParameterPath.CameraDevice);
                            }

                            try
                            {
                                camera.StreamGrabber.ImageGrabbed += (sender, e) =>
                                {
                                    var result = e.GrabResult;
                                    if (result.IsValid)
                                    {
                                        int channels;
                                        IplDepth depth;
                                        PixelType outputFormat;
                                        var size = new Size(result.Width, result.Height);
                                        GetImageDepth(result.PixelTypeValue, out depth, out channels, out outputFormat);
                                        converter.OutputPixelFormat = outputFormat;
                                        var output = new IplImage(size, depth, channels);
                                        converter.Convert(output.ImageData, output.WidthStep * output.Height, result);
                                        observer.OnNext(new PylonDataFrame(output, result));
                                    }
                                };

                                camera.StreamGrabber.GrabStopped += (sender, e) =>
                                {
                                    if (e.Reason != GrabStopReason.UserRequest)
                                    {
                                        observer.OnError(new CaptureException(e.ErrorMessage));
                                    }
                                };

                                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                                camera.StreamGrabber.Start(GrabStrategy, GrabLoop.ProvidedByStreamGrabber);
                                cancellationToken.WaitHandle.WaitOne();
                            }
                            finally
                            {
                                camera.StreamGrabber.Stop();
                                camera.Close();
                            }
                        }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            });
        }

        class SerialNumberConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                var cameras = from camera in CameraFinder.Enumerate()
                              where camera.ContainsKey(CameraInfoKey.SerialNumber)
                              select camera[CameraInfoKey.SerialNumber];
                return new StandardValuesCollection(cameras.ToArray());
            }
        }
    }
}
