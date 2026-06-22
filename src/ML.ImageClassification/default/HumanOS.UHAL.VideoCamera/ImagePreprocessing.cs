/*****************************************************************************
 * Copyright (C) by CyberTech Engineering 2026 – www.cybertech.swiss         *
 *****************************************************************************
 * Project: HumanOS (R)
 * Date   : 2026
 *****************************************************************************
 * License:                                                                  *
 *   This library is protected software; you are not allowed to redistribute *
 *   whole or part of it to other companies or external persons without the  *
 *   authorization of the CEO CyberTech Engineering GmbH.                    *
 *****************************************************************************/

using HumanOS.Kernel.Processing;
using SkiaSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HumanOS.IoT.Designer.Library.Scripts
{
  /// <summary>
  /// Script to convert the raw JPEG webcam frame into the input tensor expected
  /// by the ResNet-18 ONNX model. The model input 'x' has the shape
  /// [1, 3, TargetHeight, TargetWidth] (NCHW, RGB) and contains no built-in
  /// normalization, so the frame is decoded, resized and normalized with the
  /// standard ImageNet mean/standard-deviation here. The flattened layout is
  /// channel-major (all R, then all G, then all B), row-major within a channel,
  /// which ML.NET reshapes back into the [1, 3, H, W] tensor.
  /// </summary>
  public class TImagePreprocessing : TAbstractProcessingScriptObject
  {
    ///<see cref="TAbstractProcessingScriptObject"/>
    public override void process(IProcessingNode Processor)
    {
      byte[] naui8RawImage = Processor.getProperty<byte[]>("ImageInputPort");
      if (naui8RawImage != null && naui8RawImage.Length > 0)
      {
        int iTargetWidth = Processor.getProperty<int>("TargetWidth", 224);
        int iTargetHeight = Processor.getProperty<int>("TargetHeight", 224);
        int iPlanes = Processor.getProperty<int>("Planes", 3);
        if (iTargetWidth <= 0)
        {
          iTargetWidth = 224;
        }
        if (iTargetHeight <= 0)
        {
          iTargetHeight = 224;
        }
        if (iPlanes <= 0)
        {
          iPlanes = 3;
        }

        float[] af32Vector = convertImageToTensor(naui8RawImage, iPlanes, iTargetWidth, iTargetHeight);
        Processor.setProperty<float[]>("ImageVectorOutputPort", af32Vector);
      }
    }


    private static float[] convertImageToTensor(byte[] aui8RawImage, int iPlanes, int iTargetWidth, int iTargetHeight)
    {
      int iPlaneSize = iTargetWidth * iTargetHeight;
      float[] af32Vector = new float[iPlanes * iPlaneSize];

      // 1. Bild plattformunabhängig dekodieren
      using (SKBitmap sourceBitmap = SKBitmap.Decode(aui8RawImage))
      {
        if (sourceBitmap == null)
        {
          throw new ArgumentException("Das bereitgestellte Byte-Array enthält kein gültiges Bild.");
        }

        // 2. Ziel-Layout definieren (Rgb888x erzwingt 4 Bytes pro Pixel)
        SKImageInfo targetInfo = new SKImageInfo(iTargetWidth, iTargetHeight, SKColorType.Rgb888x, SKAlphaType.Opaque);
        
        using (SKBitmap resizedBitmap = new SKBitmap(targetInfo))
        {
          // 3. Bild skalieren mit hoher Qualität
          sourceBitmap.ScalePixels(resizedBitmap, SKFilterQuality.High);

          // 4. Pixel als sicheren Lese-Span abgreifen (Kein unsafe / kein Zeiger nötig)
          ReadOnlySpan<byte> pixelSpan = resizedBitmap.GetPixelSpan();
          int rowBytes = resizedBitmap.RowBytes;

          for (int iRow = 0; iRow < iTargetHeight; iRow++)
          {
            for (int iColumn = 0; iColumn < iTargetWidth; iColumn++)
            {
              // Im Rgb888x-Modus hat jeder Pixel exakt 4 Byte (R, G, B, Ignoriert)
              int iPixelOffset = iRow * rowBytes + iColumn * 4;
              
              byte ui8Red   = pixelSpan[iPixelOffset];
              byte ui8Green = pixelSpan[iPixelOffset + 1];
              byte ui8Blue  = pixelSpan[iPixelOffset + 2];

              int iChannelIndex = iRow * iTargetWidth + iColumn;

              // 5. CHW-Tensor-Layout befüllen und normalisieren
              af32Vector[iChannelIndex] = (ui8Red / 255f - MeanRed) / StdRed;
              af32Vector[iPlaneSize + iChannelIndex] = (ui8Green / 255f - MeanGreen) / StdGreen;
              af32Vector[2 * iPlaneSize + iChannelIndex] = (ui8Blue / 255f - MeanBlue) / StdBlue;
            }
          }
        }
      }

      return af32Vector;
    }

    // ImageNet normalization constants (RGB) used by the timm ResNet-18 export
    private const float MeanRed = 0.485f;
    private const float MeanGreen = 0.456f;
    private const float MeanBlue = 0.406f;
    private const float StdRed = 0.229f;
    private const float StdGreen = 0.224f;
    private const float StdBlue = 0.225f;
  }
}
