
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    internal static class SRP0902SceneView
    {
        #if UNITY_EDITOR

        static bool AcceptedDrawMode(SceneView.CameraMode cameraMode)
        {
            if (
			    cameraMode.drawMode == DrawCameraMode.Textured ||
				cameraMode.drawMode == DrawCameraMode.TexturedWire ||
				cameraMode.drawMode == DrawCameraMode.Wireframe ||
                //cameraMode.drawMode == DrawCameraMode.ShadowCascades ||
                //cameraMode.drawMode == DrawCameraMode.RenderPaths ||
                cameraMode.drawMode == DrawCameraMode.AlphaChannel ||
                cameraMode.drawMode == DrawCameraMode.Overdraw ||
                cameraMode.drawMode == DrawCameraMode.Mipmaps ||
                cameraMode.drawMode == DrawCameraMode.UserDefined // ||
                //cameraMode.drawMode == DrawCameraMode.SpriteMask ||
                //cameraMode.drawMode == DrawCameraMode.DeferredDiffuse ||
                //cameraMode.drawMode == DrawCameraMode.DeferredSpecular ||
                //cameraMode.drawMode == DrawCameraMode.DeferredSmoothness ||
                //cameraMode.drawMode == DrawCameraMode.DeferredNormal ||
                //cameraMode.drawMode == DrawCameraMode.ValidateAlbedo ||
                //cameraMode.drawMode == DrawCameraMode.ValidateMetalSpecular ||
                //cameraMode.drawMode == DrawCameraMode.ShadowMasks
                //cameraMode.drawMode == DrawCameraMode.LightOverlap
                )
                return true;

            return false;
        }

        public static void SetupDrawMode()
        {
            //Setup object
            CustomDrawModeAssetObject.SetUpcdma();
            if(CustomDrawModeAssetObject.cdma == null) return;

            //Setup draw mode
            SceneView.ClearUserDefinedCameraModes();
            for (int i=0; i<CustomDrawModeAssetObject.cdma.customDrawModes.Length; i++)
            {
                if(
                    CustomDrawModeAssetObject.cdma.customDrawModes[i].name != "" && 
                    CustomDrawModeAssetObject.cdma.customDrawModes[i].category != ""
                )
                SceneView.AddCameraMode(
                CustomDrawModeAssetObject.cdma.customDrawModes[i].name,
                CustomDrawModeAssetObject.cdma.customDrawModes[i].category);
            }
            ArrayList sceneViewArray = SceneView.sceneViews;
            foreach (SceneView sceneView in sceneViewArray)
            {
                sceneView.onValidateCameraMode -= AcceptedDrawMode; // Clean up
                sceneView.onValidateCameraMode += AcceptedDrawMode;
            }
        }
        #endif
    }

