---
id: usecase-ml-image-classification
title: "Use Case: ML Image Classification"
subject: "Real-Time Webcam Image Classification with an ONNX Neural Network and the HumanOS Machine Learning Plugin"
keywords: [HumanOS, machine learning, ML, image classification, neural network, ONNX, ResNet-18, ImageNet, webcam, video camera, processing network, OPC-UA, VideoCamera, PeMiL]
---

# ML Image Classification

Shows how to run a **machine learning image-classification model** directly on the gateway: frames captured from a USB webcam are preprocessed, fed through a **ResNet-18 ONNX neural network** and the predicted class is published as a human-readable label over OPC-UA.

The use case is built entirely from a **processing network** inside a single device template — no external inference service is required. Image preprocessing and label selection are implemented as small C# script processors, while the inference step is handled by the HumanOS **Machine Learning plugin** (`HumanOS.PeMiL.MachineLearning`) running the ONNX model on CPU or GPU.

## Architecture

```text
   USB Webcam                         HumanOS IoT Runtime (Gateway)                          OPC-UA client
┌──────────────┐    JPEG frames   ┌──────────────────────────────────────────────────┐   ┌───────────────┐
│              │── VideoStream ──▶│  ImagePreprocessing  (C# script)                   │   │               │
│  Video Cam   │    (Byte[])      │    decode → resize 224×224 → ImageNet normalize     │   │  reads         │
│  (PnP/USB)   │                  │              │ Single[] (NCHW tensor)               │   │  Detected      │
└──────────────┘                  │              ▼                                      │   │  Classification│
                                  │  ImageClassificationProcessor (ML plugin / ONNX)    │◀──│  via OPC-UA    │
                                  │    ResNet-18 → class score vector                   │   │  (UAExpert,…)  │
                                  │              │ Single[] (logits)                    │   │               │
                                  │              ▼                                      │   └───────────────┘
                                  │  ClassificationSelectionProcessor (C# script)       │
                                  │    argmax + threshold → ImageNet label              │
                                  │              │ String                               │
                                  │              ▼                                      │
                                  │  DetectedClassification ── OPC-UA Server (4840) ────┼──▶
                                  └──────────────────────────────────────────────────┘
```

The three processors are wired together inside the device template's *Blank Processing Network* using
**port matching** (`PortMatchId`), so the camera stream, the normalized tensor and the classification
vector flow from one processor to the next without explicit node wiring.

## Project Layout

| Path                                                  | Purpose                                                                                  |
| :---------------------------------------------------- | :--------------------------------------------------------------------------------------- |
| `ImageClassification.h2proj`                          | HumanOS IoT Designer project file (targets, plugins, device instances)                   |
| `DeviceTemplates/WebCam_v1.json`                      | Device template: data nodes, Start/Stop commands and the image-classification network    |
| `default/`                                            | Default IoT Gateway target                                                               |
| `default/Devices/WebCam.json`                         | Concrete webcam device instance (PnP/USB address)                                        |
| `default/HumanOS.UHAL.VideoCamera/`                   | Video camera plugin config, the two C# script processors, the ONNX model and `labels.txt`|
| `default/HumanOS.PeSeL.OPCUAServer/settings.json`     | OPC-UA server (`opc.tcp://localhost:4840/`) exposing the device nodes                    |
| `default/HumanOS.UHAL.DeviceDetectors/`               | Device-detector plugin (enabled) used to enumerate the camera                            |
| `Build/`                                              | Deployment-ready, published artifact for the `default` target (incl. service scripts)    |

## Device Template (`WebCam_v1`)

The template exposes the camera and the classification result, plus commands to control capture:

| Node / Command          | Type        | Direction | Purpose                                                            |
| :---------------------- | :---------- | :-------- | :----------------------------------------------------------------- |
| `VideoStream`           | `Byte[]`    | read      | Raw JPEG frame from the camera (`RawImageType` port)               |
| `DetectedClassification`| `String`    | read      | The predicted ImageNet label (`ClassificationType` port)           |
| `Start`                 | command     | —         | Start video capture (`Video.Start`)                                |
| `Stop`                  | command     | —         | Stop video capture (`Video.Stop`)                                  |

### Processing Network

The *Blank Processing Network* contains three processors connected by two links
(`ImageVectorLink`, `ClassificationVectorLink`):

| Processor                          | Type                       | Role                                                                                              |
| :--------------------------------- | :------------------------- | :------------------------------------------------------------------------------------------------ |
| `ImagePreprocessing`               | `CSharpScriptProcessingNode` | Decodes the JPEG frame, resizes to 224×224 and applies ImageNet mean/std normalization into an NCHW `Single[]` tensor. Sampled at `SamplingRate` 0.2 to throttle the inference rate. |
| `ImageClassificationProcessor`     | `EventProcessingProxyNode` (ML plugin) | Runs the ResNet-18 ONNX model (`TNeuralNetworkProcessor`) and emits the class-score vector (logits). |
| `ClassificationSelectionProcessor` | `CSharpScriptProcessingNode` | Takes the argmax of the score vector; if it exceeds `ThresholdLevel` (0.2) maps the class index to a label from `labels.txt`, otherwise reports `unknown`. |

Key processor properties (configurable in the Designer):

- **`ImagePreprocessing`** — `TargetWidth`/`TargetHeight` (224), `Planes` (3); the ImageNet
  mean/std constants are defined in `ImagePreprocessing.cs`.
- **`ImageClassificationProcessor`** — `ModelFileName` (`…\resnet18_Opset18_timm.onnx`),
  `ProcessorType` (`HumanOS.PeMiL.MachineLearning.Processors.TNeuralNetworkProcessor`),
  `GpuDeviceId` (`-1` = CPU), `FallbackToCpu` (`true`).
- **`ClassificationSelectionProcessor`** — `ThresholdLevel` (0.2); below it the result is `unknown`.

## The Model

- **`resnet18_Opset18_timm.onnx`** — a ResNet-18 image classifier exported from `timm` (ONNX opset 18),
  trained on the 1000 **ImageNet** classes.
- Input `x` has shape `[1, 3, 224, 224]` (NCHW, RGB) with **no built-in normalization**, so the
  preprocessing script applies the standard ImageNet mean `(0.485, 0.456, 0.406)` and standard
  deviation `(0.229, 0.224, 0.225)` after scaling pixels to `[0, 1]`.
- **`labels.txt`** holds the 1000 class names, one per line (line *N* = class *N*); the same list is
  embedded in `ClassificationSelectionProcessor.cs` as a fallback.

## Processing Flow

1. The webcam is enumerated by the device-detector plugin and captures frames; calling `Start`
   begins streaming raw JPEG frames into the `VideoStream` / `RawImageType` port.
2. `ImagePreprocessing` decodes and normalizes each sampled frame into a `[1, 3, 224, 224]` float
   tensor (`NormalizedImageType`).
3. `ImageClassificationProcessor` runs the ResNet-18 ONNX model and outputs a 1000-element score
   vector (`ClassificationVectorType`).
4. `ClassificationSelectionProcessor` picks the highest-scoring class above the threshold and
   resolves it to a label, writing it to `DetectedClassification`.
5. The OPC-UA server publishes `DetectedClassification` (and the other device nodes) for any client.

## Prerequisites

- HumanOS IoT Runtime ≥ 2.11 with the **Machine Learning plugin** (`HumanOS.PeMiL.MachineLearning`)
  and the **Video Camera plugin** (`HumanOS.UHAL.VideoCamera`) licensed and installed
- A USB / PnP webcam (the included instance is bound to a specific device address — adjust it to your
  own camera, see *Configuration*)
- An OPC-UA client such as [UAExpert](https://www.unified-automation.com/products/development-tools/uaexpert.html)
  to read the `DetectedClassification` node
- For GPU inference (optional): a CUDA-capable GPU; otherwise the model runs on CPU
  (`GpuDeviceId = -1`, `FallbackToCpu = true`)

## Configuration

- **Select the camera**: in the `default` target, edit `Devices/WebCam.json` (and the device entry
  in `ImageClassification.h2proj`) and set the `Address` to your webcam's PnP/USB device path. The
  shipped value (`@device:pnp:\\?\usb#vid_045e&pid_0990…`) must be replaced with the path of the
  machine running the gateway.
- **Swap the model**: drop a different ONNX classifier into `HumanOS.UHAL.VideoCamera/`, update the
  `ModelFileName` property and `labels.txt`, and adjust `TargetWidth`/`TargetHeight` and the
  normalization constants in `ImagePreprocessing.cs` to match the new model's expected input.
- **Tune throughput / sensitivity**: change `SamplingRate` on `ImagePreprocessing` to control how
  often frames are classified, and `ThresholdLevel` on `ClassificationSelectionProcessor` to control
  when a prediction is reported as `unknown`.
- **GPU**: set `GpuDeviceId` to a valid device id to run inference on the GPU.
- **OPC-UA server**: the gateway publishes on `opc.tcp://localhost:4840/`
  (see `HumanOS.PeSeL.OPCUAServer/settings.json`).
- **Deployment**: the `Build/` folder contains the published, deployment-ready target, including
  `RegisterService.ps1` / `UnRegisterService.ps1` to install the gateway as a Windows service.

## See Also

- [HumanOS Runtime Reference Manual](https://doc.cybertech.swiss/runtime/intro)
- [Video Camera Connector Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.UHAL.VideoCamera/)
- [Machine Learning Plugin Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeMiL.MachineLearning/)
- [Processing Networks](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.Runtime/ProcessingNetworks/)
- [OPC-UA Server Manual](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.OPCUAServer/)
</content>
