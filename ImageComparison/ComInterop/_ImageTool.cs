using System.Runtime.InteropServices;

namespace XnaFan.ImageComparison.ComInterop;

[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
[Guid("57b01751-f9f0-40b2-b252-947546c52456")]
public interface _ImageTool
{
    float GetPercentageDifference(string image1Path, string image2Path, byte threshold);
}