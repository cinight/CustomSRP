# CustomSRP
A new SRP from scratch

Unity version : 2019.1.0b4

Tested with : 
Win DX11 Gamma / Linear

| Scene | Image | Description |
| --- | - | --- |
| `SRP0101_Basic` | ![](READMEImages/SRP0101_Basic.JPG) | Super basic SRP that renders unlit material objects |
| `SRP0101_Fog` | ![](READMEImages/SRP0101_Fog.gif) | Use Fog on Lighting Settings |
| `SRP0102_AssetSettings` | ![](READMEImages/SRP0102_AssetSettings.gif) | Let the SRP Asset to pass some custom variables |
| `SRP0103_CustomGUI` | ![](READMEImages/SRP0103_CustomGUI.gif) | Have a proper interface for the SRP Asset |
| `SRP0201_FrustumCulling` | ![](READMEImages/SRP0201_FrustumCulling.gif) | Frustum culling always work. This is a test scene to verify the culling results |
| `SRP0202_OcclusionCulling` | ![](READMEImages/SRP0202_OcclusionCulling.gif) | Baked Occlusion Culling |
| `SRP0301_Batching` | ![](READMEImages/SRP0301_Batching.JPG) | Use Static Batching, Dynamic Batching, GPU Instancing and SRP Batcher |
| `SRP0401_NoSpecificPass` | ![](READMEImages/SRP0401_NoSpecificPass.JPG) | To draw the shaders that do not have a tag, e.g. default Unlit shaders |
| `SRP0402_Multipass` | ![](READMEImages/SRP0402_Multipass.JPG) | In SRP we need to specify the pass names, so no more infinite pass. But we can specify the orders of passes |
| `SRP0403_Compute` | ![](READMEImages/SRP0403_Compute.JPG) | Use compute shader to achieve simple edge detection |
| `SRP0501_SoftParticle` | ![](READMEImages/SRP0501_SoftParticle.JPG) | Setup CameraDepthTexture to achieve soft-particle effect |
| `SRP0502_Distortion` | ![](READMEImages/SRP0502_Distortion.gif) | No more grab pass but we can implement our own |
| `SRP0701_HDR_MSAA` | ![](READMEImages/SRP0701_HDR_MSAA.gif) | Use HDR and MSAA |
| `SRP0702_Postprocessing` | ![](READMEImages/SRP0702_Postprocessing.gif) | This shows you how to use Postprocessing Stack with SRP (transparent effects e.g. Bloom, Depth of Field) |
| `SRP0801_UGUI` | ![](READMEImages/SRP0801_UGUI.JPG) | Use UICamera to render UGUI, also render 3D objects and particle on UI |
| `SRP0802_RenderPass` | ![](READMEImages/SRP0802_RenderPass.gif) | Use RenderPass to target multiple color attachments and read / write from / to them |
| `SRP0803_MultiRenderTarget` | ![](READMEImages/SRP0803_MultiRenderTarget.JPG) | Use CommandBuffer.SetRenderTarget() to target multiple color surfaces |
| `SRP0901_SceneViewFix` | ![](READMEImages/SRP0901_SceneViewFix.JPG) | Make the gizmos / icons appear on scene view |
| `SRP0902_SceneViewDrawMode` | ![](READMEImages/SRP0902_SceneViewDrawMode.gif) | Adding custom Scene View draw modes |
| `SRP1001_Error` | ![](READMEImages/SRP1001_Error.JPG) | Render the pink shaders on the materials that the SRP doesn't support |
| `SRP1002_Debug` | ![](READMEImages/SRP1002_Debug.JPG) | Make the Profiler records the timing for SRP performance debugging |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |
| `xxxx` | ![](READMEImages/xxxx.gif) | xxxxxx |




